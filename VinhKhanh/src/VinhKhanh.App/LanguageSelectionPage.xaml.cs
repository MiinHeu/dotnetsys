using System.Globalization;
using Microsoft.Maui.Controls;
using VinhKhanh.App.Services;

namespace VinhKhanh.App;

public partial class LanguageSelectionPage : ContentPage
{
    public LanguageSelectionPage()
    {
        InitializeComponent();
    }

    private void OnLangSelected(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string langCode)
        {
            // Set user preference
            Microsoft.Maui.Storage.Preferences.Set(AppPreferences.UiLanguage, langCode);
            Microsoft.Maui.Storage.Preferences.Set(AppPreferences.HasSelectedLanguage, true);

            // Change thread culture
            var culture = new CultureInfo(langCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // Transition to main app structure
            Application.Current.MainPage = new AppShell();
        }
    }
}
