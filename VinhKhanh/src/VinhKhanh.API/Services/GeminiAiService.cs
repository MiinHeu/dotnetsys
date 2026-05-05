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
            
            // Add history with role alternation check
            string lastRole = "";
            if (history != null)
            {
                foreach (var msg in history.Where(h => !string.IsNullOrWhiteSpace(h.Content)))
                {
                    var currentRole = msg.Role == "user" ? "user" : "model";
                    
                    // Gemini requires alternating roles. If same role, we skip or could merge.
                    // For simplicity, we skip consecutive same-role messages.
                    if (currentRole == lastRole) continue; 

                    contents.Add(new
                    {
                        role = currentRole,
                        parts = new[] { new { text = msg.Content } }
                    });
                    lastRole = currentRole;
                }
            }

            // Ensure the next message is 'user'. If last was 'user', we must append to it or handle it.
            var userText = $"[SYSTEM INSTRUCTION]\n{system}\n\n[USER]\n{user}";
            if (lastRole == "user" && contents.Count > 0)
            {
                // Append to the last user message instead of adding a new one
                // (Actually Gemini preferred way is alternating, so we just replace/append if needed)
                // But usually, we can just ensure we don't send two users.
                // Let's just remove the last user message from history if we are about to send a new user prompt.
                contents.RemoveAt(contents.Count - 1);
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userText } }
            });

            var payload = new { contents };

            // Switch to v1beta API to support gemini-1.5-flash and newer models
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={cleanKey}";
            
            using var http = httpClientFactory.CreateClient();
            using var response = await http.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("Gemini Chat API Error: Status={Status}, Detail={Detail}, URL_Model={Model}", response.StatusCode, error, _model);
                return $"Bé Vinh (v1beta) gặp lỗi kết nối (Mã: {(int)response.StatusCode}, Model: {_model}). Bạn hãy kiểm tra lại cấu hình trên Azure nhé.";
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
