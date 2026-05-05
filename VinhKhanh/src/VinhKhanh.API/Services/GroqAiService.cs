using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Services;

public class GroqAiService(
    IConfiguration cfg,
    IHttpClientFactory httpClientFactory,
    ILogger<GroqAiService> logger) : IAiService
{
    private readonly string? _apiKey = cfg["Groq:ApiKey"];
    private readonly string _model = string.IsNullOrWhiteSpace(cfg["Groq:Model"]) ? "llama-3.3-70b-versatile" : cfg["Groq:Model"];

    public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Xin lỗi, cấu hình Groq AI đang bị thiếu.";
        }

        var messages = new List<object>
        {
            new { role = "system", content = system }
        };

        if (history != null)
        {
            foreach (var h in history.Where(x => !string.IsNullOrWhiteSpace(x.Content)))
            {
                messages.Add(new { role = h.Role?.ToLower() == "assistant" ? "assistant" : "user", content = h.Content });
            }
        }

        messages.Add(new { role = "user", content = user });

        var payload = new
        {
            model = _model,
            messages = messages,
            temperature = 0.7,
            max_tokens = 1024
        };

        try
        {
            using var http = httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey.Trim());

            using var response = await http.PostAsJsonAsync("https://api.groq.com/openai/v1/chat/completions", payload);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                logger.LogError("Groq Error: {Err}", err);
                return $"Bé Vinh (Groq) lỗi: {response.StatusCode}";
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("choices")[0]
                         .GetProperty("message")
                         .GetProperty("content")
                         .GetString()?.Trim() ?? "...";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Groq Chat failed");
            return "Lỗi kết nối Groq AI.";
        }
    }
}
