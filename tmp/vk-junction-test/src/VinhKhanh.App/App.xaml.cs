using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.Services;

namespace VinhKhanh.App;

public partial class App : Application
{
	// Giữ reference tránh GC collect — kick-start auto-sync khi WiFi kết nối lại
	private readonly ConnectivityService _connectivity;

	public App(ConnectivityService connectivity)
	{
		InitializeComponent();
		_connectivity = connectivity;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}