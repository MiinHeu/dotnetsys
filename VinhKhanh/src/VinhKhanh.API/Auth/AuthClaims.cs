using System.Security.Claims;

namespace VinhKhanh.API.Auth;

public static class AuthClaims
{
	public static bool TryGetUserId(ClaimsPrincipal user, out int userId)
	{
		userId = 0;
		var v = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		return v != null && int.TryParse(v, out userId);
	}

	public static bool IsOwner(ClaimsPrincipal user)
		=> user.IsInRole("Owner");

	public static bool IsAdmin(ClaimsPrincipal user)
		=> user.IsInRole("Admin");
}
