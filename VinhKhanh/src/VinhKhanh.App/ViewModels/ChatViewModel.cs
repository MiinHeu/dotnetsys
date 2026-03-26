using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Services;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	private readonly ApiClientService _api;

	public ChatViewModel(ApiClientService api) => _api = api;

	public ObservableCollection<string> Lines { get; } = new();

	[ObservableProperty] private string _input = "";
	[ObservableProperty] private string _lang = "vi";

	[RelayCommand]
	private async Task SendAsync()
	{
		if (string.IsNullOrWhiteSpace(Input)) return;
		var q = Input.Trim();
		Input = "";
		Lines.Add($"Bạn: {q}");
		try
		{
			var reply = await _api.ChatAsync(new ChatRequest(q, Lang));
			Lines.Add(string.IsNullOrWhiteSpace(reply) ? "AI: (không phản hồi)" : $"AI: {reply}");
		}
		catch (Exception ex)
		{
			Lines.Add($"Lỗi: {ex.Message}");
		}
	}
}
