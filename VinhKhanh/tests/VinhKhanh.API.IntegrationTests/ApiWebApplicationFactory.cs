using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VinhKhanh.API.IntegrationTests;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
	private readonly string _tempDir;
	private readonly string _dbPath;

	public ApiWebApplicationFactory()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), "vinh-khanh-int", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_tempDir);
		_dbPath = Path.Combine(_tempDir, "integration.db");
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Development");
		builder.ConfigureLogging(logging =>
		{
			// Avoid Windows EventLog permission issues in sandboxed test runners.
			logging.ClearProviders();
			logging.AddConsole();
		});

		builder.ConfigureAppConfiguration((_, configBuilder) =>
		{
			var overrides = new Dictionary<string, string?>
			{
				["ConnectionStrings:Sqlite"] = $"Data Source={_dbPath}",
				["Ollama:BaseUrl"] = "http://127.0.0.1:65530",
				["AzureOpenAI:Endpoint"] = "",
				["AzureOpenAI:Key"] = "",
				["AzureTTS:Key"] = "",
				["AzureTTS:Region"] = ""
			};

			configBuilder.AddInMemoryCollection(overrides);
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (!disposing)
			return;

		try
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, recursive: true);
		}
		catch
		{
			// Keep test cleanup non-fatal.
		}
	}
}
