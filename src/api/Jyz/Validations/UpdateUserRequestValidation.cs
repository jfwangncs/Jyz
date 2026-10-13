using FluentValidation;
using Jyz.Model.Request;

namespace Jyz.Validations
{
    public class UpdateUserRequestValidation : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidation()
        {
            RuleFor(x => x.NickName)
                .MaximumLength(50);

            RuleFor(x => x.RealName)
                .MaximumLength(50);

            RuleFor(x => x.Phone)
                .MaximumLength(50);
        }
    }
}
