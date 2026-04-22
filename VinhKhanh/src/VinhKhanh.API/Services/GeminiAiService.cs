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
    private readonly string _model = cfg["Gemini:Model"] ?? "gemini-1.5-flash";

    public async Task<string> ChatAsync(string system, string user, List<MessageHistory>? history = null)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("Gemini API Key is missing.");
            return "Xin lỗi, hiện tại tôi không thể trả lời được do thiếu cấu hình AI.";
        }

        try
        {
            // Build contents for Gemini
            var contents = new List<object>();

            // 1. System instruction (as a special model turn if needed, but here we prepended to user prompt for simplicity or use system_instruction if API supports)
            // Note: In newer v1beta, system_instruction is a separate field.
            
            // 2. Add history
            if (history != null)
            {
                foreach (var msg in history)
                {
                    contents.Add(new
                    {
                        role = msg.IsUser ? "user" : "model",
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            // 3. Add current user prompt (including system instructions to ensure context)
            var finalUserPrompt = $"[SYSTEM INSTRUCTION]\n{system}\n\n[USER]\n{user}";
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = finalUserPrompt } }
            });

            var payload = new { contents };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            
            using var http = httpClientFactory.CreateClient();
            using var response = await http.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("Gemini Chat API Error: Status={Status}, Detail={Detail}", response.StatusCode, error);
                return "Xin lỗi, có lỗi xảy ra khi kết nối với máy chủ AI.";
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
