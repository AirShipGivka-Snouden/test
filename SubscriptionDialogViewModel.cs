using System;

namespace Ciphra.VPN.Common.ViewModels.DialogViewModels;

public class SubscriptionDialogViewModel
{
	public SubscriptionManager SubscriptionManager { get; }

	public AccountInfoViewModel AccountInfo { get; }

	public SubscriptionDialogViewModel(SubscriptionManager subscriptionManager, AccountInfoViewModel accountInfo)
	{
		SubscriptionManager = subscriptionManager ?? throw new ArgumentNullException("subscriptionManager");
		AccountInfo = accountInfo ?? throw new ArgumentNullException("accountInfo");
	}
}
