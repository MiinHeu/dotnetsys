using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Services;

public class OllamaAiService(HttpClient httpClient, IConfiguration cfg) : IAiService
{
	private readonly string _baseUrl = cfg["Ollama:BaseUrl"] ?? "http://localhost:11434";
	private readonly string _model = cfg["Ollama:Model"] ?? "llama3.2:3b";

	public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
	{
		// Ollama compatible API (/api/chat) expects:
		// { model, messages: [{role, content}], stream: false }
		var messages = new List<object> { new { role = "system", content = system } };

		if (history != null)
		{
			foreach (var h in history)
				messages.Add(new { role = h.Role, content = h.Content });
		}

		messages.Add(new { role = "user", content = user });

		var payload = new
		{
			model = _model,
			messages,
			stream = false
		};

		var res = await httpClient.PostAsJsonAsync($"{_baseUrl}/api/chat", payload);
		if (!res.IsSuccessStatusCode)
			return "Xin loi, AI dang bao tri.";

		// Expected: { message: { content: "..." }, ... }
		var json = await res.Content.ReadFromJsonAsync<JsonElement>();
		if (!json.TryGetProperty("message", out var message))
			return "AI tra ve du lieu khong hop le.";

		if (!message.TryGetProperty("content", out var content))
			return "AI khong co noi dung phan hoi.";

		return content.GetString() ?? "";
	}
}

