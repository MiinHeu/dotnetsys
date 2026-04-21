using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
// using StackExchange.Redis;
using System.Text;
using VinhKhanh.API.Hubs;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);
#if DEBUG
builder.WebHost.UseUrls("https://0.0.0.0:7016", "http://0.0.0.0:5283");
#endif

// Keep logging portable across local/dev/test environments
// and avoid hard dependency on Windows EventLog permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
	builder.Logging.AddDebug();
}

builder.Services.AddOpenApi();
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		// Prevent runtime 500 when entities have circular navigation references (EF)
		options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
		options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
	});
builder.Services.AddHealthChecks();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	// Ưu tiên PostgreSQL nếu có chuỗi kết nối Default trong appsettings hoặc Env Vars
	var connStr = builder.Configuration.GetConnectionString("Default");
	if (!string.IsNullOrWhiteSpace(connStr) && !connStr.Contains("DATABASE_HOST"))
	{
		options.UseNpgsql(connStr);
	}
	else
	{
		// Dự phòng sang SQLite cho bản Deploy ban đầu hoặc Local Debug
		var sqlite = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=app.db";
		options.UseSqlite(sqlite);
		options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
	}
});

builder.Services.AddSignalR();

builder.Services.AddHttpClient();
builder.Services.AddScoped<GeminiTranslationService>();
builder.Services.AddScoped<OllamaTranslationService>();
builder.Services.AddScoped<LibreTranslateService>();
builder.Services.AddScoped<MicrosoftTranslatorService>();
builder.Services.AddScoped<ITranslationService, ResilientTranslationService>();
if (!string.IsNullOrWhiteSpace(builder.Configuration["AzureOpenAI:Endpoint"])
    && !string.IsNullOrWhiteSpace(builder.Configuration["AzureOpenAI:Key"]))
{
	builder.Services.AddScoped<IAiService, AzureAiService>();
}
else
{
	builder.Services.AddScoped<IAiService, OllamaAiService>();
}
if (!string.IsNullOrWhiteSpace(builder.Configuration["VoiceRss:ApiKey"]))
{
	builder.Services.AddScoped<ITtsService, VoiceRssTtsService>();
}
else
{
	builder.Services.AddScoped<ITtsService, AzureTtsService>();
}

// Redis — dùng NoOpRedisService cho testing
builder.Services.AddScoped<IRedisService, NoOpRedisService>();

builder.Services.AddCors(options =>
{
	options.AddPolicy("Default", policy =>
	{
		var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

		// Production-safe default: allow any origin (no credentials).
		// If you set Cors:AllowedOrigins, we will lock it down to those origins.
		if (allowedOrigins is { Length: > 0 })
		{
			policy
				.WithOrigins(allowedOrigins)
				.AllowAnyMethod()
				.AllowAnyHeader();
		}
		else if (builder.Environment.IsDevelopment())
		{
			// Dev convenience: Allow everything related to your local network and emulators
			policy
				.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader();
		}
		else
		{
			policy
				.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader();
		}
	});
});

// JWT (chưa áp authorize cho toàn bộ endpoint, nhưng cấu hình để sẵn cho PHAN sau).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Jwt:Issuer"],
			ValidAudience = builder.Configuration["Jwt:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
		};
	});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
	var forceDefaultCredentials = builder.Configuration.GetValue<bool>("Seed:ForceDefaultCredentials");
	try
	{
		if (db.Database.IsSqlite() && await IsLegacySqliteDatabaseAsync(db))
		{
			// Legacy dev DB created via EnsureCreated (no migration history).
			// Skip Migrate() to avoid noisy "table already exists" startup failures.
			db.Database.EnsureCreated();
		}
		else
		{
			db.Database.Migrate();
		}
	}
	catch (Exception ex)
	{
		logger.LogWarning(ex, "Database.Migrate failed, fallback to EnsureCreated.");
		db.Database.EnsureCreated();
	}
	await DbSeeder.SeedAsync(db, forceDefaultCredentials);
}

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}
app.UseCors("Default");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<VinhKhanhHub>("/hubs/vinh-khanh");
app.MapHealthChecks("/health");

app.Run();

static async Task<bool> IsLegacySqliteDatabaseAsync(ApplicationDbContext db)
{
	await using var conn = db.Database.GetDbConnection();
	if (conn.State != System.Data.ConnectionState.Open)
		await conn.OpenAsync();

	// Has any user table?
	await using var tableCmd = conn.CreateCommand();
	tableCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
	var tableCount = Convert.ToInt32(await tableCmd.ExecuteScalarAsync());
	if (tableCount == 0) return false;

	// Has migration history table?
	await using var histCmd = conn.CreateCommand();
	histCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory';";
	var historyCount = Convert.ToInt32(await histCmd.ExecuteScalarAsync());
	if (historyCount == 0) return true;

	// History table exists but has no rows => still a legacy EnsureCreated DB.
	await using var rowCmd = conn.CreateCommand();
	rowCmd.CommandText = "SELECT COUNT(*) FROM __EFMigrationsHistory;";
	var appliedMigrations = Convert.ToInt32(await rowCmd.ExecuteScalarAsync());
	return appliedMigrations == 0;
}

public partial class Program;