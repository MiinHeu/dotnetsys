using Microsoft.Maui.Controls;

namespace VinhKhanh.App.Pages;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        // Mark onboarding as completed so it won't show again
        Preferences.Set("HasSeenOnboarding", true);

        // Xin quyền Location lịch sự trước khi vào app
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            await App.Current.MainPage.DisplayAlert("Xin phép sử dụng GPS", 
                "Ứng dụng cần quyền vị trí để có thể tự động phát âm thanh giới thiệu khi bạn đi ngang qua các quán ăn trên phố Vĩnh Khánh.", "Đồng ý");
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        // Xin quyền Microphone (nếu sau này Chatbot xài voice)
        var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (micStatus != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.Microphone>();
        }

        // Điều hướng sang AppShell (Giao diện chính)
        Application.Current.MainPage = new AppShell();
    }
}
