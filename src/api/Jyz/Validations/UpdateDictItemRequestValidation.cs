using FluentValidation;
using Jyz.Model.DictItem;

namespace Jyz.Validations
{
    public class UpdateDictItemRequestValidation : AbstractValidator<UpdateDictItemRequest>
    {
        public UpdateDictItemRequestValidation()
        {
            RuleFor(x => x.Label).MaximumLength(100);
        }
    }
}
