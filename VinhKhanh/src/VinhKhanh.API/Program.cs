using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using StackExchange.Redis;
using VinhKhanh.API.Hubs;
using VinhKhanh.API.Services;
using VinhKhanh.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
	});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	var connStr = builder.Configuration.GetConnectionString("Default");
	options.UseNpgsql(connStr);
});

var redisConn = builder.Configuration["Redis"];
if (string.IsNullOrWhiteSpace(redisConn))
{
	builder.Services.AddSingleton<IRedisService, NoOpRedisService>();
}
else
{
	var redisOpts = ConfigurationOptions.Parse(redisConn);
	redisOpts.AbortOnConnectFail = false;
	redisOpts.ConnectTimeout = 3000;
	builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOpts));
	builder.Services.AddScoped<IRedisService, RedisService>();
}

builder.Services.AddSignalR();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAiService, OllamaAiService>();
builder.Services.AddScoped<ITtsService, AzureTtsService>();

builder.Services.AddCors(options =>
{
	options.AddPolicy("Dev", policy =>
	{
		policy
			.WithOrigins(
				"http://localhost:5173",
				"http://127.0.0.1:5173",
				"http://localhost:5174",
				"http://127.0.0.1:5174")
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();
	});
});

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
	db.Database.Migrate();
	DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
}

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

// Chỉ ép HTTPS khi đã cấu hình URL HTTPS (tránh cảnh báo "Failed to determine the https port" khi chỉ http://localhost:5283).
if (!app.Environment.IsDevelopment())
	app.UseHttpsRedirection();

app.UseCors("Dev");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<VinhKhanhHub>("/hubs/vinh-khanh");

app.Run();
