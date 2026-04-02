using System.Security.Claims;

namespace VinhKhanh.API.Auth;

public static class AuthClaims
{
	public static bool TryGetOwnedPoiId(ClaimsPrincipal user, out int poiId)
	{
		poiId = 0;
		var v = user.FindFirst("OwnedPoiId")?.Value;
		return v != null && int.TryParse(v, out poiId);
	}

	public static bool IsAdmin(ClaimsPrincipal user)
		=> user.IsInRole("Admin");
}
