namespace VinhKhanh.App.Models;

public class ChatMessage
{
    public string Content { get; set; } = "";
    public bool IsUser { get; set; }
    public bool IsBot => !IsUser;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
