using FluentValidation;
using Jyz.Model.DictType;

namespace Jyz.Validations
{
    public class CreateDictTypeRequestValidation : AbstractValidator<CreateDictTypeRequest>
    {
        public CreateDictTypeRequestValidation()
        {
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
