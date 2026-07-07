namespace Ciphra.VPN.Common.ViewModels;

public static class AccountInfoExtensions
{
	public static string ToFriendlyString(this AccountInfoViewModel.SubscriptionStatus status)
	{
		if (1 == 0)
		{
		}
		string result = status switch
		{
			AccountInfoViewModel.SubscriptionStatus.Inactive => "Inactive", 
			AccountInfoViewModel.SubscriptionStatus.Proxy => "Proxy", 
			AccountInfoViewModel.SubscriptionStatus.Active => "Active", 
			AccountInfoViewModel.SubscriptionStatus.Expired => "Expired", 
			_ => "Unknown", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public static string ToFriendlyString(this AccountInfoViewModel.AccountInfoState status)
	{
		if (1 == 0)
		{
		}
		string result = status switch
		{
			AccountInfoViewModel.AccountInfoState.Initializing => string.Empty, 
			AccountInfoViewModel.AccountInfoState.CheckingLicense => "Checking license...", 
			AccountInfoViewModel.AccountInfoState.InvalidToken => "Invalid user token", 
			AccountInfoViewModel.AccountInfoState.TokenRejected => "Token rejected", 
			AccountInfoViewModel.AccountInfoState.NetworkError => "License check failed", 
			AccountInfoViewModel.AccountInfoState.Error => "License validation error", 
			AccountInfoViewModel.AccountInfoState.TrialActive => "Trial version", 
			AccountInfoViewModel.AccountInfoState.TrialExpired => "Trial expired", 
			AccountInfoViewModel.AccountInfoState.Completed => string.Empty, 
			_ => string.Empty, 
		};
		if (1 == 0)
		{
		}
		return result;
	}
}
