using System.Security.Claims;
using Jyz.Services;
using Microsoft.AspNetCore.Http;

namespace Jyz.UnitTests.Services
{
    public class CurrentUserTests
    {
        private static CurrentUser Build(ClaimsPrincipal? user = null)
        {
            var http = new DefaultHttpContext();
            if (user != null) http.User = user;
            var accessor = new HttpContextAccessor { HttpContext = http };
            return new CurrentUser(accessor);
        }

        [Fact]
        public void Anonymous_DefaultsAreEmpty()
        {
            var sut = Build();

            sut.IsAuthenticated.Should().BeFalse();
            sut.Id.Should().BeNull();
            sut.Username.Should().BeNull();
            sut.Roles.Should().BeEmpty();
            sut.Permissions.Should().BeEmpty();
        }

        [Fact]
        public void Authenticated_ReturnsClaims()
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "42"),
                    new Claim(ClaimTypes.Name, "alice"),
                    new Claim(ClaimTypes.Role, "admin"),
                    new Claim(ClaimTypes.Role, "user"),
                    new Claim("permission", "user:add"),
                    new Claim("permission", "user:edit"),
                ],
                authenticationType: "test");
            var sut = Build(new ClaimsPrincipal(identity));

            sut.IsAuthenticated.Should().BeTrue();
            sut.Id.Should().Be(42);
            sut.Username.Should().Be("alice");
            sut.Roles.Should().BeEquivalentTo(["admin", "user"]);
            sut.Permissions.Should().BeEquivalentTo(["user:add", "user:edit"]);
        }

        [Fact]
        public void Authenticated_NonNumericId_ReturnsNull()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "not-a-number")],
                authenticationType: "test");
            var sut = Build(new ClaimsPrincipal(identity));

            sut.Id.Should().BeNull();
        }

        [Fact]
        public void NoHttpContext_DefaultsAreEmpty()
        {
            var accessorMock = new Mock<IHttpContextAccessor>();
            accessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
            var sut = new CurrentUser(accessorMock.Object);

            sut.IsAuthenticated.Should().BeFalse();
            sut.Id.Should().BeNull();
            sut.Username.Should().BeNull();
            sut.Roles.Should().BeEmpty();
            sut.Permissions.Should().BeEmpty();
        }

        [Fact]
        public void Authenticated_NoRoleOrPermissionClaims_ReturnsEmptyLists()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "bob")],
                authenticationType: "test");
            var sut = Build(new ClaimsPrincipal(identity));

            sut.IsAuthenticated.Should().BeTrue();
            sut.Roles.Should().BeEmpty();
            sut.Permissions.Should().BeEmpty();
        }
    }
}
