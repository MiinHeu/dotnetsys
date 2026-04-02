using System.Globalization;
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
		if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
			return BadRequest(new { message = "Username va Password khong duoc de trong." });

		var username = req.Username.Trim();
		var user = await db.AppUsers.AsNoTracking()
			.FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

		if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
			return Unauthorized(new { message = "Sai tai khoan hoac mat khau" });

		var jwtKey = cfg["Jwt:Key"];
		var issuer = cfg["Jwt:Issuer"];
		var audience = cfg["Jwt:Audience"];
		if (string.IsNullOrWhiteSpace(jwtKey)
		    || string.IsNullOrWhiteSpace(issuer)
		    || string.IsNullOrWhiteSpace(audience))
		{
			return StatusCode(StatusCodes.Status500InternalServerError,
				new { message = "JWT configuration is missing." });
		}

		var expiryMinutesRaw = cfg["Jwt:ExpiryMinutes"] ?? "1440";
		if (!double.TryParse(expiryMinutesRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var expiryMinutes))
			expiryMinutes = 1440;
		if (expiryMinutes <= 0)
			expiryMinutes = 1440;

		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
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
			issuer: issuer,
			audience: audience,
			claims: claims,
			expires: expires,
			signingCredentials: creds);

		var jwt = new JwtSecurityTokenHandler().WriteToken(token);
		return Ok(new LoginResponse(jwt, user.Role, expires));
	}
}
