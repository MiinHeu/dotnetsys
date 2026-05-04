using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VinhKhanh.App.Services;

namespace VinhKhanh.App.ViewModels;

public partial class PassportViewModel : ObservableObject
{
    private readonly ILocalDbService _db;

    public PassportViewModel(ILogger<PassportViewModel> logger, ILocalDbService db)
    {
        _db = db;
    }

    public ObservableCollection<StampItem> Stamps { get; } = new();

    [ObservableProperty]
    private string _progressText = "0/0 địa điểm";

    [RelayCommand]
    public async Task RefreshAsync()
    {
        try
        {
            var allPois = await _db.GetPoisAsync();
            var visitedIds = await _db.GetVisitedPoiIdsAsync();

            Stamps.Clear();
            foreach (var poi in allPois.OrderBy(p => p.Priority).ThenBy(p => p.Name))
            {
                Stamps.Add(new StampItem
                {
                    PoiId = poi.Id,
                    Name = poi.Name,
                    IsVisited = visitedIds.Contains(poi.Id)
                });
            }

            ProgressText = $"{visitedIds.Count}/{allPois.Count} địa điểm";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PassportViewModel] Error: {ex.Message}");
        }
    }
}

public class StampItem
{
    public int PoiId { get; set; }
    public string Name { get; set; } = "";
    public bool IsVisited { get; set; }
}
