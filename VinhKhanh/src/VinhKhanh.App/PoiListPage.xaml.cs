using System.Collections.ObjectModel;
using VinhKhanh.App.Services;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App;

public partial class PoiListPage : ContentPage
{
    private readonly ILocalDbService _db;
    private readonly ApiClientService _api;
    private List<Poi> _allPois = new();
    public ObservableCollection<Poi> FilteredPois { get; } = new();

    public PoiListPage()
    {
        InitializeComponent();
        _db = MauiProgram.Services.GetRequiredService<ILocalDbService>();
        _api = MauiProgram.Services.GetRequiredService<ApiClientService>();
        
        BindingContext = this;
        PoisCollectionView.ItemsSource = FilteredPois;

        Loaded += async (_, _) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        if (MainRefreshView.IsRefreshing) return;

        try
        {
            MainRefreshView.IsRefreshing = true;
            
            // 1. Load from Local first (Fast)
            var localPois = await _db.GetPoisAsync();
            _allPois = localPois.OrderByDescending(p => p.Priority).ToList();
            
            // 2. Refresh UI
            ApplyFilter(SearchEntry.Text);

            try 
            {
                var lang = Microsoft.Maui.Storage.Preferences.Get(AppPreferences.UiLanguage, "vi");
                var remoteSnapshots = await _api.GetPoisAsync(lang);
                if (remoteSnapshots.Count > 0)
                {
                    // Map snapshots to entities and save
                    var entities = remoteSnapshots.Select(s => new Poi
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        Category = Enum.TryParse<PoiCategory>(s.Category, out var cat) ? cat : PoiCategory.ComTam,
                        Priority = s.Priority,
                        ImageUrl = s.ImageUrl,
                        Address = s.Address,
                        PhoneNumber = s.PhoneNumber,
                        Rating = s.Rating,
                        IsActive = true
                    }).ToList();

                    await _db.SavePoisAsync(entities);
                    
                    // Update list if there are changes
                    _allPois = entities.OrderByDescending(p => p.Priority).ToList();
                    ApplyFilter(SearchEntry.Text);
                }
            }
            catch { /* Ignore API errors during background sync */ }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PoiListPage] Error: {ex}");
            await DisplayAlert("Lỗi dữ liệu", ex.Message, "OK");
        }
        finally
        {
            MainRefreshView.IsRefreshing = false;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(e.NewTextValue);
    }

    private void ApplyFilter(string filter)
    {
        FilteredPois.Clear();
        var results = string.IsNullOrWhiteSpace(filter) 
            ? _allPois 
            : _allPois.Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) 
                                  || p.Category.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase));

        foreach (var p in results)
        {
            FilteredPois.Add(p);
        }
    }

    private async void OnPoiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Poi selected)
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "PoiId", selected.Id }
            };

            await Shell.Current.GoToAsync(nameof(PoiDetailPage), navigationParameter);
        }
    }

    private async void OnReloadClicked(object sender, EventArgs e)
    {
        await LoadDataAsync();
    }
}
