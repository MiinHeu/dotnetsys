using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Models;
using VinhKhanh.App.Services;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.ViewModels;

public partial class ToursViewModel : ObservableObject
{
	private readonly ApiClientService _api;
	private readonly ILocalDbService _localDb;
	private readonly MainViewModel _mainVm;

	public ToursViewModel(ApiClientService api, ILocalDbService localDb, MainViewModel mainVm)
	{
		_api = api;
		_localDb = localDb;
		_mainVm = mainVm;
	}


	public ObservableCollection<Tour> Tours { get; } = new();

	[ObservableProperty] private string _lang = "vi";
	[ObservableProperty] private string _status = "";
	[ObservableProperty] private bool _isBusy;

	[RelayCommand]
	public async Task LoadAsync()
	{
		if (IsBusy) return;
		IsBusy = true;

		try
		{
			// 1. Load from Local DB first
			var local = await _localDb.GetToursAsync();
			ReplaceTours(local);

			// 2. Fetch from Remote
			var language = string.IsNullOrWhiteSpace(Lang) ? "vi" : Lang.Trim().ToLowerInvariant();
			var remoteSnapshots = await _api.GetToursAsync(language);

			if (remoteSnapshots.Count > 0)
			{
				var remoteEntities = remoteSnapshots.Select(t => new Tour
				{
					Id = t.Id,
					Name = t.Name,
					Description = t.Description,
					EstimatedMinutes = t.EstimatedMinutes,
					IsActive = true
				}).ToList();

				await _localDb.SaveToursAsync(remoteEntities);

				// Luôn đồng bộ các Stops cho từng Tour để vẽ bản đồ
				foreach (var t in remoteSnapshots)
				{
					if (t.Stops != null)
					{
						await _localDb.SaveTourStopsAsync(t.Id, t.Stops.Select(s => new TourStop
						{
							TourId = t.Id,
							PoiId = s.Poi?.Id ?? 0,
							StopOrder = s.StopOrder
						}).ToList());
					}
				}

				ReplaceTours(remoteEntities);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[ToursViewModel] Load Error: {ex}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void ReplaceTours(IReadOnlyList<Tour> list)
	{
		Tours.Clear();
		foreach (var t in list.OrderBy(x => x.Name))
			Tours.Add(t);
	}

	[RelayCommand]
	private async Task SelectTour(Tour tour)
	{
		if (tour == null) return;
		
		// Map back to snapshot for MainViewModel (or update MainViewModel to use Tour)
		var snapshot = new TourSnapshot 
		{ 
			Id = tour.Id, 
			Name = tour.Name, 
			Description = tour.Description, 
			EstimatedMinutes = tour.EstimatedMinutes,
			Stops = [] 
		};

		_mainVm.SelectedTour = snapshot;
		await Shell.Current.GoToAsync("//MainPage");
	}
}
