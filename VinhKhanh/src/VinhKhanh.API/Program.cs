using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
// using StackExchange.Redis;
using System.Text;
using VinhKhanh.API.Hubs;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

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
	});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
#if DEBUG
	var sqlite = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=app.db";
	options.UseSqlite(sqlite);
#else
	var connStr = builder.Configuration.GetConnectionString("Default");
	options.UseNpgsql(connStr);
#endif
});

builder.Services.AddSignalR();

builder.Services.AddHttpClient();
if (!string.IsNullOrWhiteSpace(builder.Configuration["Ollama:BaseUrl"]))
{
	builder.Services.AddScoped<ITranslationService, OllamaTranslationService>();
}
else if (!string.IsNullOrWhiteSpace(builder.Configuration["LibreTranslate:BaseUrl"]))
{
	builder.Services.AddScoped<ITranslationService, LibreTranslateService>();
}
else
{
	builder.Services.AddScoped<ITranslationService, MicrosoftTranslatorService>();
}
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
	options.AddPolicy("Dev", policy =>
	{
		policy
			.SetIsOriginAllowed(origin =>
			{
				if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
					return false;

				return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
				       || uri.Host.Equals("127.0.0.1")
				       || uri.Host.Equals("10.0.2.2");
			})
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();
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

// Auto-migrate on startup (only for real databases)
#if !DEBUG
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.Migrate();
	await DbSeeder.SeedAsync(db);
}
#else
// Database EnsureCreated for local SQLite.
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
	db.Database.EnsureCreated();
	db.SeedDemoData();
	await DbSeeder.SeedAsync(db);
}
#endif

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}
app.UseCors("Dev");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<VinhKhanhHub>("/hubs/vinh-khanh");

app.Run();

public partial class Program;
