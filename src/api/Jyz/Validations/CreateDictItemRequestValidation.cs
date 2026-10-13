using FluentValidation;
using Jyz.Model.DictItem;

namespace Jyz.Validations
{
    public class CreateDictItemRequestValidation : AbstractValidator<CreateDictItemRequest>
    {
        public CreateDictItemRequestValidation()
        {
            RuleFor(x => x.DictTypeId).GreaterThan(0);
            RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        }
    }
}
