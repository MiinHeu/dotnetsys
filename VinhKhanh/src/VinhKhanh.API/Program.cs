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

try 
{
    var builder = WebApplication.CreateBuilder(args);
    Console.WriteLine($"[STARTUP] Starting VinhKhanh API in {builder.Environment.EnvironmentName} mode");
    #if DEBUG
    builder.WebHost.UseUrls("https://0.0.0.0:7016", "http://0.0.0.0:5283");
    #else
    // Azure Linux App Service for .NET 8/9/10 defaults to 8080
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(8080);
    });
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
    builder.Services.AddMemoryCache();
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
        // 1. Ưu tiên Azure SQL (SQL Server) nếu có chuỗi kết nối "SqlServer"
        var sqlServerStr = builder.Configuration.GetConnectionString("SqlServer");
        if (!string.IsNullOrWhiteSpace(sqlServerStr) && !sqlServerStr.Contains("YOUR_SERVER"))
        {
            Console.WriteLine("[STARTUP] Using Azure SQL (SQL Server)");
            options.UseSqlServer(sqlServerStr);
        }
        // 2. Dự phòng sang PostgreSQL (Giữ lại logic cũ để linh hoạt)
        else if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default")) && 
                 !builder.Configuration.GetConnectionString("Default")!.Contains("DATABASE_HOST"))
        {
            Console.WriteLine("[STARTUP] Using PostgreSQL Database");
            options.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
        }
        // 3. Mặc định dùng SQLite cho Local Debug
        else
        {
            var sqlite = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=app.db";
            Console.WriteLine($"[STARTUP] Using SQLite Database: {sqlite}");
            options.UseSqlite(sqlite);
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }
    });

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();

    builder.Services.AddHttpClient();
    // Ưu tiên dùng Azure hoàn toàn cho Dịch thuật
    builder.Services.AddScoped<GeminiTranslationService>();
    builder.Services.AddScoped<OllamaTranslationService>();
    builder.Services.AddScoped<LibreTranslateService>();
    builder.Services.AddScoped<MicrosoftTranslatorService>();
    builder.Services.AddScoped<ITranslationService, MicrosoftTranslatorService>();
    // Ưu tiên dùng Azure OpenAI cho AI Service
    builder.Services.AddScoped<GeminiAiService>();
    builder.Services.AddScoped<OllamaAiService>();
    builder.Services.AddScoped<IAiService, GeminiAiService>();
    // Ưu tiên dùng Azure cho TTS
    builder.Services.AddScoped<VoiceRssTtsService>();
    builder.Services.AddScoped<AzureTtsService>();
    builder.Services.AddScoped<ITtsService, AzureTtsService>();

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
            Console.WriteLine("[STARTUP] Initializing Database (EnsureCreated)...");
            // Sử dụng EnsureCreated để tạo DB sạch từ Model hiên tại, bỏ qua Migration history
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[STARTUP] Database initialization failed: {ex.Message}");
        }
        await DbSeeder.SeedAsync(db, forceDefaultCredentials);
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // Cấu hình Proxy để nhận diện IP và HTTPs đúng từ Azure LB
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors("Default");
    app.UseDefaultFiles();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<VinhKhanhHub>("/hubs/vinh-khanh");
    app.MapHealthChecks("/health");

    // SPA fallback: serve index.html for any non-API, non-file route
    app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("[CRITICAL] Global Startup Error:");
    Console.WriteLine(ex.ToString());
    throw;
}

public partial class Program;

public partial class Program;