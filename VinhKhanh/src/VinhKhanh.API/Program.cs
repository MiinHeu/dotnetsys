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
    builder.Services.AddControllers(options => 
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        })
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
    builder.Services.AddScoped<GroqAiService>();
    builder.Services.AddScoped<OllamaAiService>();

    // Cấu hình linh hoạt AI Service: Ưu tiên Groq vì Gemini bị chặn tại East Asia
    var groqKey = builder.Configuration["Groq:ApiKey"];
    var geminiKey = builder.Configuration["Gemini:ApiKey"];

    if (!string.IsNullOrWhiteSpace(groqKey))
    {
        Console.WriteLine("[STARTUP] Using GroqAiService for AI Chat (Preferred for East Asia).");
        builder.Services.AddScoped<IAiService, GroqAiService>();
    }
    else if (!string.IsNullOrWhiteSpace(geminiKey) && !geminiKey.Contains("YOUR_GEMINI_API_KEY"))
    {
        Console.WriteLine("[STARTUP] Using GeminiAiService for AI Chat.");
        builder.Services.AddScoped<IAiService, GeminiAiService>();
    }
    else
    {
        Console.WriteLine("[STARTUP] No Cloud AI Key found. Falling back to Local OllamaAiService.");
        builder.Services.AddScoped<IAiService, OllamaAiService>();
    }
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
    app.UseDeveloperExceptionPage();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        var forceDefaultCredentials = builder.Configuration.GetValue<bool>("Seed:ForceDefaultCredentials");
        try
        {
            if (db.Database.IsNpgsql())
            {
                Console.WriteLine("[STARTUP] Applying Entity Framework Migrations for PostgreSQL...");
                await db.Database.MigrateAsync();
            }
            else
            {
                Console.WriteLine($"[STARTUP] Initializing Database (EnsureCreated) for {db.Database.ProviderName}...");
                db.Database.EnsureCreated();
                try
                {
                    // Test if the Address column exists (Mobile V2.0 update)
                    db.Database.ExecuteSqlRaw("SELECT Address FROM Pois");
                }
                catch
                {
                    Console.WriteLine($"[STARTUP] Patching {db.Database.ProviderName} schema for Mobile V2.0...");
                    if (db.Database.IsSqlServer())
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD Address NVARCHAR(256);");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD PhoneNumber NVARCHAR(32);");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD OperatingHours NVARCHAR(64);");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD Rating FLOAT NOT NULL DEFAULT 5.0;");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD ImagesJson NVARCHAR(MAX);");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD MenuJson NVARCHAR(MAX);");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD TagsJson NVARCHAR(MAX);");
                    }
                    else // SQLite
                    {
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD COLUMN Address TEXT;");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD COLUMN PhoneNumber TEXT;");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD COLUMN OperatingHours TEXT;");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD COLUMN Rating REAL NOT NULL DEFAULT 5.0;");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD COLUMN ImagesJson TEXT;");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD COLUMN MenuJson TEXT;");
                        db.Database.ExecuteSqlRaw("ALTER TABLE Pois ADD COLUMN TagsJson TEXT;");
                    }
                }
            }
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