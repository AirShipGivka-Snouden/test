using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Windows.Input;
using Ciphra.VPN.Common.App;
using Ciphra.VPN.Common.Models;
using Ciphra.VPN.Common.Services;
using ReactiveUI;

namespace Ciphra.VPN.Common.ViewModels.DataViewModels;

public class VpnServerViewModel : ReactiveObject
{
	public delegate VpnServerViewModel Factory(VpnServerDto vpnServerDto, Action selectAction);

	private readonly IImageUriFormatProvider _imageUriFormatProvider;

	private bool _isFavorite;

	private string _countryName;

	private string _cityName;

	private string _flagIconUri;

	private ServerCategory _category;

	private bool _requiresUpgrade;

	public string CountryName
	{
		get
		{
			return _countryName;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<VpnServerViewModel, string>(this, ref _countryName, value, "CountryName");
		}
	}

	public string CityName
	{
		get
		{
			return _cityName;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<VpnServerViewModel, string>(this, ref _cityName, value, "CityName");
		}
	}

	public string FlagIconUri
	{
		get
		{
			return _flagIconUri;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<VpnServerViewModel, string>(this, ref _flagIconUri, value, "FlagIconUri");
		}
	}

	public bool RequiresUpgrade
	{
		get
		{
			return _requiresUpgrade;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<VpnServerViewModel, bool>(this, ref _requiresUpgrade, value, "RequiresUpgrade");
		}
	}

	public ServerCategory Category
	{
		get
		{
			return _category;
		}
		private set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<VpnServerViewModel, ServerCategory>(this, ref _category, value, "Category");
		}
	}

	public VpnServerDto VpnServer { get; private set; }

	public ICommand Select { get; }

	public ICommand ToggleFavoriteCommand { get; }

	public bool IsBestServer => string.Equals(VpnServer?.CountryIso, "best", StringComparison.OrdinalIgnoreCase);

	public bool IsFavorite
	{
		get
		{
			return _isFavorite;
		}
		set
		{
			IReactiveObjectExtensions.RaiseAndSetIfChanged<VpnServerViewModel, bool>(this, ref _isFavorite, value, "IsFavorite");
		}
	}

	public VpnServerViewModel(VpnServerDto vpnServerDto, Action selectAction, VpnServerFavoritesRepository favoritesRepository, IImageUriFormatProvider imageUriFormatProvider, Lazy<IExceptionHandler> exceptionHandler)
	{
		VpnServerViewModel vpnServerViewModel = this;
		if (favoritesRepository == null)
		{
			throw new ArgumentNullException("favoritesRepository");
		}
		_imageUriFormatProvider = imageUriFormatProvider ?? throw new ArgumentNullException("imageUriFormatProvider");
		VpnServer = vpnServerDto ?? throw new ArgumentNullException("vpnServerDto");
		FlagIconUri = GetFlagIconUri(vpnServerDto.CountryIso);
		CountryName = GetCountryName(vpnServerDto.CountryIso);
		CityName = vpnServerDto.City ?? string.Empty;
		Category = (vpnServerDto.ServerCategory.HasValue ? ((ServerCategory)vpnServerDto.ServerCategory.Value) : ServerCategory.Free);
		RequiresUpgrade = vpnServerDto.RequiresUpgrade;
		ReactiveCommand<Unit, Unit> val = ReactiveCommand.Create(selectAction, (IObservable<bool>)null, (IScheduler)null);
		ToggleFavoriteCommand = (ICommand)ReactiveCommand.Create<bool>((Func<bool>)(() => vpnServerViewModel.IsFavorite = !vpnServerViewModel.IsFavorite), (IObservable<bool>)null, (IScheduler)null);
		if (!string.IsNullOrEmpty(vpnServerDto.Id))
		{
			IsFavorite = favoritesRepository.IsFavorite(vpnServerDto.Id);
			ObservableExtensions.Subscribe<bool>(WhenAnyMixin.WhenAnyValue<VpnServerViewModel, bool>(this, (Expression<Func<VpnServerViewModel, bool>>)((VpnServerViewModel x) => x.IsFavorite)), (Action<bool>)delegate(bool isFavorite)
			{
				if (isFavorite)
				{
					favoritesRepository.AddFavorite(vpnServerDto.Id);
				}
				else
				{
					favoritesRepository.RemoveFavorite(vpnServerDto.Id);
				}
			});
		}
		ObservableExtensions.Subscribe<Exception>(((ReactiveCommandBase<Unit, Unit>)(object)val).ThrownExceptions, (Action<Exception>)delegate(Exception ex)
		{
			exceptionHandler.Value.HandleException(ex, "VpnServerViewModel", fatal: false);
		});
		Select = (ICommand)val;
	}

	public VpnServerViewModel(Action selectAction)
	{
		Select = (ICommand)ReactiveCommand.Create(selectAction, (IObservable<bool>)null, (IScheduler)null);
		ToggleFavoriteCommand = (ICommand)ReactiveCommand.Create<bool>((Func<bool>)(() => IsFavorite = !IsFavorite), (IObservable<bool>)null, (IScheduler)null);
		FlagIconUri = string.Empty;
		CountryName = "No server selected";
		CityName = string.Empty;
		Category = ServerCategory.Free;
		RequiresUpgrade = false;
	}

	private string GetFlagIconUri(string? countryIso)
	{
		if (string.IsNullOrEmpty(countryIso))
		{
			return string.Empty;
		}
		return _imageUriFormatProvider.GetImageUri(countryIso);
	}

	private static string GetCountryName(string? countryIso)
	{
		if (string.IsNullOrWhiteSpace(countryIso))
		{
			return string.Empty;
		}
		if (string.Equals(countryIso, "best", StringComparison.OrdinalIgnoreCase))
		{
			return "Best Server";
		}
		try
		{
			RegionInfo regionInfo = new RegionInfo(countryIso.Trim().ToUpperInvariant());
			return regionInfo.EnglishName;
		}
		catch (ArgumentException)
		{
			RegionInfo regionInfo2 = (from c in CultureInfo.GetCultures(CultureTypes.SpecificCultures)
				select new RegionInfo(c.Name)).FirstOrDefault((RegionInfo r) => string.Equals(r.TwoLetterISORegionName, countryIso, StringComparison.OrdinalIgnoreCase));
			if (regionInfo2 != null)
			{
				return regionInfo2.EnglishName;
			}
			return countryIso.Trim().ToUpperInvariant();
		}
	}
}
