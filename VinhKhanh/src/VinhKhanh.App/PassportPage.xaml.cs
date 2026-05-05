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

        Loaded += async (_, _) => await _vm.RefreshCommand.ExecuteAsync(null);
    }
}
