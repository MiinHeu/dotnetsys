using System.Net.Http.Json;
using System.Text.Json;

namespace VinhKhanh.API.Services;

using VinhKhanh.Shared.DTOs;

public class GeminiAiService(
    IConfiguration cfg,
    IHttpClientFactory httpClientFactory,
    ILogger<GeminiAiService> logger) : IAiService
{
    private readonly string? _apiKey = cfg["Gemini:ApiKey"];
    private readonly string _model = string.IsNullOrWhiteSpace(cfg["Gemini:Model"]) ? "gemini-1.5-flash" : cfg["Gemini:Model"].Trim();

    public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.Contains("YOUR_GEMINI_API_KEY"))
        {
            logger.LogWarning("Gemini API Key is missing or invalid.");
            return "Xin lỗi, hiện tại tôi không thể trả lời được do thiếu cấu hình AI trên Server.";
        }

        var cleanKey = _apiKey.Trim();

        try
        {
            // Build contents for Gemini
            var contents = new List<object>();
            
            // Add history
            if (history != null)
            {
                foreach (var msg in history)
                {
                    contents.Add(new
                    {
                        role = msg.Role == "user" ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            // Add current user prompt
            var finalUserPrompt = $"[SYSTEM INSTRUCTION]\n{system}\n\n[USER]\n{user}";
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = finalUserPrompt } }
            });

            var payload = new { contents };

            // Switch to v1 stable API
            var url = $"https://generativelanguage.googleapis.com/v1/models/{_model}:generateContent?key={cleanKey}";
            
            using var http = httpClientFactory.CreateClient();
            using var response = await http.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("Gemini Chat API Error: Status={Status}, Detail={Detail}, URL_Model={Model}", response.StatusCode, error, _model);
                return $"Bé Vinh gặp lỗi kết nối (Mã: {(int)response.StatusCode}, Model: {_model}). Bạn hãy kiểm tra lại cấu hình trên Azure nhé.";
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var reply = result.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()?.Trim();

            return reply ?? "Không nhận được phản hồi từ AI.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gemini Chat call failed.");
            return "Xin lỗi, đã có lỗi hệ thống xảy ra.";
        }
    }
}
