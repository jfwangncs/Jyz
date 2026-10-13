using JfYu.Data.Model;
using Jyz.Entity;
using Jyz.Extensions;
using Jyz.Model.Response;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Jyz.UnitTests.Extensions
{
    public class MapsterExtensionTests
    {
        [Fact]
        public void AddMapster_ReturnsSameServiceCollection()
        {
            var services = new ServiceCollection();

            var result = services.AddMapster();

            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddMapster_ConfiguresUserToUserResponseMapping()
        {
            new ServiceCollection().AddMapster();

            var activeRole = new Role { Id = 1, Name = "Admin", Status = (int)DataStatus.Active };
            var activePermission = new Permission { Id = 1, Code = "user:get", Name = "Get User", Status = (int)DataStatus.Active };
            var inactivePermission = new Permission { Id = 2, Code = "user:delete", Name = "Delete User", Status = (int)DataStatus.Disable };
            activeRole.Permissions.Add(activePermission);
            activeRole.Permissions.Add(inactivePermission);

            var inactiveRole = new Role { Id = 2, Name = "Guest", Status = (int)DataStatus.Disable };
            var guestPermission = new Permission { Id = 3, Code = "guest:view", Name = "Guest View", Status = (int)DataStatus.Active };
            inactiveRole.Permissions.Add(guestPermission);

            var user = new User { Id = 1, UserName = "alice" };
            user.Roles.Add(activeRole);
            user.Roles.Add(inactiveRole);

            var response = user.Adapt<UserResponse>();

            response.Roles.Should().ContainSingle().Which.Should().Be("user:get");
            response.RoleList.Should().HaveCount(2);
        }
    }
}
