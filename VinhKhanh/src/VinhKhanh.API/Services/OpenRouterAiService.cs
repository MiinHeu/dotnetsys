using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Services;

public class OpenRouterAiService(
    IConfiguration cfg,
    IHttpClientFactory httpClientFactory,
    ILogger<OpenRouterAiService> logger) : IAiService
{
    private readonly string? _apiKey = cfg["OpenRouter:ApiKey"];
    private readonly string _model = string.IsNullOrWhiteSpace(cfg["OpenRouter:Model"]) ? "google/gemini-2.0-flash-exp:free" : cfg["OpenRouter:Model"];

    public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Xin lỗi, cấu hình OpenRouter AI đang bị thiếu.";
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
            temperature = 0.7
        };

        try
        {
            using var http = httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey.Trim());
            
            // OpenRouter requires these headers for some models
            http.DefaultRequestHeaders.Add("HTTP-Referer", "https://vinhkhanh.azurewebsites.net"); 
            http.DefaultRequestHeaders.Add("X-Title", "Vinh Khanh Food Street");

            using var response = await http.PostAsJsonAsync("https://openrouter.ai/api/v1/chat/completions", payload);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                logger.LogError("OpenRouter Error: {Err}", err);
                return $"Bé Vinh (OpenRouter) lỗi: {response.StatusCode}";
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("choices")[0]
                         .GetProperty("message")
                         .GetProperty("content")
                         .GetString()?.Trim() ?? "...";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenRouter Chat failed");
            return "Lỗi kết nối OpenRouter AI.";
        }
    }
}
