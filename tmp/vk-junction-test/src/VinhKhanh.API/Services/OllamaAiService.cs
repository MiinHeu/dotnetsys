using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Services;

public class OllamaAiService(HttpClient httpClient, IConfiguration cfg, ILogger<OllamaAiService> logger) : IAiService
{
	private readonly string _baseUrl = cfg["Ollama:BaseUrl"] ?? "http://localhost:11434";
	private readonly string _model = cfg["Ollama:Model"] ?? "llama3.2:3b";

	public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
	{
		if (string.IsNullOrWhiteSpace(user))
			return "Vui long nhap noi dung can hoi.";

		var messages = BuildMessages(system, user, history);
		var payload = new
		{
			model = _model,
			messages,
			stream = false
		};

		var url = $"{_baseUrl.TrimEnd('/')}/api/chat";

		try
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
			var res = await httpClient.PostAsJsonAsync(url, payload, cts.Token);
			if (!res.IsSuccessStatusCode)
			{
				var body = await res.Content.ReadAsStringAsync(cts.Token);
				logger.LogWarning("Ollama API failed. Status={StatusCode}, Body={Body}", (int)res.StatusCode, body);
				return "Xin loi, AI tam thoi ban. Vui long thu lai sau.";
			}

			var json = await res.Content.ReadFromJsonAsync<JsonElement>(cts.Token);
			if (!json.TryGetProperty("message", out var message))
				return "AI tra ve du lieu khong hop le.";

			if (!message.TryGetProperty("content", out var content))
				return "AI khong co noi dung phan hoi.";

			var text = content.GetString()?.Trim();
			return string.IsNullOrWhiteSpace(text)
				? "AI chua co cau tra loi ro rang."
				: text;
		}
		catch (OperationCanceledException)
		{
			return "AI phan hoi qua lau, vui long thu lai.";
		}
		catch (HttpRequestException ex)
		{
			logger.LogWarning(ex, "Cannot reach Ollama service at {Url}", url);
			return "Khong ket noi duoc AI engine. Vui long kiem tra dich vu AI.";
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unexpected error while calling Ollama service.");
			return "Co loi xay ra khi xu ly AI.";
		}
	}

	private static List<object> BuildMessages(string system, string user, List<MessageHistory>? history)
	{
		var messages = new List<object>();
		if (!string.IsNullOrWhiteSpace(system))
			messages.Add(new { role = "system", content = system.Trim() });

		if (history != null && history.Count > 0)
		{
			foreach (var h in history.TakeLast(10))
			{
				if (string.IsNullOrWhiteSpace(h.Content))
					continue;

				var role = NormalizeRole(h.Role);
				messages.Add(new { role, content = h.Content.Trim() });
			}
		}

		messages.Add(new { role = "user", content = user.Trim() });
		return messages;
	}

	private static string NormalizeRole(string? role)
	{
		if (string.IsNullOrWhiteSpace(role))
			return "user";

		var key = role.Trim().ToLowerInvariant();
		return key is "system" or "assistant" or "user" ? key : "user";
	}
}
