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
	options.UseInMemoryDatabase("VinhKhanhDB");
#else
	var connStr = builder.Configuration.GetConnectionString("Default");
	options.UseNpgsql(connStr);
#endif
});

builder.Services.AddSignalR();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAiService, OllamaAiService>();
builder.Services.AddScoped<ITtsService, AzureTtsService>();

// Redis — dùng NoOpRedisService cho testing
builder.Services.AddScoped<IRedisService, NoOpRedisService>();

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
	scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
}
#endif

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("Dev");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<VinhKhanhHub>("/hubs/vinh-khanh");

app.Run();
