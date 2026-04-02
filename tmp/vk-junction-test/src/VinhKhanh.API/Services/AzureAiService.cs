using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Services;

public class AzureAiService(
	IConfiguration cfg,
	IHttpClientFactory httpClientFactory,
	ILogger<AzureAiService> logger) : IAiService
{
	public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
	{
		if (string.IsNullOrWhiteSpace(user))
			return "Vui long nhap noi dung can hoi.";

		var endpoint = cfg["AzureOpenAI:Endpoint"];
		var apiKey = cfg["AzureOpenAI:Key"];
		var deployment = cfg["AzureOpenAI:Deployment"] ?? "gpt-4o-mini";

		if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
			return "Azure AI chua duoc cau hinh.";

		var messages = new List<object>
		{
			new { role = "system", content = system }
		};

		if (history != null)
		{
			foreach (var h in history.TakeLast(10))
			{
				if (string.IsNullOrWhiteSpace(h.Content))
					continue;

				messages.Add(new
				{
					role = NormalizeRole(h.Role),
					content = h.Content.Trim()
				});
			}
		}

		messages.Add(new { role = "user", content = user.Trim() });

		var body = new
		{
			messages,
			max_tokens = 800,
			temperature = 0.6
		};

		var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-01";

		try
		{
			using var http = httpClientFactory.CreateClient();
			http.DefaultRequestHeaders.Add("api-key", apiKey);
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
			var resp = await http.PostAsJsonAsync(url, body, cts.Token);

			if (!resp.IsSuccessStatusCode)
			{
				var err = await resp.Content.ReadAsStringAsync(cts.Token);
				logger.LogWarning("Azure AI failed. Status={StatusCode}, Body={Body}", (int)resp.StatusCode, err);
				return "Xin loi, Azure AI dang bao tri.";
			}

			var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cts.Token);
			if (!json.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
				return "Azure AI khong tra ve ket qua hop le.";

			var content = choices[0]
				.GetProperty("message")
				.GetProperty("content")
				.GetString();

			return string.IsNullOrWhiteSpace(content)
				? "Azure AI chua co cau tra loi phu hop."
				: content.Trim();
		}
		catch (OperationCanceledException)
		{
			return "Azure AI phan hoi qua lau, vui long thu lai.";
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Azure AI call failed unexpectedly.");
			return "Xin loi, Azure AI dang gap su co.";
		}
	}

	private static string NormalizeRole(string? role)
	{
		if (string.IsNullOrWhiteSpace(role))
			return "user";

		var key = role.Trim().ToLowerInvariant();
		return key is "system" or "assistant" or "user" ? key : "user";
	}
}
