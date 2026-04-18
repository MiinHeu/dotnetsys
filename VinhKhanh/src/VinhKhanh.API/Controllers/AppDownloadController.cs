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
        // Đường dẫn tương đối từ máy tính của người dùng
        string apkPath = @"C:\Users\nt\dotnetsys\VinhKhanh\src\VinhKhanh.App\bin\Release\net10.0-android\com.companyname.vinhkhanh.app-Signed.apk";

        if (!System.IO.File.Exists(apkPath))
        {
            return NotFound("Không tìm thấy file APK. Vui lòng build bản Release trước.");
        }

        var fileBytes = System.IO.File.ReadAllBytes(apkPath);
        return File(fileBytes, "application/vnd.android.package-archive", "VinhKhanh_v1.0.apk");
    }
}
