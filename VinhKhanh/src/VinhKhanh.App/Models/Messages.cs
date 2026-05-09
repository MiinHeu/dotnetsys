using Microsoft.Maui.Devices.Sensors;
using VinhKhanh.Infrastructure.Data;

namespace VinhKhanh.App.Models;

public record LanguageChangedMessage(string NewLanguage);
public record LocationUpdatedMessage(Location Location);
public record NarrationStartedMessage(PoiSnapshot Poi, string Language, string TriggerType);
public record NarrationEndedMessage(int PoiId, int DurationSeconds, string TriggerType, string LanguageCode);
