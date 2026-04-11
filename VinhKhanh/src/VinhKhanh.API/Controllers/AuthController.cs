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

	/// <summary>Admin retrieves all users.</summary>
	[Authorize(Roles = "Admin"), HttpGet("users")]
	public async Task<IActionResult> GetAllUsers(CancellationToken ct)
	{
		var users = await db.AppUsers.AsNoTracking()
			.Select(u => new
			{
				u.Id,
				u.Username,
				u.Role,
				u.IsActive,
				u.CreatedAt
			})
			.ToListAsync(ct);
		return Ok(users);
	}

	/// <summary>Admin updates user status (e.g. deactivate).</summary>
	[Authorize(Roles = "Admin"), HttpPut("users/{id}/status")]
	public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest req, CancellationToken ct)
	{
		var user = await db.AppUsers.FindAsync(new object[] { id }, ct);
		if (user == null)
			return NotFound(new { message = "User khong ton tai." });

		if (user.Username == "admin" && !req.IsActive)
			return BadRequest(new { message = "Khong the khoa tai khoan admin chinh." });

		user.IsActive = req.IsActive;
		await db.SaveChangesAsync(ct);

		return Ok(new { message = "Cap nhat trang thai thanh cong.", user.Id, user.IsActive });
	}

	/// <summary>Admin creates any user (Admin or Owner).</summary>
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

	/// <summary>Owner self-registration (no auth required).</summary>
	[AllowAnonymous, HttpPost("register-owner")]
	public async Task<IActionResult> RegisterOwner([FromBody] RegisterRequest req, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
			return BadRequest(new { message = "Username va Password khong duoc de trong." });

		if (req.Username.Trim().Length < 3 || req.Username.Trim().Length > 50)
			return BadRequest(new { message = "Username phai tu 3-50 ky tu." });

		if (req.Password.Length < 6)
			return BadRequest(new { message = "Password phai co it nhat 6 ky tu." });

		var username = req.Username.Trim();
		var exists = await db.AppUsers.AnyAsync(u => u.Username == username, ct);
		if (exists)
			return Conflict(new { message = $"Username '{username}' da ton tai." });

		var user = new AppUser
		{
			Username = username,
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
			Role = "Owner",
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		db.AppUsers.Add(user);
		await db.SaveChangesAsync(ct);

		return Ok(new { message = "Da tao tai khoan Owner", user.Id, user.Username, user.Role });
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

	/// <summary>Logged-in user changes their own password.</summary>
	[Authorize, HttpPut("change-password")]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
			return BadRequest(new { message = "Mat khau cu va moi khong duoc de trong." });

		if (req.NewPassword.Length < 6)
			return BadRequest(new { message = "Mat khau moi phai co it nhat 6 ky tu." });

		if (!Auth.AuthClaims.TryGetUserId(HttpContext.User, out var userId))
			return Unauthorized(new { message = "Token khong hop le." });

		var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);
		if (user == null)
			return NotFound(new { message = "Tai khoan khong ton tai." });

		if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
			return BadRequest(new { message = "Mat khau hien tai khong dung." });

		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
		await db.SaveChangesAsync(ct);

		return Ok(new { message = "Da doi mat khau thanh cong." });
	}

	/// <summary>Reset password for Owner accounts (self-service, no auth).</summary>
	[AllowAnonymous, HttpPost("forgot-password")]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(req.Username))
			return BadRequest(new { message = "Username khong duoc de trong." });

		if (string.IsNullOrWhiteSpace(req.NewPassword))
			return BadRequest(new { message = "Mat khau moi khong duoc de trong." });

		if (req.NewPassword.Length < 6)
			return BadRequest(new { message = "Mat khau moi phai co it nhat 6 ky tu." });

		var username = req.Username.Trim();
		var user = await db.AppUsers.FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);
		if (user == null)
			return NotFound(new { message = "Tai khoan khong ton tai." });

		// Only allow reset for Owner accounts; Admin password can only be changed via change-password
		if (user.Role == "Admin")
			return BadRequest(new { message = "Tai khoan Admin khong the reset mat khau bang cach nay." });

		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
		await db.SaveChangesAsync(ct);

		return Ok(new { message = "Da dat lai mat khau thanh cong." });
	}
}
