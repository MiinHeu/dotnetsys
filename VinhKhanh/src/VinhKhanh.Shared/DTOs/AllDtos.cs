using System;
using System.Collections.Generic;

namespace VinhKhanh.Shared.DTOs;

public record LocationQueryDto(double Lat, double Lon, string Lang = "vi");

public record VisitLogDto(int PoiId, string SessionId, string LanguageCode, string TriggerType, int Duration);

public record TourStopDto(int PoiId, int StopOrder, int StayMinutes = 10, string? Note = null);

public record TourCreateDto(string Name, string Description, int EstimatedMinutes, List<TourStopDto> Stops);

public record MovementPointDto(double Lat, double Lon, float Accuracy, DateTime Timestamp);

public record MovementBatchDto(string SessionId, List<MovementPointDto> Points);

public record AppHistoryLogDto(
	string SessionId,
	string EventType,
	int? PoiId = null,
	int? TourId = null,
	string LanguageCode = "vi",
	string? Payload = null);

public record MessageHistory(string Role, string Content);

public record ChatRequest(string Message, string Language = "vi", List<MessageHistory>? History = null);

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Role, DateTime Expires);

public class ChatMessage
{
	public string Content { get; set; } = "";
	public bool IsUser { get; set; }
}

public record TtsSynthesizeDto(string Text, string Lang = "vi", string? Voice = null);

public record PoiTranslationDto(string LanguageCode, string Name, string Description, string? AudioUrl = null);
