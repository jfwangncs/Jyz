using FluentValidation.TestHelper;
using Jyz.Constants;
using Jyz.Model.Permission;
using Jyz.Validations;

namespace Jyz.UnitTests.Validations
{
    public class PermissionValidationTests
    {
        // ─── CreatePermissionRequestValidation ─────────────────────────────────

        [Fact]
        public void CreatePermission_EmptyName_HasValidationError()
        {
            var v = new CreatePermissionRequestValidation();
            var result = v.TestValidate(new CreatePermissionRequest { Name = "", Code = "c", Type = PermissionType.Button });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void CreatePermission_NameTooLong_HasValidationError()
        {
            var v = new CreatePermissionRequestValidation();
            var result = v.TestValidate(new CreatePermissionRequest { Name = new string('a', 51), Code = "c", Type = PermissionType.Button });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void CreatePermission_EmptyCode_HasValidationError()
        {
            var v = new CreatePermissionRequestValidation();
            var result = v.TestValidate(new CreatePermissionRequest { Name = "n", Code = "", Type = PermissionType.Button });
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void CreatePermission_CodeTooLong_HasValidationError()
        {
            var v = new CreatePermissionRequestValidation();
            var result = v.TestValidate(new CreatePermissionRequest { Name = "n", Code = new string('a', 101), Type = PermissionType.Button });
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void CreatePermission_DescriptionTooLong_HasValidationError()
        {
            var v = new CreatePermissionRequestValidation();
            var result = v.TestValidate(new CreatePermissionRequest { Name = "n", Code = "c", Type = PermissionType.Button, Description = new string('a', 201) });
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void CreatePermission_IconTooLong_HasValidationError()
        {
            var v = new CreatePermissionRequestValidation();
            var result = v.TestValidate(new CreatePermissionRequest { Name = "n", Code = "c", Type = PermissionType.Button, Icon = new string('a', 51) });
            result.ShouldHaveValidationErrorFor(x => x.Icon);
        }

        [Fact]
        public void CreatePermission_Valid_NoValidationErrors()
        {
            var v = new CreatePermissionRequestValidation();
            var result = v.TestValidate(new CreatePermissionRequest { Name = "n", Code = "c", Type = PermissionType.Button, Description = "d", Icon = "i" });
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ─── UpdatePermissionRequestValidation ──────────────────────────────────

        [Fact]
        public void UpdatePermission_NameTooLong_HasValidationError()
        {
            var v = new UpdatePermissionRequestValidation();
            var result = v.TestValidate(new UpdatePermissionRequest { Name = new string('a', 51) });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void UpdatePermission_DescriptionTooLong_HasValidationError()
        {
            var v = new UpdatePermissionRequestValidation();
            var result = v.TestValidate(new UpdatePermissionRequest { Description = new string('a', 201) });
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void UpdatePermission_IconTooLong_HasValidationError()
        {
            var v = new UpdatePermissionRequestValidation();
            var result = v.TestValidate(new UpdatePermissionRequest { Icon = new string('a', 51) });
            result.ShouldHaveValidationErrorFor(x => x.Icon);
        }

        [Fact]
        public void UpdatePermission_AllNulls_NoValidationErrors()
        {
            var v = new UpdatePermissionRequestValidation();
            var result = v.TestValidate(new UpdatePermissionRequest());
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
