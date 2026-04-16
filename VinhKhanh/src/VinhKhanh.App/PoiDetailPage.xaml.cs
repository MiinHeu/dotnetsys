using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class PoiDetailPage : ContentPage
{
	public PoiDetailPage(PoiDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
