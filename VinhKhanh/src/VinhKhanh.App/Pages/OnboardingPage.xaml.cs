using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace VinhKhanh.App.Pages;

public partial class OnboardingPage : ContentPage
{
    public ObservableCollection<OnboardingStep> Steps { get; set; }

    public OnboardingPage()
    {
        InitializeComponent();
        
        Steps = new ObservableCollection<OnboardingStep>
        {
            new OnboardingStep 
            { 
                Title = "Chào mừng tới Vĩnh Khánh", 
                Description = "Khám phá phố ẩm thực sầm uất nhất Sài Gòn với sự trợ giúp của công nghệ AI.",
                Icon = "🍜"
            },
            new OnboardingStep 
            { 
                Title = "Thuyết minh tự động", 
                Description = "Chỉ cần đeo tai nghe và đi dạo, ứng dụng sẽ tự động thuyết minh khi bạn đi ngang qua các quán ăn.",
                Icon = "🎧"
            },
            new OnboardingStep 
            { 
                Title = "Hộ chiếu du lịch", 
                Description = "Tích lũy tem kỹ thuật số khi ghé thăm các quán ăn để nhận những phần quà hấp dẫn.",
                Icon = "🎫"
            }
        };

        OnboardingCarousel.ItemsSource = Steps;
    }

    private void OnNextClicked(object sender, EventArgs e)
    {
        if (OnboardingCarousel.Position < Steps.Count - 1)
        {
            OnboardingCarousel.Position++;
        }
        else
        {
            OnStartClicked(sender, e);
        }
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        try 
        {
            // Mark onboarding as completed so it won't show again
            Preferences.Set("HasSeenOnboarding", true);

            // Xin quyền Location lịch sự trước khi vào app
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Xin phép sử dụng GPS", 
                    "Ứng dụng cần quyền vị trí để có thể tự động phát âm thanh giới thiệu khi bạn đi ngang qua các quán ăn trên phố Vĩnh Khánh.", "Đồng ý");
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            // Điều hướng sang trang tiếp theo an toàn trên Main Thread
            MainThread.BeginInvokeOnMainThread(() => {
                try 
                {
                    var hasSelectedLang = Preferences.Get(VinhKhanh.App.Services.AppPreferences.HasSelectedLanguage, false);
                    if (!hasSelectedLang)
                    {
                        Application.Current.MainPage = new LanguageSelectionPage();
                    }
                    else
                    {
                        Application.Current.MainPage = new AppShell();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CRITICAL] Navigation Failed: {ex}");
                    Application.Current.MainPage = new AppShell(); // Fallback
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CRITICAL] System Error: {ex}");
            await DisplayAlert("Thông báo", 
                "Rất tiếc, đã có sự cố xảy ra. Vui lòng kiểm tra kết nối mạng và thử lại.", "Đóng");
        }
    }
}

public class OnboardingStep
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
}
