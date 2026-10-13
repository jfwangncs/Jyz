using FluentValidation.TestHelper;
using Jyz.Model.DictItem;
using Jyz.Model.DictType;
using Jyz.Validations;

namespace Jyz.UnitTests.Validations
{
    public class DictValidationTests
    {
        // ─── CreateDictTypeRequestValidation ──────────────────────────────────

        [Fact]
        public void CreateDictType_EmptyCode_HasValidationError()
        {
            var v = new CreateDictTypeRequestValidation();
            var result = v.TestValidate(new CreateDictTypeRequest { Code = "", Name = "Gender" });
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void CreateDictType_EmptyName_HasValidationError()
        {
            var v = new CreateDictTypeRequestValidation();
            var result = v.TestValidate(new CreateDictTypeRequest { Code = "gender", Name = "" });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void CreateDictType_CodeTooLong_HasValidationError()
        {
            var v = new CreateDictTypeRequestValidation();
            var result = v.TestValidate(new CreateDictTypeRequest { Code = new string('a', 51), Name = "Gender" });
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void CreateDictType_NameTooLong_HasValidationError()
        {
            var v = new CreateDictTypeRequestValidation();
            var result = v.TestValidate(new CreateDictTypeRequest { Code = "gender", Name = new string('a', 101) });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void CreateDictType_DescriptionTooLong_HasValidationError()
        {
            var v = new CreateDictTypeRequestValidation();
            var result = v.TestValidate(new CreateDictTypeRequest { Code = "gender", Name = "Gender", Description = new string('a', 501) });
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void CreateDictType_Valid_NoValidationErrors()
        {
            var v = new CreateDictTypeRequestValidation();
            var result = v.TestValidate(new CreateDictTypeRequest { Code = "gender", Name = "Gender" });
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ─── UpdateDictTypeRequestValidation ──────────────────────────────────

        [Fact]
        public void UpdateDictType_NameTooLong_HasValidationError()
        {
            var v = new UpdateDictTypeRequestValidation();
            var result = v.TestValidate(new UpdateDictTypeRequest { Name = new string('a', 101) });
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void UpdateDictType_DescriptionTooLong_HasValidationError()
        {
            var v = new UpdateDictTypeRequestValidation();
            var result = v.TestValidate(new UpdateDictTypeRequest { Description = new string('a', 501) });
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void UpdateDictType_AllNulls_NoValidationErrors()
        {
            var v = new UpdateDictTypeRequestValidation();
            var result = v.TestValidate(new UpdateDictTypeRequest());
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ─── CreateDictItemRequestValidation ──────────────────────────────────

        [Fact]
        public void CreateDictItem_ZeroDictTypeId_HasValidationError()
        {
            var v = new CreateDictItemRequestValidation();
            var result = v.TestValidate(new CreateDictItemRequest { DictTypeId = 0, Code = "m", Label = "Male" });
            result.ShouldHaveValidationErrorFor(x => x.DictTypeId);
        }

        [Fact]
        public void CreateDictItem_NegativeDictTypeId_HasValidationError()
        {
            var v = new CreateDictItemRequestValidation();
            var result = v.TestValidate(new CreateDictItemRequest { DictTypeId = -1, Code = "m", Label = "Male" });
            result.ShouldHaveValidationErrorFor(x => x.DictTypeId);
        }

        [Fact]
        public void CreateDictItem_EmptyCode_HasValidationError()
        {
            var v = new CreateDictItemRequestValidation();
            var result = v.TestValidate(new CreateDictItemRequest { DictTypeId = 1, Code = "", Label = "Male" });
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void CreateDictItem_EmptyLabel_HasValidationError()
        {
            var v = new CreateDictItemRequestValidation();
            var result = v.TestValidate(new CreateDictItemRequest { DictTypeId = 1, Code = "m", Label = "" });
            result.ShouldHaveValidationErrorFor(x => x.Label);
        }

        [Fact]
        public void CreateDictItem_CodeTooLong_HasValidationError()
        {
            var v = new CreateDictItemRequestValidation();
            var result = v.TestValidate(new CreateDictItemRequest { DictTypeId = 1, Code = new string('a', 51), Label = "Male" });
            result.ShouldHaveValidationErrorFor(x => x.Code);
        }

        [Fact]
        public void CreateDictItem_LabelTooLong_HasValidationError()
        {
            var v = new CreateDictItemRequestValidation();
            var result = v.TestValidate(new CreateDictItemRequest { DictTypeId = 1, Code = "m", Label = new string('a', 101) });
            result.ShouldHaveValidationErrorFor(x => x.Label);
        }

        [Fact]
        public void CreateDictItem_Valid_NoValidationErrors()
        {
            var v = new CreateDictItemRequestValidation();
            var result = v.TestValidate(new CreateDictItemRequest { DictTypeId = 1, Code = "m", Label = "Male" });
            result.ShouldNotHaveAnyValidationErrors();
        }

        // ─── UpdateDictItemRequestValidation ──────────────────────────────────

        [Fact]
        public void UpdateDictItem_LabelTooLong_HasValidationError()
        {
            var v = new UpdateDictItemRequestValidation();
            var result = v.TestValidate(new UpdateDictItemRequest { Label = new string('a', 101) });
            result.ShouldHaveValidationErrorFor(x => x.Label);
        }

        [Fact]
        public void UpdateDictItem_NullLabel_NoValidationError()
        {
            var v = new UpdateDictItemRequestValidation();
            var result = v.TestValidate(new UpdateDictItemRequest());
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
