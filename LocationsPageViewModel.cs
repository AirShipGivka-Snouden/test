using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using Ciphra.VPN.Common.ViewModels.DataViewModels;
using Ciphra.VPN.Common.ViewModels.DialogViewModels;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;

namespace Ciphra.VPN.Common.ViewModels;

public class LocationsPageViewModel : ReactiveObject, IExceptionBroadcaster
{
	private readonly VpnServerRepository _serverRepository;

	private readonly DialogController _dialogController;

	private readonly Settings _settings;

	private readonly ReadOnlyObservableCollection<VpnServerViewModel> _bindingData;

	private readonly SourceList<VpnServerDto> _sourceList = new SourceList<VpnServerDto>((IObservable<IChangeSet<VpnServerDto>>)null);

	private readonly ObservableAsPropertyHelper<bool> _isLoading;

	private string _filterQuery;

	private EqualityComparer<VpnServerDto> _serverComparer = EqualityComparer<VpnServerDto>.Create(delegate(VpnServerDto? dto, VpnServerDto? serverDto)
	{
		if (dto == null || serverDto == null)
		{
			return false;
		}
		if (dto.Id != serverDto.Id)
		{
			return false;
		}
		return !(dto.AccessKey != serverDto.AccessKey);
	});

	public AccountInfoViewModel AccountInfo { get; }

	public bool IsLoading => _isLoading.Value;

	public ICommand GetServers { get; }

	public string FilterQuery
	{
		get
		{
			return _filterQuery;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<LocationsPageViewModel, string>(this, ref _filterQuery, value, "FilterQuery");
		}
	}

	public IEnumerable<VpnServerViewModel> VpnServers => _bindingData;

	public IObservable<(Exception ex, string source)> ExceptionObservable { get; }

	public LocationsPageViewModel(VpnServerRepository serverRepository, AccountInfoViewModel accountInfoViewModel, AuthDialogViewModel authDialogViewModel, DialogController dialogController, Settings settings, VpnServerViewModel.Factory vpnServerFactory, INavigationService navigation)
	{
		LocationsPageViewModel locationsPageViewModel = this;
		if (accountInfoViewModel == null)
		{
			throw new ArgumentNullException("accountInfoViewModel");
		}
		if (authDialogViewModel == null)
		{
			throw new ArgumentNullException("authDialogViewModel");
		}
		if (vpnServerFactory == null)
		{
			throw new ArgumentNullException("vpnServerFactory");
		}
		if (navigation == null)
		{
			throw new ArgumentNullException("navigation");
		}
		_serverRepository = serverRepository ?? throw new ArgumentNullException("serverRepository");
		_dialogController = dialogController ?? throw new ArgumentNullException("dialogController");
		_settings = settings ?? throw new ArgumentNullException("settings");
		AccountInfo = accountInfoViewModel;
		ReactiveCommand<Unit, List<VpnServerDto>> val = ReactiveCommand.CreateFromTask<List<VpnServerDto>>((Func<CancellationToken, Task<List<VpnServerDto>>>)GetServersAsync, (IObservable<bool>)null, (IScheduler)null);
		ObservableExtensions.Subscribe<List<VpnServerDto>>(Observable.ObserveOn<List<VpnServerDto>>((IObservable<List<VpnServerDto>>)val, RxSchedulers.TaskpoolScheduler), (Action<List<VpnServerDto>>)delegate(List<VpnServerDto> servers)
		{
			try
			{
				if (servers != null)
				{
					if (locationsPageViewModel._sourceList.Items.Count > 0)
					{
						bool flag = false;
						foreach (VpnServerDto server in servers)
						{
							if (!locationsPageViewModel._sourceList.Items.Contains(server, locationsPageViewModel._serverComparer))
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							foreach (VpnServerDto item in locationsPageViewModel._sourceList.Items)
							{
								if (!servers.Contains(item, locationsPageViewModel._serverComparer))
								{
									flag = true;
									break;
								}
							}
						}
						if (!flag)
						{
							return;
						}
					}
					locationsPageViewModel._sourceList.Edit((Action<IExtendedList<VpnServerDto>>)delegate(IExtendedList<VpnServerDto> list)
					{
						((ICollection<VpnServerDto>)list).Clear();
						list.AddRange((IEnumerable<VpnServerDto>)servers);
					});
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to process servers");
			}
		}, (Action<Exception>)delegate(Exception ex)
		{
			Log.Error(ex, "Failed to get servers");
		});
		ReactiveCommandMixins.InvokeCommand<Unit, List<VpnServerDto>>(Observable.Merge<Unit>(Observable.Select<bool, Unit>(Observable.Where<bool>(accountInfoViewModel.TokenValidated, (Func<bool, bool>)((bool valid) => valid)), (Func<bool, Unit>)((bool _) => Unit.Default)), authDialogViewModel.ActivatedObservable), (ReactiveCommandBase<Unit, List<VpnServerDto>>)(object)val);
		SortExpressionComparer<VpnServerViewModel> val2 = SortExpressionComparer<VpnServerViewModel>.Descending((Func<VpnServerViewModel, IComparable>)((VpnServerViewModel t) => t.IsFavorite)).ThenByDescending((Func<VpnServerViewModel, IComparable>)((VpnServerViewModel t) => t.IsBestServer)).ThenByAscending((Func<VpnServerViewModel, IComparable>)((VpnServerViewModel t) => t.RequiresUpgrade))
			.ThenByDescending((Func<VpnServerViewModel, IComparable>)((VpnServerViewModel t) => t.Category))
			.ThenByAscending((Func<VpnServerViewModel, IComparable>)((VpnServerViewModel t) => t.CountryName))
			.ThenByAscending((Func<VpnServerViewModel, IComparable>)((VpnServerViewModel t) => t.CityName));
		IObservable<Func<VpnServerViewModel, bool>> observable = Observable.Select<string, Func<VpnServerViewModel, bool>>(WhenAnyMixin.WhenAnyValue<LocationsPageViewModel, string>(this, (Expression<Func<LocationsPageViewModel, string>>)((LocationsPageViewModel x) => x.FilterQuery)), (Func<string, Func<VpnServerViewModel, bool>>)delegate(string query)
		{
			string lowerQuery = query?.ToLowerInvariant();
			return Func;
			bool Func(VpnServerViewModel server)
			{
				return string.IsNullOrEmpty(lowerQuery) || server.CountryName.ToLowerInvariant().Contains(lowerQuery) || (server.VpnServer.City != null && server.VpnServer.City.ToLowerInvariant().Contains(lowerQuery));
			}
		});
		IDisposable disposable = ObservableExtensions.Subscribe<IChangeSet<VpnServerViewModel>>(ObservableListEx.DisposeMany<VpnServerViewModel>(ObservableListEx.Bind<VpnServerViewModel>(Observable.ObserveOn<IChangeSet<VpnServerViewModel>>(ObservableListEx.Sort<VpnServerViewModel>(ObservableListEx.Filter<VpnServerViewModel>(ObservableListEx.AutoRefresh<VpnServerViewModel>(ObservableListEx.Transform<VpnServerDto, VpnServerViewModel>(_sourceList.Connect((Func<VpnServerDto, bool>)null), (Func<VpnServerDto, VpnServerViewModel>)((VpnServerDto t) => vpnServerFactory(t, delegate
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (!t.RequiresUpgrade)
				{
					navigation.NavigateTo(NavigationIntent.MainPage, t);
				}
				else
				{
					accountInfoViewModel.ManageSubscription.Execute(Unit.Default);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to select server");
			}
		})), false), (TimeSpan?)null, (TimeSpan?)null, (IScheduler)null), observable, (ListFilterPolicy)1), (IComparer<VpnServerViewModel>)val2, (SortOptions)0, (IObservable<Unit>)null, (IObservable<IComparer<VpnServerViewModel>>)null, 50), RxSchedulers.MainThreadScheduler), ref _bindingData, 25)));
		GetServers = (ICommand)val;
		IObservable<bool> observable2 = Observable.StartWith<bool>(ObservableListEx.QueryWhenChanged<VpnServerDto, bool>(_sourceList.Connect((Func<VpnServerDto, bool>)null), (Func<IReadOnlyCollection<VpnServerDto>, bool>)((IReadOnlyCollection<VpnServerDto> q) => q.Count == 0)), new bool[1] { true });
		_isLoading = OAPHCreationHelperMixin.ToProperty<LocationsPageViewModel, bool>(Observable.ObserveOn<bool>(Observable.CombineLatest<bool, bool, bool>(((ReactiveCommandBase<Unit, List<VpnServerDto>>)(object)val).IsExecuting, observable2, (Func<bool, bool, bool>)((bool executing, bool empty) => executing && empty)), RxSchedulers.MainThreadScheduler), this, (Expression<Func<LocationsPageViewModel, bool>>)((LocationsPageViewModel x) => x.IsLoading), false, (IScheduler)null);
		ExceptionObservable = Observable.RefCount<(Exception, string)>(Observable.Publish<(Exception, string)>(Observable.Select<Exception, (Exception, string)>(((ReactiveCommandBase<Unit, List<VpnServerDto>>)(object)val).ThrownExceptions, (Func<Exception, (Exception, string)>)((Exception ex) => (ex: ex, "getServers")))));
	}

	private async Task<List<VpnServerDto>?> GetServersAsync(CancellationToken ct)
	{
		if (!_settings.IsUserTokenValid)
		{
			Log.Warning("Cannot fetch servers, user token is not valid. Go to Settings to manage your token.");
			return null;
		}
		try
		{
			return await _serverRepository.GetServersAsync(ct);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to get servers");
			throw;
		}
	}
}
