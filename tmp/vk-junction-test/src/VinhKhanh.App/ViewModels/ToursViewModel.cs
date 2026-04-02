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

	public ToursViewModel(ApiClientService api, ILocalDbService localDb)
	{
		_api = api;
		_localDb = localDb;
	}

	public ObservableCollection<TourSnapshot> Tours { get; } = new();

	[ObservableProperty] private string _lang = "vi";
	[ObservableProperty] private string _status = "";
	[ObservableProperty] private bool _isBusy;

	[RelayCommand]
	public async Task LoadAsync()
	{
		if (IsBusy) return;
		IsBusy = true;
		Status = "Dang tai lo trinh...";

		try
		{
			var language = string.IsNullOrWhiteSpace(Lang) ? "vi" : Lang.Trim().ToLowerInvariant();
			var remote = await _api.GetToursAsync(language);

			if (remote.Count > 0)
			{
				ReplaceTours(remote);
				await SaveToursToLocalCacheAsync(remote);
				Status = $"Da tai {Tours.Count} lo trinh.";
			}
			else
			{
				await LoadFromLocalAsync("API tra ve rong");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine(ex);
			await LoadFromLocalAsync("Loi ket noi");
		}
		finally
		{
			IsBusy = false;
		}
	}

	private void ReplaceTours(IReadOnlyList<TourSnapshot> list)
	{
		Tours.Clear();
		foreach (var t in list.OrderBy(x => x.Name))
			Tours.Add(t);
	}

	private async Task SaveToursToLocalCacheAsync(IReadOnlyList<TourSnapshot> list)
	{
		var localModels = list.Select(t => new Tour
		{
			Id = t.Id,
			Name = t.Name,
			Description = t.Description,
			EstimatedMinutes = t.EstimatedMinutes,
			IsActive = true
		}).ToList();

		await _localDb.SaveToursAsync(localModels);
	}

	private async Task LoadFromLocalAsync(string reason)
	{
		var local = await _localDb.GetToursAsync();
		var snapshots = local.Select(t => new TourSnapshot
		{
			Id = t.Id,
			Name = t.Name,
			Description = t.Description,
			EstimatedMinutes = t.EstimatedMinutes,
			Stops = []
		}).ToList();

		ReplaceTours(snapshots);
		Status = snapshots.Count > 0
			? $"{reason}: dang dung {snapshots.Count} lo trinh offline."
			: $"{reason}: chua co du lieu offline.";
	}
}
