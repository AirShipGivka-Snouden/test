using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Client.ConnectorServices;
using VpnHood.Core.Client.Exceptions;
using VpnHood.Core.Common.Exceptions;
using VpnHood.Core.Common.Messaging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Streams;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling;
using VpnHood.Core.Tunneling.Connections;
using VpnHood.Core.Tunneling.Messaging;
using VpnHood.Core.Tunneling.Utils;

namespace VpnHood.Core.Client;

internal class RequestSender(ConnectorService connectorService) : IDisposable
{
	private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

	private int _isDisposed;

	public ConnectorService ConnectorService { get; } = connectorService;

	public Task<ConnectorRequestResult<T>> SendRequest<T>(ClientRequest request, CancellationToken cancellationToken) where T : SessionResponse
	{
		ClientRequestEx requestEx = new ClientRequestEx
		{
			Request = request,
			PostBuffer = Memory<byte>.Empty
		};
		return SendRequest<T>(requestEx, cancellationToken);
	}

	public async Task<ConnectorRequestResult<T>> SendRequest<T>(ClientRequestEx requestEx, CancellationToken cancellationToken) where T : SessionResponse
	{
		ClientRequest request = requestEx.Request;
		CancellationTokenSource timeoutCts = new CancellationTokenSource(ConnectorService.RequestTimeout);
		try
		{
			InlineArray3<CancellationToken> buffer = default(InlineArray3<CancellationToken>);
			buffer[0] = timeoutCts.Token;
			buffer[1] = cancellationToken;
			buffer[2] = _cancellationTokenSource.Token;
			using CancellationTokenSource requestCts = CancellationTokenSource.CreateLinkedTokenSource(buffer);
			try
			{
				EventId eventId = GetRequestEventId(request);
				VhLogger.Instance.LogDebug(eventId, "Sending a request. RequestCode: {RequestCode}, RequestId: {RequestId}", (RequestCode)request.RequestCode, request.RequestId);
				Memory<byte> memory = StreamUtils.ObjectToJsonBuffer(request);
				int num = 2 + memory.Length;
				ReadOnlyMemory<byte> postBuffer = requestEx.PostBuffer;
				byte[] array = new byte[num + postBuffer.Length];
				array[0] = 1;
				array[1] = request.RequestCode;
				memory.Span.CopyTo(array.AsSpan(2));
				postBuffer = requestEx.PostBuffer;
				postBuffer.Span.CopyTo(array.AsSpan(2 + memory.Length));
				ConnectorRequestResult<T> connectorRequestResult = await SendRequest<T>(array, request.RequestId, OnAttempt, requestCts.Token).Vhc();
				VhLogger.Instance.LogDebug(eventId, "Received a response... ErrorCode: {ErrorCode}.", connectorRequestResult.Response.ErrorCode);
				lock (ConnectorService.Stat)
				{
					ConnectorService.Stat.RequestCount++;
					return connectorRequestResult;
				}
			}
			catch (Exception) when (timeoutCts.IsCancellationRequested)
			{
				throw new TimeoutException($"Could not send the {(RequestCode)request.RequestCode} request in the given time.");
			}
		}
		finally
		{
			if (timeoutCts != null)
			{
				((IDisposable)timeoutCts).Dispose();
			}
		}
		void OnAttempt()
		{
			timeoutCts.CancelAfter(ConnectorService.RequestTimeout);
		}
	}

	private async Task<ConnectorRequestResult<T>> SendRequest<T>(ReadOnlyMemory<byte> request, string requestId, Action onAttempt, CancellationToken cancellationToken) where T : SessionResponse
	{
		ReusableStreamConnection reusableConnection = ConnectorService.GetFreeConnection();
		if (reusableConnection != null)
		{
			try
			{
				VhLogger.Instance.LogDebug(GeneralEventId.Stream, "A shared Connection has been reused. ConnectionId: {ConnectionId}, LocalEp: {LocalEp}", reusableConnection.ConnectionId, reusableConnection.LocalEndPoint);
				await reusableConnection.Stream.WriteAsync(request, cancellationToken).Vhc();
				T response = await ReadSessionResponse<T>(reusableConnection.Stream, cancellationToken).Vhc();
				lock (ConnectorService.Stat)
				{
					ConnectorService.Stat.ReusedConnectionSucceededCount++;
				}
				return new ConnectorRequestResult<T>
				{
					Response = response,
					StreamConnection = reusableConnection
				};
			}
			catch (SessionException)
			{
				await reusableConnection.DisposeAsync();
				throw;
			}
			catch (Exception exception)
			{
				lock (ConnectorService.Stat)
				{
					ConnectorService.Stat.ReusedConnectionFailedCount++;
				}
				reusableConnection.PreventReuse();
				await reusableConnection.DisposeAsync();
				VhLogger.Instance.LogError(GeneralEventId.Stream, exception, "Error in reusing the Connection. Try a new connection. ConnectionId: {ConnectionId}, RequestId: {requestId}", reusableConnection.ConnectionId, requestId);
			}
		}
		IStreamConnection connection = await ConnectorService.GetConnectionToServer(requestId + ":tunnel", request.Length, onAttempt, cancellationToken).Vhc();
		try
		{
			await connection.Stream.WriteAsync(request, cancellationToken).Vhc();
			await connection.Stream.FlushAsync(cancellationToken);
			if (connection.RequireHttpResponse)
			{
				connection.RequireHttpResponse = false;
				if ((await HttpUtils.ReadResponse(connection.Stream, cancellationToken).Vhc()).StatusCode == HttpStatusCode.Unauthorized)
				{
					throw new UnauthorizedAccessException();
				}
			}
			T response2 = await ReadSessionResponse<T>(connection.Stream, cancellationToken).Vhc();
			return new ConnectorRequestResult<T>
			{
				Response = response2,
				StreamConnection = connection
			};
		}
		catch (SessionException)
		{
			connection.Dispose();
			throw;
		}
		catch (Exception exception2)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.Stream, exception2, "Error in sending a request. ConnectionId: {ConnectionId}, RequestId: {requestId}", connection.ConnectionId, requestId);
			(connection as ReusableStreamConnection)?.PreventReuse();
			connection.Dispose();
			throw;
		}
	}

	private static async Task<T> ReadSessionResponse<T>(Stream stream, CancellationToken cancellationToken) where T : SessionResponse
	{
		string json = await StreamUtils.ReadMessageAsync(stream, cancellationToken).Vhc();
		try
		{
			T val = JsonUtils.Deserialize<T>(json);
			ProcessResponseException(val);
			return val;
		}
		catch (SessionException)
		{
			throw;
		}
		catch when (typeof(T) != typeof(SessionResponse))
		{
			ProcessResponseException(JsonUtils.Deserialize<SessionResponse>(json));
			throw;
		}
	}

	private static void ProcessResponseException(SessionResponse response)
	{
		if (response.ErrorCode == SessionErrorCode.RedirectHost)
		{
			throw new RedirectHostException(response);
		}
		if (response.ErrorCode == SessionErrorCode.Maintenance)
		{
			throw new MaintenanceException();
		}
		if (response.ErrorCode != SessionErrorCode.Ok)
		{
			throw new SessionException(response);
		}
	}

	private static EventId GetRequestEventId(ClientRequest request)
	{
		return (RequestCode)request.RequestCode switch
		{
			RequestCode.Hello => GeneralEventId.Session, 
			RequestCode.Bye => GeneralEventId.Session, 
			RequestCode.TcpPacketChannel => GeneralEventId.PacketChannel, 
			RequestCode.ProxyChannel => GeneralEventId.ProxyChannel, 
			_ => GeneralEventId.Request, 
		};
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
		{
			_cancellationTokenSource.TryCancel();
			_cancellationTokenSource.Dispose();
			ConnectorService.Dispose();
		}
	}
}
