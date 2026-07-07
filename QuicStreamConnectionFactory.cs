using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using VpnHood.Core.Toolkit.Extensions;
using VpnHood.Core.Toolkit.Jobs;
using VpnHood.Core.Toolkit.Utils;
using VpnHood.Core.Tunneling.Connections;

namespace VpnHood.Core.Client.ConnectorServices;

internal class QuicStreamConnectionFactory : IAsyncDisposable
{
	private readonly VpnEndPoint _vpnEndPoint;

	private readonly RemoteCertificateValidationCallback _certificateValidationCallback;

	private readonly List<QuicStreamConnectionItem> _items = new List<QuicStreamConnectionItem>();

	private readonly Job _cleanupJob;

	private int _isDisposed;

	public int MaxStreamsPerConnection { get; set; } = 30;

	public int MaxLifetimeStreamsPerConnection { get; set; } = 500;

	public TimeSpan IdleConnectionTimeout { get; set; } = TimeSpan.FromMinutes(2L);

	public IPEndPoint? QuicEndPoint { get; set; }

	public QuicStreamConnectionFactory(VpnEndPoint vpnEndPoint, RemoteCertificateValidationCallback certificateValidationCallback)
	{
		_vpnEndPoint = vpnEndPoint;
		_certificateValidationCallback = certificateValidationCallback;
		_cleanupJob = new Job(Cleanup, "QuicStreamConnectionCleanup");
	}

	public Task<IStreamConnection> CreateConnection(string connectionId, CancellationToken cancellationToken)
	{
		return GetOrCreateConnectionItem().OpenStreamConnection(_vpnEndPoint, _certificateValidationCallback, connectionId, cancellationToken);
	}

	private QuicStreamConnectionItem GetOrCreateConnectionItem()
	{
		lock (_items)
		{
			QuicStreamConnectionItem quicStreamConnectionItem = _items.FirstOrDefault((QuicStreamConnectionItem c) => c.CanOpenStream);
			if (quicStreamConnectionItem != null)
			{
				return quicStreamConnectionItem;
			}
			IPEndPoint quicEndPoint = QuicEndPoint ?? throw new InvalidOperationException("QuicEndPoint has not been set.");
			quicStreamConnectionItem = new QuicStreamConnectionItem(MaxStreamsPerConnection, MaxLifetimeStreamsPerConnection, quicEndPoint);
			_items.Add(quicStreamConnectionItem);
			return quicStreamConnectionItem;
		}
	}

	private ValueTask Cleanup(CancellationToken cancellationToken)
	{
		List<Task> list = new List<Task>();
		lock (_items)
		{
			QuicStreamConnectionItem[] array = _items.Where((QuicStreamConnectionItem x) => x.ActiveStreamCount == 0).ToArray();
			foreach (QuicStreamConnectionItem quicStreamConnectionItem in array)
			{
				if (quicStreamConnectionItem.IsDead || quicStreamConnectionItem.ZeroActiveSince + IdleConnectionTimeout <= FastDateTime.Now)
				{
					_items.Remove(quicStreamConnectionItem);
					list.Add(quicStreamConnectionItem.SafeDisposeAsync().AsTask());
				}
			}
		}
		return new ValueTask(Task.WhenAll(list));
	}

	public ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
		{
			return ValueTask.CompletedTask;
		}
		_cleanupJob.Dispose();
		List<QuicStreamConnectionItem> source;
		lock (_items)
		{
			source = _items.ToList();
			_items.Clear();
		}
		return new ValueTask(Task.WhenAll(source.Select((QuicStreamConnectionItem c) => c.SafeDisposeAsync().AsTask())));
	}
}
