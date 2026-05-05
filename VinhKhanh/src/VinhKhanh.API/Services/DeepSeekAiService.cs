using System.Net.Http.Json;
using System.Text.Json;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Services;

public class DeepSeekAiService(
    IConfiguration cfg,
    IHttpClientFactory httpClientFactory,
    ILogger<DeepSeekAiService> logger) : IAiService
{
    private readonly string? _apiKey = cfg["DeepSeek:ApiKey"];
    private readonly string _model = string.IsNullOrWhiteSpace(cfg["DeepSeek:Model"]) ? "deepseek-chat" : cfg["DeepSeek:Model"];

    public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Xin lỗi, cấu hình DeepSeek AI đang bị thiếu.";
        }

        var messages = new List<object>
        {
            new { role = "system", content = system }
        };

        if (history != null)
        {
            foreach (var h in history.Where(x => !string.IsNullOrWhiteSpace(x.Content)))
            {
                // DeepSeek uses 'assistant' and 'user' roles
                messages.Add(new { role = h.Role?.ToLower() == "assistant" ? "assistant" : "user", content = h.Content });
            }
        }

        messages.Add(new { role = "user", content = user });

        var payload = new
        {
            model = _model,
            messages = messages,
            temperature = 0.7,
            max_tokens = 2048
        };

        try
        {
            using var http = httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey.Trim());

            using var response = await http.PostAsJsonAsync("https://api.deepseek.com/chat/completions", payload);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                logger.LogError("DeepSeek Error: {Err}", err);
                return $"Bé Vinh (DeepSeek) lỗi: {response.StatusCode}";
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("choices")[0]
                         .GetProperty("message")
                         .GetProperty("content")
                         .GetString()?.Trim() ?? "...";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeepSeek Chat failed");
            return "Lỗi kết nối DeepSeek AI.";
        }
    }
}
