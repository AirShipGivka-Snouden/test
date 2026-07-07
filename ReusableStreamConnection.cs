using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Channels.Streams;

namespace VpnHood.Core.Tunneling.Connections;

public class ReusableStreamConnection : StreamConnectionDecorator
{
	private readonly Lock _reuseLock = new Lock();

	private readonly ReuseConnectionCallback _reuseConnectionCallback;

	private bool _allowReuse = true;

	private bool _reusing;

	private bool CanReuse
	{
		get
		{
			if (_allowReuse)
			{
				if (base.Stream is ChunkStream chunkStream)
				{
					return chunkStream.CanReuse;
				}
				return false;
			}
			return false;
		}
	}

	private static bool IsDebug => false;

	public ReusableStreamConnection(IStreamConnection streamConnection, ReuseConnectionCallback reuseConnectionCallback)
		: base(streamConnection)
	{
		_reuseConnectionCallback = reuseConnectionCallback;
		VhLogger.Instance.LogDebug(GeneralEventId.Stream, "A ReusableConnection has been created. ConnectionId: {ConnectionId}, StreamType: {StreamType}, LocalEp: {LocalEp}, RemoteEp: {RemoteEp}", base.ConnectionId, streamConnection.Stream.GetType().Name, VhLogger.Format(base.LocalEndPoint), VhLogger.Format(base.RemoteEndPoint));
	}

	public void PreventReuse()
	{
		using (_reuseLock.EnterScope())
		{
			_allowReuse = false;
			if (base.Stream is ChunkStream chunkStream)
			{
				chunkStream.PreventReuse();
			}
		}
	}

	private async Task ReuseThenDispose()
	{
		_ = 1;
		try
		{
			_reusing = true;
			VhLogger.Instance.LogDebug(GeneralEventId.Stream, "Reusing a connection. ConnectionId: {ConnectionId}", base.ConnectionId);
			if (!(base.Stream is ChunkStream chunkStream))
			{
				throw new InvalidOperationException("Can not reuse the stream when stream is not ChunkStream.");
			}
			await Reuse(chunkStream, _reuseConnectionCallback).Vhc();
		}
		catch (Exception exception)
		{
			VhLogger.Instance.LogDebug(GeneralEventId.Stream, exception, "Could not reuse the connection. ConnectionId: {ConnectionId}", base.ConnectionId);
			PreventReuse();
			await DisposeAsync();
		}
		finally
		{
			_reusing = false;
		}
	}

	private async Task Reuse(ChunkStream chunkStream, ReuseConnectionCallback reuseConnectionCallback)
	{
		ChunkStream stream = await chunkStream.CreateReuse().Vhc();
		using (_reuseLock.EnterScope())
		{
			ObjectDisposedException.ThrowIf(Disposed, this);
			object obj;
			if (!IsDebug)
			{
				IDisposable disposable = ExecutionContext.SuppressFlow();
				obj = disposable;
			}
			else
			{
				obj = null;
			}
			using (obj)
			{
				StreamConnectionDecorator streamConnection = new StreamConnectionDecorator(base.InnerStreamConnection, stream);
				ReusableStreamConnection reusableConnection = new ReusableStreamConnection(streamConnection, reuseConnectionCallback);
				Task.Run(delegate
				{
					reuseConnectionCallback(reusableConnection);
				}).ContinueWith(delegate(Task task)
				{
					VhLogger.Instance.LogError(GeneralEventId.Stream, task.Exception, "Reuse callback failed. ConnectionId: {ConnectionId}", base.ConnectionId);
				}, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
				Disposed = true;
			}
		}
	}

	public override void Dispose()
	{
		using (_reuseLock.EnterScope())
		{
			if (Disposed)
			{
				return;
			}
			if (_allowReuse)
			{
				if (_reusing)
				{
					return;
				}
				if (CanReuse)
				{
					ReuseThenDispose();
					return;
				}
			}
			PreventReuse();
			base.Dispose();
			Disposed = true;
		}
	}

	public override ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}
}
