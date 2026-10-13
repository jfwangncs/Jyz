using FluentValidation.TestHelper;
using Jyz.Model.Request;
using Jyz.Validations;

namespace Jyz.UnitTests.Validations
{
    public class UserValidationTests
    {
        // ─── UpdateUserRequestValidation ────────────────────────────────────────

        [Fact]
        public void UpdateUser_NickNameTooLong_HasValidationError()
        {
            var v = new UpdateUserRequestValidation();
            var result = v.TestValidate(new UpdateUserRequest { NickName = new string('a', 51) });
            result.ShouldHaveValidationErrorFor(x => x.NickName);
        }

        [Fact]
        public void UpdateUser_RealNameTooLong_HasValidationError()
        {
            var v = new UpdateUserRequestValidation();
            var result = v.TestValidate(new UpdateUserRequest { RealName = new string('a', 51) });
            result.ShouldHaveValidationErrorFor(x => x.RealName);
        }

        [Fact]
        public void UpdateUser_PhoneTooLong_HasValidationError()
        {
            var v = new UpdateUserRequestValidation();
            var result = v.TestValidate(new UpdateUserRequest { Phone = new string('1', 51) });
            result.ShouldHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void UpdateUser_AllNulls_NoValidationErrors()
        {
            var v = new UpdateUserRequestValidation();
            var result = v.TestValidate(new UpdateUserRequest());
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateUser_ValidValues_NoValidationErrors()
        {
            var v = new UpdateUserRequestValidation();
            var result = v.TestValidate(new UpdateUserRequest
            {
                NickName = "Nick",
                RealName = "Real Name",
                Phone = "1234567890"
            });
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
