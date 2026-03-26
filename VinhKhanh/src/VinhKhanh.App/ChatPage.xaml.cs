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
		ChatList.ItemsSource = _vm.Lines;
		_vm.Lines.CollectionChanged += OnLinesChanged;
		MsgEntry.SetBinding(Entry.TextProperty, new Binding(nameof(ChatViewModel.Input), source: _vm));
	}

	private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (_vm.Lines.Count > 0)
			ChatList.ScrollTo(_vm.Lines[^1], position: ScrollToPosition.End, animate: true);
	}

	private async void OnSend(object? sender, EventArgs e)
		=> await _vm.SendCommand.ExecuteAsync(null);
}
