using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class PassportPage : ContentPage
{
    private readonly PassportViewModel _vm;

    public PassportPage()
    {
        InitializeComponent();
        _vm = MauiProgram.Services.GetRequiredService<PassportViewModel>();
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshCommand.Execute(null);
    }
}
