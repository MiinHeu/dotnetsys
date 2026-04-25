using Microsoft.AspNetCore.Mvc;

namespace VinhKhanh.API.Controllers;

[ApiController]
[Route("api/app")]
public class AppDownloadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public AppDownloadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("download")]
    public IActionResult DownloadApk()
    {
        // Chạy linh hoạt trên cả local và production
        string apkPath = Path.Combine(_env.ContentRootPath, "..", "VinhKhanh.App", "bin", "Release", "net10.0-android", "com.companyname.vinhkhanh.app-Signed.apk");

        if (!System.IO.File.Exists(apkPath))
        {
            // Fallback cho môi trường production nếu file được copy vào wwwroot/downloads
            apkPath = Path.Combine(_env.WebRootPath, "downloads", "VinhKhanh_v1.0.apk");
        }

        if (!System.IO.File.Exists(apkPath))
        {
            return NotFound("Không tìm thấy file APK. Vui lòng build bản Release hoặc kiểm tra thư mục downloads.");
        }

        return PhysicalFile(apkPath, "application/vnd.android.package-archive", "VinhKhanh_v1.0.apk");
    }
}
