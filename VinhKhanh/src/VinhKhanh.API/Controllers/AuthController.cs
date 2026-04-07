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
	[Authorize, HttpGet("owners")]
	public async Task<IActionResult> GetOwners(CancellationToken ct)
	{
		var owners = await db.AppUsers.AsNoTracking()
			.Where(u => u.Role == "Owner" && u.IsActive)
			.Select(u => new { u.Id, u.Username })
			.ToListAsync(ct);
		return Ok(owners);
	}

	[Authorize(Roles = "Admin"), HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
			return BadRequest(new { message = "Username va Password khong duoc de trong." });

		if (req.Username.Trim().Length < 3 || req.Username.Trim().Length > 50)
			return BadRequest(new { message = "Username phai tu 3-50 ky tu." });

		if (req.Password.Length < 6)
			return BadRequest(new { message = "Password phai co it nhat 6 ky tu." });

		var role = (req.Role ?? "Owner").Trim();
		if (role != "Owner" && role != "Admin")
			return BadRequest(new { message = "Role phai la Owner hoac Admin." });

		var username = req.Username.Trim();
		var exists = await db.AppUsers.AnyAsync(u => u.Username == username, ct);
		if (exists)
			return Conflict(new { message = $"Username '{username}' da ton tai." });

		var user = new AppUser
		{
			Username = username,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
			Role = role,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		db.AppUsers.Add(user);
		await db.SaveChangesAsync(ct);

		return Ok(new { message = "Da tao tai khoan", user.Id, user.Username, user.Role });
	}

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
