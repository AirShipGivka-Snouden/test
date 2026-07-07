using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VpnHood.Core.Packets;
using VpnHood.Core.Packets.Extensions;
using VpnHood.Core.TcpStack.Abstractions;
using VpnHood.Core.TcpStack.Primitives;
using VpnHood.Core.Toolkit.Logging;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.TcpStack;

internal sealed class LocalTcpConnection(IPEndPointPairValue ipEndPointPair, uint isnLocal, uint isnRemote, ushort? peerMss, LocalTcpListener listener, byte peerWsShift = 0, TimeSpan? tcpTimeout = null) : IDisposable
{
	private const ushort LoopbackWindowSize = ushort.MaxValue;

	private const ushort DefaultMss = 536;

	private const ushort MaxMss = 1460;

	private readonly TimeSpan _idleTimeout = tcpTimeout ?? TimeSpan.FromMinutes(15L);

	private static readonly TimeSpan IdleCheckInterval = TimeSpan.FromMinutes(1L);

	private static readonly PipeOptions PipeOpts = new PipeOptions(null, null, null, 65535L, 32767L, -1, useSynchronizationContext: false);

	private readonly Pipe _netToAppPipe = new Pipe(PipeOpts);

	private readonly Lock _seqLock = new Lock();

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private readonly SemaphoreSlim _windowSignal = new SemaphoreSlim(0, 1);

	private LocalTcpClient? _pendingClient;

	private LocalTcpStack? _stack;

	private bool _finSent;

	private bool _finReceived;

	private bool _sndNxtAfterSynSet;

	private bool _disposed;

	private int _closedFlag;

	private long _lastActivityTicks = Stopwatch.GetTimestamp();

	private bool _netToAppCompleted;

	private bool _appToNetCompleted;

	private uint _sndNxt = isnLocal;

	private uint _sndUna = isnLocal;

	private readonly byte _peerWsShift = (byte)((peerWsShift > 14) ? 14 : peerWsShift);

	private uint _peerWindow = 65535u;

	private uint _rcvNxt = isnRemote + 1;

	private int _unackedSegments;

	private int _ackCount;

	private int _lastZeroWinLogTick;

	private int _lastZwpLogTick;

	private const int RetxBufferSize = 65536;

	private readonly byte[] _retxBuffer = new byte[65536];

	private int _retxRingStart;

	private int _retxBufferLen;

	private uint _lastDupAck;

	private int _dupAckCount;

	private long _retxCount;

	public IPEndPointPairValue IpEndPointPair => ipEndPointPair;

	public uint IsnLocal { get; } = isnLocal;

	public ushort Mss { get; } = ClampMss(peerMss);

	public TcpConnectionState State { get; private set; }

	public PipeReader NetToAppReader => _netToAppPipe.Reader;

	public event Action<LocalTcpConnection>? OnClosed;

	private static ushort ClampMss(ushort? peerMss)
	{
		if ((!peerMss.HasValue || peerMss.GetValueOrDefault() == 0) ? true : false)
		{
			return 536;
		}
		ushort value = peerMss.Value;
		if (value < 64)
		{
			return 64;
		}
		if (value > 1460)
		{
			return 1460;
		}
		return value;
	}

	public void Start(LocalTcpStack stack)
	{
		_stack = stack;
		LocalTcpStream stream = new LocalTcpStream(this, stack);
		_pendingClient = new LocalTcpClient(stream, ipEndPointPair.Destination.ToIPEndPoint(), ipEndPointPair.Source.ToIPEndPoint());
		Task.Run((Func<Task?>)MonitorIdleAsync);
	}

	public Task GracefulCloseAsync(LocalTcpStack stack)
	{
		if (_disposed)
		{
			return Task.CompletedTask;
		}
		_appToNetCompleted = true;
		TryStartFin(stack);
		return Task.CompletedTask;
	}

	public void SetSndNxtAfterSyn()
	{
		using (_seqLock.EnterScope())
		{
			if (!_sndNxtAfterSynSet)
			{
				_sndNxt = IsnLocal + 1;
				_sndUna = IsnLocal + 1;
				_sndNxtAfterSynSet = true;
			}
		}
	}

	public void MarkEstablished()
	{
		LocalTcpClient pendingClient;
		using (_seqLock.EnterScope())
		{
			if (State != TcpConnectionState.SynReceived)
			{
				return;
			}
			State = TcpConnectionState.Established;
			pendingClient = _pendingClient;
			_pendingClient = null;
		}
		if (pendingClient != null && !listener.TryEnqueueAccept(pendingClient))
		{
			pendingClient.Dispose();
		}
	}

	public (uint sndNxt, uint rcvNxt) SnapshotSequence()
	{
		using (_seqLock.EnterScope())
		{
			return (sndNxt: _sndNxt, rcvNxt: _rcvNxt);
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		try
		{
			_cts.Cancel();
		}
		catch
		{
		}
	}

	public async ValueTask SendAppDataAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default(CancellationToken))
	{
		if (_disposed || _appToNetCompleted)
		{
			return;
		}
		Touch();
		LocalTcpStack stack = _stack;
		if (stack == null)
		{
			return;
		}
		ushort mss = Mss;
		int offset = 0;
		while (offset < data.Length)
		{
			ct.ThrowIfCancellationRequested();
			if (_disposed || _appToNetCompleted)
			{
				break;
			}
			DrainWindowSignal();
			uint pw;
			uint sndUna;
			uint sndNxt;
			int num2;
			using (_seqLock.EnterScope())
			{
				pw = _peerWindow;
				sndUna = _sndUna;
				sndNxt = _sndNxt;
				long num = sndNxt - sndUna;
				int val = (int)Math.Max(0L, _peerWindow - num);
				int val2 = 65536 - _retxBufferLen;
				num2 = Math.Min(val, val2);
			}
			if (num2 <= 0)
			{
				int tickCount = Environment.TickCount;
				if (tickCount - _lastZeroWinLogTick > 500)
				{
					_lastZeroWinLogTick = tickCount;
					if (stack.VerboseLogging)
					{
						VhLogger.Instance.LogTrace(TcpStackEventIds.TcpStackDiag, "[SEND] zero-win wait offset={Offset}/{DataLength} pw={PeerWindow} sndUna={SndUna} sndNxt={SndNxt} inFlight={InFlight}", offset, data.Length, pw, sndUna, sndNxt, (long)(sndNxt - sndUna));
					}
				}
				using (CancellationTokenSource zwpCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
				{
					zwpCts.CancelAfter(TimeSpan.FromMilliseconds(200L));
					try
					{
						await _windowSignal.WaitAsync(zwpCts.Token);
					}
					catch (OperationCanceledException) when (!ct.IsCancellationRequested)
					{
						if (offset >= data.Length)
						{
							continue;
						}
						if (Environment.TickCount - _lastZwpLogTick > 500)
						{
							_lastZwpLogTick = Environment.TickCount;
							if (stack.VerboseLogging)
							{
								VhLogger.Instance.LogTrace(TcpStackEventIds.TcpStackDiag, "[SEND] ZWP fire offset={Offset} pw={PeerWindow}", offset, pw);
							}
						}
						offset += SendZeroWindowProbe(stack, data.Span[offset]);
					}
					catch (OperationCanceledException)
					{
						break;
					}
				}
				continue;
			}
			int num3 = Math.Min(data.Length - offset, num2);
			while (num3 > 0)
			{
				int num4 = Math.Min(num3, mss);
				ReadOnlySpan<byte> readOnlySpan = data.Span.Slice(offset, num4);
				IpPacket ipPacket = PacketBuilder.BuildTcp(IpEndPointPair.Destination, IpEndPointPair.Source, ReadOnlySpan<byte>.Empty, readOnlySpan);
				TcpPacket tcpPacket = ipPacket.ExtractTcp();
				uint sndNxt2;
				uint rcvNxt;
				using (_seqLock.EnterScope())
				{
					sndNxt2 = _sndNxt;
					rcvNxt = _rcvNxt;
					_sndNxt += (uint)num4;
					AppendToRetxBufferLocked(readOnlySpan);
				}
				tcpPacket.SequenceNumber = sndNxt2;
				tcpPacket.AcknowledgmentNumber = rcvNxt;
				tcpPacket.Acknowledgment = true;
				tcpPacket.WindowSize = ushort.MaxValue;
				if (offset + num4 >= data.Length)
				{
					tcpPacket.Push = true;
				}
				stack.SendPacket(ipPacket);
				offset += num4;
				num3 -= num4;
			}
		}
	}

	public (bool handled, bool needsAck) TryHandleIncoming(uint seq, uint ack, ushort windowSize, TcpFlags flags, ReadOnlySpan<byte> payload)
	{
		if (_disposed)
		{
			return (handled: false, needsAck: false);
		}
		Touch();
		if (flags.HasFlag(TcpFlags.Rst))
		{
			Close();
			return (handled: false, needsAck: false);
		}
		if (flags.HasFlag(TcpFlags.Ack))
		{
			bool flag = false;
			uint sndUna;
			uint sndNxt;
			long num;
			uint peerWindow;
			using (_seqLock.EnterScope())
			{
				sndUna = _sndUna;
				sndNxt = _sndNxt;
				num = ack - _sndUna;
				if (num > 0 && num <= _sndNxt - _sndUna)
				{
					_sndUna = ack;
					int num2 = (int)num;
					if (num2 >= _retxBufferLen)
					{
						_retxBufferLen = 0;
						_retxRingStart = 0;
					}
					else
					{
						_retxRingStart = (_retxRingStart + num2) % 65536;
						_retxBufferLen -= num2;
					}
					_dupAckCount = 0;
					_lastDupAck = ack;
				}
				else if (num == 0L && payload.Length == 0 && _retxBufferLen > 0)
				{
					if (ack == _lastDupAck)
					{
						_dupAckCount++;
					}
					else
					{
						_lastDupAck = ack;
						_dupAckCount = 1;
					}
					if (_dupAckCount >= 3)
					{
						flag = true;
						_dupAckCount = 0;
					}
				}
				_peerWindow = (uint)(windowSize << (int)_peerWsShift);
				peerWindow = _peerWindow;
			}
			int num3 = Interlocked.Increment(ref _ackCount);
			if (windowSize == 0 || peerWindow < 4096 || (num <= 0 && payload.Length == 0) || num3 % 5000 == 0)
			{
				LocalTcpStack? stack = _stack;
				if (stack != null && stack.VerboseLogging)
				{
					VhLogger.Instance.LogTrace(TcpStackEventIds.TcpStackDiag, "[ACK#{AckCount}] ack={Ack} prevUna={PrevUna} nxt={SndNxtSnap} diff={Diff} winRaw={WindowSize} pw={NewPw} payload={PayloadLength} sig={CurrentCount} dup={DupAckCount}", num3, ack, sndUna, sndNxt, num, windowSize, peerWindow, payload.Length, _windowSignal.CurrentCount, _dupAckCount);
				}
			}
			if (flag)
			{
				long num4 = Interlocked.Increment(ref _retxCount);
				LocalTcpStack? stack2 = _stack;
				if (stack2 != null && stack2.VerboseLogging)
				{
					VhLogger.Instance.LogTrace(TcpStackEventIds.TcpStackDiag, "[RETX#{RetxCount}] fast retransmit at sndUna={Ack} retxLen={RetxBufferLen}", num4, ack, _retxBufferLen);
				}
				FastRetransmit();
			}
			TrySignalWindow();
		}
		try
		{
			bool flag2;
			bool flag3;
			using (_seqLock.EnterScope())
			{
				long num5 = (long)seq - (long)_rcvNxt;
				if (num5 < 0)
				{
					if ((uint)((int)seq + payload.Length) > _rcvNxt && payload.Length > 0)
					{
						int start = (int)(_rcvNxt - seq);
						ReadOnlySpan<byte> data = payload.Slice(start);
						if (data.Length > 0)
						{
							WriteToAppPipe(data);
							_rcvNxt += (uint)data.Length;
						}
					}
					return (handled: true, needsAck: true);
				}
				if (num5 > 0)
				{
					return (handled: true, needsAck: true);
				}
				if (payload.Length > 0)
				{
					WriteToAppPipe(payload);
					_rcvNxt += (uint)payload.Length;
				}
				flag2 = payload.Length > 0;
				flag3 = false;
				if (flag2 && !flags.HasFlag(TcpFlags.Fin) && !flags.HasFlag(TcpFlags.Psh))
				{
					_unackedSegments++;
					if (_unackedSegments < 2)
					{
						flag2 = false;
					}
					else
					{
						_unackedSegments = 0;
					}
				}
				else if (flag2)
				{
					_unackedSegments = 0;
				}
				if (flags.HasFlag(TcpFlags.Fin))
				{
					_rcvNxt++;
					_finReceived = true;
					flag2 = true;
					if (_finSent)
					{
						State = TcpConnectionState.Closed;
						flag3 = true;
					}
					else
					{
						State = TcpConnectionState.Closing;
					}
				}
			}
			if (flags.HasFlag(TcpFlags.Fin))
			{
				CompleteNetToApp();
				if (flag3)
				{
					Close();
				}
			}
			return (handled: true, needsAck: flag2);
		}
		catch (InvalidOperationException)
		{
			Close();
			return (handled: false, needsAck: false);
		}
	}

	private void WriteToAppPipe(ReadOnlySpan<byte> data)
	{
		if (!_disposed && !_netToAppCompleted)
		{
			Span<byte> span = _netToAppPipe.Writer.GetSpan(data.Length);
			data.CopyTo(span);
			_netToAppPipe.Writer.Advance(data.Length);
			_netToAppPipe.Writer.FlushAsync();
		}
	}

	private void Touch()
	{
		Interlocked.Exchange(ref _lastActivityTicks, Stopwatch.GetTimestamp());
	}

	private async Task MonitorIdleAsync()
	{
		try
		{
			using PeriodicTimer timer = new PeriodicTimer(IdleCheckInterval);
			while (await timer.WaitForNextTickAsync(_cts.Token) && !_disposed && State != TcpConnectionState.Closed)
			{
				if (Stopwatch.GetElapsedTime(Interlocked.Read(in _lastActivityTicks)) >= _idleTimeout)
				{
					Close();
					break;
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
	}

	public void StartFin(LocalTcpStack stack)
	{
		IpPacket ipPacket;
		bool finReceived;
		using (_seqLock.EnterScope())
		{
			if (_finSent)
			{
				return;
			}
			_finSent = true;
			_appToNetCompleted = true;
			ipPacket = PacketBuilder.BuildTcp(IpEndPointPair.Destination, IpEndPointPair.Source, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty);
			TcpPacket tcpPacket = ipPacket.ExtractTcp();
			tcpPacket.SequenceNumber = _sndNxt;
			tcpPacket.AcknowledgmentNumber = _rcvNxt;
			tcpPacket.Finish = true;
			tcpPacket.Acknowledgment = true;
			tcpPacket.WindowSize = ushort.MaxValue;
			_sndNxt++;
			finReceived = _finReceived;
			State = (finReceived ? TcpConnectionState.Closed : TcpConnectionState.FinWait1);
		}
		stack.SendPacket(ipPacket);
		if (finReceived)
		{
			Close();
		}
	}

	public void TryStartFin(LocalTcpStack stack)
	{
		try
		{
			StartFin(stack);
		}
		catch
		{
		}
	}

	private void CompleteNetToApp()
	{
		if (_netToAppCompleted)
		{
			return;
		}
		_netToAppCompleted = true;
		try
		{
			_netToAppPipe.Writer.Complete();
		}
		catch
		{
		}
	}

	private void TrySignalWindow()
	{
		if (_windowSignal.CurrentCount == 0)
		{
			try
			{
				_windowSignal.Release();
			}
			catch (SemaphoreFullException)
			{
			}
		}
	}

	private int SendZeroWindowProbe(LocalTcpStack stack, byte probeByte)
	{
		if (_disposed || _appToNetCompleted)
		{
			return 0;
		}
		IpPacket ipPacket = null;
		try
		{
			ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>((byte)probeByte);
			ipPacket = PacketBuilder.BuildTcp(IpEndPointPair.Destination, IpEndPointPair.Source, ReadOnlySpan<byte>.Empty, payload);
			TcpPacket tcpPacket = ipPacket.ExtractTcp();
			uint sndNxt;
			uint rcvNxt;
			using (_seqLock.EnterScope())
			{
				sndNxt = _sndNxt;
				rcvNxt = _rcvNxt;
				_sndNxt++;
			}
			tcpPacket.SequenceNumber = sndNxt;
			tcpPacket.AcknowledgmentNumber = rcvNxt;
			tcpPacket.Acknowledgment = true;
			tcpPacket.WindowSize = ushort.MaxValue;
			stack.SendPacket(ipPacket);
			return 1;
		}
		catch
		{
			ipPacket?.Dispose();
			return 0;
		}
	}

	private void DrainWindowSignal()
	{
		while (_windowSignal.Wait(0))
		{
		}
	}

	private void AppendToRetxBufferLocked(ReadOnlySpan<byte> segment)
	{
		if (segment.Length != 0)
		{
			int num = (_retxRingStart + _retxBufferLen) % 65536;
			int num2 = Math.Min(segment.Length, 65536 - num);
			segment.Slice(0, num2).CopyTo(_retxBuffer.AsSpan(num));
			if (num2 < segment.Length)
			{
				segment.Slice(num2).CopyTo(_retxBuffer.AsSpan(0));
			}
			_retxBufferLen += segment.Length;
		}
	}

	private void FastRetransmit()
	{
		LocalTcpStack stack = _stack;
		if (stack == null || _disposed)
		{
			return;
		}
		byte[] array;
		uint sndUna;
		uint rcvNxt;
		using (_seqLock.EnterScope())
		{
			if (_retxBufferLen == 0)
			{
				return;
			}
			int num = Math.Min(_retxBufferLen, Mss);
			array = new byte[num];
			int num2 = Math.Min(num, 65536 - _retxRingStart);
			Array.Copy(_retxBuffer, _retxRingStart, array, 0, num2);
			if (num2 < num)
			{
				Array.Copy(_retxBuffer, 0, array, num2, num - num2);
			}
			sndUna = _sndUna;
			rcvNxt = _rcvNxt;
		}
		IpPacket ipPacket = PacketBuilder.BuildTcp(IpEndPointPair.Destination, IpEndPointPair.Source, ReadOnlySpan<byte>.Empty, array);
		TcpPacket tcpPacket = ipPacket.ExtractTcp();
		tcpPacket.SequenceNumber = sndUna;
		tcpPacket.AcknowledgmentNumber = rcvNxt;
		tcpPacket.Acknowledgment = true;
		tcpPacket.WindowSize = ushort.MaxValue;
		tcpPacket.Push = true;
		stack.SendPacket(ipPacket);
	}

	private void Close()
	{
		if (Interlocked.Exchange(ref _closedFlag, 1) == 0)
		{
			LocalTcpClient pendingClient;
			using (_seqLock.EnterScope())
			{
				State = TcpConnectionState.Closed;
				_finSent = true;
				pendingClient = _pendingClient;
				_pendingClient = null;
			}
			pendingClient?.Dispose();
			CompleteNetToApp();
			_appToNetCompleted = true;
			TrySignalWindow();
			try
			{
				this.OnClosed?.Invoke(this);
			}
			catch
			{
			}
			Dispose();
		}
	}
}
