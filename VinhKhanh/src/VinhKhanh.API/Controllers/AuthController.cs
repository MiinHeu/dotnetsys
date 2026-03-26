using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VinhKhanh.Infrastructure.Data;
using VinhKhanh.Shared.DTOs;

namespace VinhKhanh.API.Controllers;

[ApiController, Route("api/[controller]")]
public class AuthController(ApplicationDbContext db, IConfiguration cfg) : ControllerBase
{
	[AllowAnonymous, HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
	{
		var user = await db.AppUsers.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Username == req.Username && u.IsActive, ct);

		if (user == null)
			return Unauthorized(new { message = "Sai tai khoan hoac mat khau" });

		if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
			return Unauthorized(new { message = "Sai tai khoan hoac mat khau" });

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var expiryMinutes = double.Parse(cfg["Jwt:ExpiryMinutes"] ?? "1440");
		var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.Username),
			new(ClaimTypes.Role, user.Role)
		};

		if (user.OwnedPoiId is { } oid)
			claims.Add(new Claim("OwnedPoiId", oid.ToString()));

		var token = new JwtSecurityToken(
			issuer: cfg["Jwt:Issuer"],
			audience: cfg["Jwt:Audience"],
			claims: claims,
			expires: expires,
			signingCredentials: creds);

		var jwt = new JwtSecurityTokenHandler().WriteToken(token);
		return Ok(new LoginResponse(jwt, user.Role, expires));
	}
}
