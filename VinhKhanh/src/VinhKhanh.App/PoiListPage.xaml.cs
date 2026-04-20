using System.Collections.ObjectModel;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;

namespace VinhKhanh.App;

public partial class PoiListPage : ContentPage
{
	private readonly LocalPoiCacheService _cache;
	private List<PoiSnapshot> _allPois = new();
	public ObservableCollection<PoiSnapshot> DisplayedPois { get; } = new();

	public PoiListPage(LocalPoiCacheService cache)
	{
		InitializeComponent();
		_cache = cache;
		BindingContext = this;
		PoiCollectionView.ItemsSource = DisplayedPois;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadDataAsync();
	}

	private async Task LoadDataAsync()
	{
		try
		{
			PoiRefreshView.IsRefreshing = true;
			var pois = await _cache.LoadPoisAsync();
			_allPois = pois.OrderByDescending(p => p.Priority).ToList();
			ApplyFilter(PoiSearchBar.Text);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Lỗi", "Không thể tải danh sách quán ăn.", "OK");
		}
		finally
		{
			PoiRefreshView.IsRefreshing = false;
		}
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilter(e.NewTextValue);
	}

	private void ApplyFilter(string filter)
	{
		DisplayedPois.Clear();
		var results = string.IsNullOrWhiteSpace(filter) 
			? _allPois 
			: _allPois.Where(p => p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) 
			                      || (p.Category?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));

		foreach (var p in results)
		{
			DisplayedPois.Add(p);
		}
	}

	private async void OnPoiTapped(object sender, TappedEventArgs e)
	{
		if (e.Parameter is PoiSnapshot selected)
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
