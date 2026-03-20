using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI (.NET 10 template keeps Scalar integration minimal).
builder.Services.AddOpenApi();

// EF Core (SQL Server).
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();

var app = builder.Build();

// Auto-migrate + seed on startup (good for PoC; can be disabled later).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
