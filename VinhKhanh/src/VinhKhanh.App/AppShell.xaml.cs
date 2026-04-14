using CommunityToolkit.Mvvm.Messaging;
using VinhKhanh.App.Models;
using VinhKhanh.App.Resources.Strings;

namespace VinhKhanh.App;

public partial class AppShell : Shell, IRecipient<LanguageChangedMessage>
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(PoiDetailPage), typeof(PoiDetailPage));
		WeakReferenceMessenger.Default.Register(this);
	}

	public void Receive(LanguageChangedMessage message)
	{
		TabMap.Title = AppResources.TabMap;
		TabPois.Title = AppResources.TabPois;
		TabTours.Title = AppResources.TabTours;
		TabChat.Title = AppResources.TabChat;
		TabScan.Title = AppResources.TabScan;
		TabSettings.Title = AppResources.TabSettings;
	}
}
