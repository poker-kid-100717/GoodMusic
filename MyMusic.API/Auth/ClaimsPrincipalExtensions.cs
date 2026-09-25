using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MyMusic.API.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string UserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new InvalidOperationException("Authenticated principal has no subject claim.");
}
