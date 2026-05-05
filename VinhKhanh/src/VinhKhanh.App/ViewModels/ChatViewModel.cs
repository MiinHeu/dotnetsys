using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VinhKhanh.App.Services;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.App.ViewModels;

public partial class ChatViewModel : ObservableObject
{
	private readonly ApiClientService _api;

	public ChatViewModel(ApiClientService api)
	{
		_api = api;
		// Initial greeting from Mascot
		Messages.Add(new ChatMessage { Content = "Chào bạn! Mình là bé Vinh đây, trợ lý AI du lịch của phố Vĩnh Khánh. Bạn muốn hỏi gì về ẩm thực hay đường đi hôm nay?", IsUser = false });
	}

	public ObservableCollection<ChatMessage> Messages { get; } = new();

	[ObservableProperty] private string _currentMessage = "";
	[ObservableProperty] private string _lang = "vi";
	[ObservableProperty] private bool _isBusy;

	[RelayCommand]
	private async Task SendAsync()
	{
		if (string.IsNullOrWhiteSpace(CurrentMessage)) return;
		if (IsBusy) return;

		var q = CurrentMessage.Trim();
		CurrentMessage = "";
		Messages.Add(new ChatMessage { Content = q, IsUser = true });
		
		IsBusy = true;
		try
		{
			var reply = await _api.ChatAsync(new ChatRequest(q, Lang));
			Messages.Add(new ChatMessage { Content = string.IsNullOrWhiteSpace(reply) ? "Bé Vinh hiện đang bận một chút, bạn thử lại sau nhé!" : reply, IsUser = false });
		}
		catch (Exception ex)
		{
			Messages.Add(new ChatMessage { Content = $"Rất tiếc, đã có lỗi xảy ra: {ex.Message}", IsUser = false });
		}
		finally
		{
			IsBusy = false;
		}
	}
}
