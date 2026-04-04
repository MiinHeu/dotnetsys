using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VinhKhanh.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
	public ApplicationDbContext CreateDbContext(string[] args)
	{
		var basePath = AppContext.BaseDirectory;
		// Try to locate appsettings.json
		var possiblePaths = new[]
		{
			basePath,
			Path.Combine(basePath, "VinhKhanh.API"),
			Path.Combine(basePath, "..", "VinhKhanh.API"),
			Directory.GetCurrentDirectory()
		};

		string? connectionString = null;

		foreach (var path in possiblePaths)
		{
			var jsonPath = Path.Combine(path, "appsettings.json");
			if (File.Exists(jsonPath))
			{
				try
				{
					var json = File.ReadAllText(jsonPath);
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs))
					{
						if (cs.TryGetProperty("Default", out var def))
							connectionString = def.GetString();
						else if (cs.TryGetProperty("Sqlite", out var sqlite))
							connectionString = sqlite.GetString();
					}
				}
				catch { /* ignore */ }
				break;
			}
		}

		connectionString ??= "Data Source=app.db";

		var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

		if (connectionString.Contains("Host=") || connectionString.Contains("Port="))
		{
			builder.UseNpgsql(connectionString);
		}
		else
		{
			builder.UseSqlite(connectionString);
		}

		return new ApplicationDbContext(builder.Options);
	}
}
