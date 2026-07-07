using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using VpnHood.Core.TcpStack.Abstractions;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.TcpStack;

public sealed class LocalTcpListener : ITcpListener, IDisposable
{
	[CompilerGenerated]
	private sealed class _003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11 : IAsyncEnumerable<ITcpClient>, IAsyncEnumerator<ITcpClient>, IAsyncDisposable, IValueTaskSource<bool>, IValueTaskSource, IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public AsyncIteratorMethodBuilder _003C_003Et__builder;

		public ManualResetValueTaskSourceCore<bool> _003C_003Ev__promiseOfValueOrEnd;

		private ITcpClient _003C_003E2__current;

		private bool _003C_003Ew__disposeMode;

		private CancellationTokenSource _003C_003Ex__combinedTokens;

		private int _003C_003El__initialThreadId;

		public LocalTcpListener _003C_003E4__this;

		private CancellationToken cancellationToken;

		public CancellationToken _003C_003E3__cancellationToken;

		private ConfiguredCancelableAsyncEnumerable<LocalTcpClient>.Enumerator _003C_003E7__wrap1;

		private object _003C_003E7__wrap2;

		private int _003C_003E7__wrap3;

		private ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter _003C_003Eu__1;

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _003C_003Eu__2;

		ITcpClient IAsyncEnumerator<ITcpClient>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11(int _003C_003E1__state)
		{
			_003C_003Et__builder = AsyncIteratorMethodBuilder.Create();
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			LocalTcpListener localTcpListener = _003C_003E4__this;
			try
			{
				ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter;
				switch (num)
				{
				default:
					if (!_003C_003Ew__disposeMode)
					{
						num = (_003C_003E1__state = -1);
						_003C_003E7__wrap1 = localTcpListener.AcceptAllAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAsyncEnumerator();
						_003C_003E7__wrap2 = null;
						_003C_003E7__wrap3 = 0;
						goto case -4;
					}
					goto end_IL_000e;
				case -4:
				case 0:
					try
					{
						ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter awaiter2;
						if (num != -4)
						{
							if (num != 0)
							{
								goto IL_00b9;
							}
							awaiter2 = _003C_003Eu__1;
							_003C_003Eu__1 = default(ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_0124;
						}
						num = (_003C_003E1__state = -1);
						if (!_003C_003Ew__disposeMode)
						{
							goto IL_00b9;
						}
						goto end_IL_0074;
						IL_00b9:
						_003C_003E2__current = null;
						awaiter2 = _003C_003E7__wrap1.MoveNextAsync().GetAwaiter();
						if (!awaiter2.IsCompleted)
						{
							num = (_003C_003E1__state = 0);
							_003C_003Eu__1 = awaiter2;
							_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11 stateMachine = this;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref stateMachine);
							return;
						}
						goto IL_0124;
						IL_0124:
						if (awaiter2.GetResult())
						{
							LocalTcpClient current = _003C_003E7__wrap1.Current;
							_003C_003E2__current = current;
							num = (_003C_003E1__state = -4);
							goto IL_02a5;
						}
						end_IL_0074:;
					}
					catch (object obj)
					{
						_003C_003E7__wrap2 = obj;
					}
					_003C_003E2__current = null;
					awaiter = _003C_003E7__wrap1.DisposeAsync().GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 1);
						_003C_003Eu__2 = awaiter;
						_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11 stateMachine = this;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
						return;
					}
					break;
				case 1:
					awaiter = _003C_003Eu__2;
					_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
					num = (_003C_003E1__state = -1);
					break;
				}
				awaiter.GetResult();
				object obj2 = _003C_003E7__wrap2;
				if (obj2 != null)
				{
					ExceptionDispatchInfo.Capture((obj2 as Exception) ?? throw obj2).Throw();
				}
				_ = _003C_003E7__wrap3;
				if (!_003C_003Ew__disposeMode)
				{
					_003C_003E7__wrap2 = null;
					_003C_003E7__wrap1 = default(ConfiguredCancelableAsyncEnumerable<LocalTcpClient>.Enumerator);
				}
				end_IL_000e:;
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003C_003E7__wrap1 = default(ConfiguredCancelableAsyncEnumerable<LocalTcpClient>.Enumerator);
				_003C_003E7__wrap2 = null;
				if (_003C_003Ex__combinedTokens != null)
				{
					_003C_003Ex__combinedTokens.Dispose();
					_003C_003Ex__combinedTokens = null;
				}
				_003C_003E2__current = null;
				_003C_003Et__builder.Complete();
				_003C_003Ev__promiseOfValueOrEnd.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003C_003E7__wrap1 = default(ConfiguredCancelableAsyncEnumerable<LocalTcpClient>.Enumerator);
			_003C_003E7__wrap2 = null;
			if (_003C_003Ex__combinedTokens != null)
			{
				_003C_003Ex__combinedTokens.Dispose();
				_003C_003Ex__combinedTokens = null;
			}
			_003C_003E2__current = null;
			_003C_003Et__builder.Complete();
			_003C_003Ev__promiseOfValueOrEnd.SetResult(result: false);
			return;
			IL_02a5:
			_003C_003Ev__promiseOfValueOrEnd.SetResult(result: true);
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}

		[DebuggerHidden]
		IAsyncEnumerator<ITcpClient> IAsyncEnumerable<ITcpClient>.GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
		{
			_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11 _003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__12;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = -3;
				_003C_003Et__builder = AsyncIteratorMethodBuilder.Create();
				_003C_003Ew__disposeMode = false;
				_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__12 = this;
			}
			else
			{
				_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__12 = new _003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11(-3)
				{
					_003C_003E4__this = _003C_003E4__this
				};
			}
			if (_003C_003E3__cancellationToken.Equals(default(CancellationToken)))
			{
				_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__12.cancellationToken = cancellationToken;
			}
			else if (cancellationToken.Equals(_003C_003E3__cancellationToken) || cancellationToken.Equals(default(CancellationToken)))
			{
				_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__12.cancellationToken = _003C_003E3__cancellationToken;
			}
			else
			{
				_003C_003Ex__combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(_003C_003E3__cancellationToken, cancellationToken);
				_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__12.cancellationToken = _003C_003Ex__combinedTokens.Token;
			}
			return _003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__12;
		}

		[DebuggerHidden]
		ValueTask<bool> IAsyncEnumerator<ITcpClient>.MoveNextAsync()
		{
			if (_003C_003E1__state == -2)
			{
				return default(ValueTask<bool>);
			}
			_003C_003Ev__promiseOfValueOrEnd.Reset();
			_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11 stateMachine = this;
			_003C_003Et__builder.MoveNext(ref stateMachine);
			short version = _003C_003Ev__promiseOfValueOrEnd.Version;
			if (_003C_003Ev__promiseOfValueOrEnd.GetStatus(version) == ValueTaskSourceStatus.Succeeded)
			{
				return new ValueTask<bool>(_003C_003Ev__promiseOfValueOrEnd.GetResult(version));
			}
			return new ValueTask<bool>(this, version);
		}

		[DebuggerHidden]
		bool IValueTaskSource<bool>.GetResult(short token)
		{
			return _003C_003Ev__promiseOfValueOrEnd.GetResult(token);
		}

		[DebuggerHidden]
		ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token)
		{
			return _003C_003Ev__promiseOfValueOrEnd.GetStatus(token);
		}

		[DebuggerHidden]
		void IValueTaskSource<bool>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_003C_003Ev__promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags);
		}

		[DebuggerHidden]
		void IValueTaskSource.GetResult(short token)
		{
			_003C_003Ev__promiseOfValueOrEnd.GetResult(token);
		}

		[DebuggerHidden]
		ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
		{
			return _003C_003Ev__promiseOfValueOrEnd.GetStatus(token);
		}

		[DebuggerHidden]
		void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_003C_003Ev__promiseOfValueOrEnd.OnCompleted(continuation, state, token, flags);
		}

		[DebuggerHidden]
		ValueTask IAsyncDisposable.DisposeAsync()
		{
			if (_003C_003E1__state >= -1)
			{
				throw new NotSupportedException();
			}
			if (_003C_003E1__state == -2)
			{
				return default(ValueTask);
			}
			_003C_003Ew__disposeMode = true;
			_003C_003Ev__promiseOfValueOrEnd.Reset();
			_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11 stateMachine = this;
			_003C_003Et__builder.MoveNext(ref stateMachine);
			return new ValueTask(this, _003C_003Ev__promiseOfValueOrEnd.Version);
		}
	}

	private readonly Channel<LocalTcpClient> _acceptQueue = Channel.CreateUnbounded<LocalTcpClient>(new UnboundedChannelOptions
	{
		SingleReader = true
	});

	private readonly LocalTcpStack _stack;

	private int _stopped;

	public IpEndPointValue? LocalEndPoint { get; }

	public bool IsAny => !LocalEndPoint.HasValue;

	internal LocalTcpListener(LocalTcpStack stack, IpEndPointValue? localEndPoint)
	{
		_stack = stack;
		LocalEndPoint = localEndPoint;
	}

	internal bool TryEnqueueAccept(LocalTcpClient client)
	{
		if (Volatile.Read(in _stopped) != 0)
		{
			return false;
		}
		return _acceptQueue.Writer.TryWrite(client);
	}

	public IAsyncEnumerable<LocalTcpClient> AcceptAllAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return _acceptQueue.Reader.ReadAllAsync(cancellationToken);
	}

	[AsyncIteratorStateMachine(typeof(_003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11))]
	IAsyncEnumerable<ITcpClient> ITcpListener.AcceptAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
	{
		return new _003CVpnHood_002DCore_002DTcpStack_002DAbstractions_002DITcpListener_002DAcceptAllAsync_003Ed__11(-2)
		{
			_003C_003E4__this = this,
			_003C_003E3__cancellationToken = cancellationToken
		};
	}

	public ValueTask<LocalTcpClient> AcceptAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return _acceptQueue.Reader.ReadAsync(cancellationToken);
	}

	async ValueTask<ITcpClient> ITcpListener.AcceptAsync(CancellationToken cancellationToken)
	{
		return await AcceptAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	public void Stop()
	{
		if (Interlocked.Exchange(ref _stopped, 1) == 0)
		{
			_acceptQueue.Writer.TryComplete();
			if (LocalEndPoint.HasValue)
			{
				_stack.StopListening(LocalEndPoint.Value);
			}
			else
			{
				_stack.StopListeningAny();
			}
			LocalTcpClient item;
			while (_acceptQueue.Reader.TryRead(out item))
			{
				item.Dispose();
			}
		}
	}

	public void Dispose()
	{
		Stop();
	}
}
