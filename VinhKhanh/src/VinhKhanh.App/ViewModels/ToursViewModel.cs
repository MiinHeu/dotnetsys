using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;

namespace VinhKhanh.App.ViewModels;

public partial class ToursViewModel : ObservableObject
{
	private readonly ApiClientService _api;

	public ToursViewModel(ApiClientService api) => _api = api;

	public ObservableCollection<TourSnapshot> Tours { get; } = new();

	[ObservableProperty] private string _lang = "vi";
	[ObservableProperty] private string _status = "";

	[RelayCommand]
	public async Task LoadAsync()
	{
		Status = "Đang tải...";
		try
		{
			var list = await _api.GetToursAsync(Lang);
			Tours.Clear();
			foreach (var t in list.OrderBy(x => x.Name))
				Tours.Add(t);
			Status = $"{Tours.Count} tuyến.";
		}
		catch (Exception ex)
		{
			Status = "Không tải được tour.";
			System.Diagnostics.Debug.WriteLine(ex);
		}
	}
}
