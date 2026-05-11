using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.API.Services;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class SampleDataController : ControllerBase
{
    [HttpGet("seed-offline")]
    public async Task<IActionResult> SeedOffline()
    {
        var sqliteFileName = "vinhkhanh_offline.db";
        var sqliteConnStr = $"Data Source={sqliteFileName}";
        
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(sqliteConnStr);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        
        using var db = new ApplicationDbContext(optionsBuilder.Options);

        try
        {
            // 1. Tạo mới database trắng
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();

            // 2. Sử dụng DbSeeder của dự án để bơm dữ liệu mẫu
            await DbSeeder.SeedAsync(db, forceDefaultCredentials: true);

            return Ok(new { 
                message = "Đã bơm dữ liệu mẫu vào file offline thành công!", 
                fileName = sqliteFileName,
                location = Path.GetFullPath(sqliteFileName),
                details = "Dữ liệu bao gồm: Các quán ăn mẫu, Tour tham quan mẫu và tài khoản Admin/Owner mặc định."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi tạo dữ liệu mẫu", detail = ex.Message });
        }
    }
}
