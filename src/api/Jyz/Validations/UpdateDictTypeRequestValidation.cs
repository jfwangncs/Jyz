using FluentValidation;
using Jyz.Model.DictType;

namespace Jyz.Validations
{
    public class UpdateDictTypeRequestValidation : AbstractValidator<UpdateDictTypeRequest>
    {
        public UpdateDictTypeRequestValidation()
        {
            RuleFor(x => x.Name).MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }
}
