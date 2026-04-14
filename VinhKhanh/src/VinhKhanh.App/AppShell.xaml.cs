namespace VinhKhanh.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(PoiDetailPage), typeof(PoiDetailPage));
	}
}
