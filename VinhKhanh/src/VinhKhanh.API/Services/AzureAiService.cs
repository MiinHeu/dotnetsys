using System.Text.Json;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Services;

public class AzureAiService(IConfiguration cfg) : IAiService
{
	public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
	{
		var endpoint = cfg["AzureOpenAI:Endpoint"]!;
		var apiKey = cfg["AzureOpenAI:Key"]!;
		var deployment = cfg["AzureOpenAI:Deployment"] ?? "gpt-4o-mini";

		var http = new HttpClient();
		http.DefaultRequestHeaders.Add("api-key", apiKey);

		var messages = new List<object>
		{
			new { role = "system", content = system }
		};

		if (history != null)
		{
			foreach (var h in history)
			{
				messages.Add(new { role = h.Role, content = h.Content });
			}
		}

		messages.Add(new { role = "user", content = user });

		var body = new
		{
			messages,
			max_tokens = 800,
			temperature = 0.7
		};

		var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-01";
		var resp = await http.PostAsJsonAsync(url, body);
		
		if (!resp.IsSuccessStatusCode)
			return "Xin loi, Azure AI dang bao tri.";

		var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
		return json
			.GetProperty("choices")[0]
			.GetProperty("message")
			.GetProperty("content")
			.GetString() ?? "";
	}
}
