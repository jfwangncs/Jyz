using System.Security.Claims;

namespace Jyz.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(IEnumerable<Claim> claims);
        ClaimsPrincipal ValidateToken(string token);
    }
}