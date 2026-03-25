namespace VinhKhanh.API.Services;

public interface ITtsService
{
	Task<byte[]> SynthesizeAsync(string text, string lang, string voice);
}
