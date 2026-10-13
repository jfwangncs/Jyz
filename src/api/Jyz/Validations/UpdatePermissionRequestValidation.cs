using FluentValidation;
using Jyz.Model.Permission;

namespace Jyz.Validations
{
    public class UpdatePermissionRequestValidation : AbstractValidator<UpdatePermissionRequest>
    {
        public UpdatePermissionRequestValidation()
        {
            RuleFor(x => x.Name)
                .MaximumLength(50);

            RuleFor(x => x.Description)
                .MaximumLength(200);

            RuleFor(x => x.Icon)
                .MaximumLength(50);
        }
    }
}
