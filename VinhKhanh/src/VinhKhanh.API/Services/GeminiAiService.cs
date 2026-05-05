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

        // Clean inputs
        var modelId = _model.Trim().ToLower();
        var key = _apiKey.Trim();

        // Build contents
        var contents = new List<object>();
        string lastRole = "";
        if (history != null)
        {
            foreach (var msg in history.Where(h => !string.IsNullOrWhiteSpace(h.Content)))
            {
                var currentRole = msg.Role?.ToLower() == "assistant" ? "model" : "user";
                if (currentRole == lastRole) continue;
                contents.Add(new { role = currentRole, parts = new[] { new { text = msg.Content } } });
                lastRole = currentRole;
            }
        }

        var userText = $"[SYSTEM INSTRUCTION]\n{system}\n\n[USER]\n{user}";
        if (lastRole == "user" && contents.Count > 0) contents.RemoveAt(contents.Count - 1);
        contents.Add(new { role = "user", parts = new[] { new { text = userText } } });

        var payload = new { contents };

        // Try v1beta then v1
        string[] versions = { "v1beta", "v1" };
        foreach (var ver in versions)
        {
            var url = $"https://generativelanguage.googleapis.com/{ver}/models/{modelId}:generateContent?key={key}";
            try
            {
                using var http = httpClientFactory.CreateClient();
                using var response = await http.PostAsJsonAsync(url, payload);

                // If v1beta returns 404, we continue to v1
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound && ver == "v1beta") continue;

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    logger.LogError("Gemini Error ({Ver}): Status={Status}, Detail={Detail}", ver, response.StatusCode, error);
                    return $"Bé Vinh ({ver}) lỗi {(int)response.StatusCode}. Hãy kiểm tra Model ID '{modelId}' trên Azure.";
                }

                var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                var reply = result.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?.Trim();

                return reply ?? "Không nhận được phản hồi.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gemini Exception on {Ver}", ver);
                if (ver == "v1") return $"Lỗi hệ thống: {ex.Message}";
            }
        }

        return "Không thể kết nối với Gemini AI.";
    }
}
