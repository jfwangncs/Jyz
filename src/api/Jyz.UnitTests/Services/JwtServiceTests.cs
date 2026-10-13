using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Jyz.Options;
using Jyz.Services;

namespace Jyz.UnitTests.Services
{
    public class JwtServiceTests
    {
        private static JwtService CreateService(int expires = 60)
        {
            var settings = new JwtSettings
            {
                SecretKey = "super-secret-key-for-unit-testing-with-256bits!!",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Expires = expires
            };
            return new JwtService(Microsoft.Extensions.Options.Options.Create(settings));
        }

        [Fact]
        public void GenerateToken_WithClaims_ReturnsNonEmptyToken()
        {
            var svc = CreateService();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1"), new(ClaimTypes.Name, "admin") };

            var token = svc.GenerateToken(claims);

            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateToken_ContainsExpectedClaims()
        {
            var svc = CreateService();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "42"),
                new(ClaimTypes.Name, "testuser"),
                new(ClaimTypes.Role, "Admin")
            };

            var token = svc.GenerateToken(claims);
            var principal = svc.ValidateToken(token);

            principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be("42");
            principal.FindFirstValue(ClaimTypes.Name).Should().Be("testuser");
            principal.IsInRole("Admin").Should().BeTrue();
        }

        [Fact]
        public void GenerateToken_IsWellFormedJwt()
        {
            var svc = CreateService();
            var token = svc.GenerateToken([new(ClaimTypes.NameIdentifier, "1")]);

            var handler = new JwtSecurityTokenHandler();
            handler.CanReadToken(token).Should().BeTrue();

            var jwt = handler.ReadJwtToken(token);
            jwt.Issuer.Should().Be("TestIssuer");
        }

        [Fact]
        public void ValidateToken_ValidToken_ReturnsPrincipal()
        {
            var svc = CreateService();
            var token = svc.GenerateToken([new(ClaimTypes.Name, "alice")]);

            var principal = svc.ValidateToken(token);

            principal.Should().NotBeNull();
            principal.FindFirstValue(ClaimTypes.Name).Should().Be("alice");
        }

        [Fact]
        public void ValidateToken_InvalidToken_Throws()
        {
            var svc = CreateService();

            var act = () => svc.ValidateToken("totally.invalid.token");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void ValidateToken_WrongSignature_Throws()
        {
            var svc = CreateService();
            var otherSettings = new JwtSettings
            {
                SecretKey = "different-secret-key-for-testing-only-256bits!",
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Expires = 60
            };
            var otherSvc = new JwtService(Microsoft.Extensions.Options.Options.Create(otherSettings));
            var token = otherSvc.GenerateToken([new(ClaimTypes.Name, "alice")]);

            var act = () => svc.ValidateToken(token);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GenerateToken_EmptyClaims_ReturnsToken()
        {
            var svc = CreateService();

            var token = svc.GenerateToken([]);

            token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateToken_Expires_IsSetCorrectly()
        {
            var svc = CreateService(expires: 120);
            var token = svc.GenerateToken([new(ClaimTypes.NameIdentifier, "1")]);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var expectedExpiry = DateTime.UtcNow.AddMinutes(120);

            jwt.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
        }
    }
}
