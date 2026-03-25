namespace VinhKhanh.Shared.DTOs;

public class TtsRequest
{
	public string Text { get; set; } = string.Empty;
	public string Lang { get; set; } = "vi";
	public string Voice { get; set; } = "vi-VN-HoaiMyNeural";
}
