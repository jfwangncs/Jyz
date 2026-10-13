using FluentValidation.TestHelper;
using Jyz.Model.Role;
using Jyz.Validations;

namespace Jyz.UnitTests.Validations
{
    public class RoleValidationTests
    {
        // ─── CreateRoleRequestValidation ──────────────────────────────────────

        [Fact]
        public void CreateRole_EmptyName_HasValidationError()
        {
            var v = new CreateRoleRequestValidation();
            var result = v.TestValidate(new CreateRoleRequest { Name = "" });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void CreateRole_NameTooLong_HasValidationError()
        {
            var v = new CreateRoleRequestValidation();
            var result = v.TestValidate(new CreateRoleRequest { Name = new string('a', 51) });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void CreateRole_DescriptionTooLong_HasValidationError()
        {
            var v = new CreateRoleRequestValidation();
            var result = v.TestValidate(new CreateRoleRequest { Name = "Admin", Description = new string('a', 201) });
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void CreateRole_Valid_NoValidationErrors()
        {
            var v = new CreateRoleRequestValidation();
            var result = v.TestValidate(new CreateRoleRequest { Name = "Admin", Description = "Admin role" });
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void CreateRole_NullDescription_NoValidationError()
        {
            var v = new CreateRoleRequestValidation();
            var result = v.TestValidate(new CreateRoleRequest { Name = "Admin" });
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }

        // ─── UpdateRoleRequestValidation ──────────────────────────────────────

        [Fact]
        public void UpdateRole_NameTooLong_HasValidationError()
        {
            var v = new UpdateRoleRequestValidation();
            var result = v.TestValidate(new UpdateRoleRequest { Name = new string('a', 51) });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void UpdateRole_DescriptionTooLong_HasValidationError()
        {
            var v = new UpdateRoleRequestValidation();
            var result = v.TestValidate(new UpdateRoleRequest { Description = new string('a', 201) });
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void UpdateRole_AllNulls_NoValidationErrors()
        {
            var v = new UpdateRoleRequestValidation();
            var result = v.TestValidate(new UpdateRoleRequest());
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateRole_ValidName_NoValidationError()
        {
            var v = new UpdateRoleRequestValidation();
            var result = v.TestValidate(new UpdateRoleRequest { Name = "Editor" });
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
