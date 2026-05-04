using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.App.ViewModels;

namespace VinhKhanh.App;

public partial class ChatPage : ContentPage
{
	private readonly ChatViewModel _vm;

	public ChatPage()
	{
		InitializeComponent();
		_vm = MauiProgram.Services.GetRequiredService<ChatViewModel>();
		BindingContext = _vm;
		LangPicker.ItemsSource = new[] { "vi", "en", "zh", "ko", "ja" };
		LangPicker.SelectedItem = _vm.Lang;
		LangPicker.SelectedIndexChanged += (_, _) =>
		{
			if (LangPicker.SelectedItem is string l) _vm.Lang = l;
		};
		ChatList.ItemsSource = _vm.Messages;
		_vm.Messages.CollectionChanged += OnLinesChanged;
	}

	private async void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (_vm.Messages.Count > 0)
		{
			var lastItem = _vm.Messages[^1];
			// Đợi một chút để MAUI CollectionView kịp render item mới trên Android
			await Task.Delay(50);
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					ChatList.ScrollTo(lastItem, position: ScrollToPosition.End, animate: false);
				}
				catch
				{
					// Bỏ qua lỗi nếu ScrollTo vẫn thất bại do UI chưa sẵn sàng
				}
			});
		}
	}

	private async void OnSend(object? sender, EventArgs e)
		=> await _vm.SendCommand.ExecuteAsync(null);
}
