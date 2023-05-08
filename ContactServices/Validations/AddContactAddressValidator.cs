using ContactMessages.Request;
using FluentValidation;

namespace ContactServices.Validations
{
    public class AddContactAddressValidator : AbstractValidator<AddContactDetail>
    {        
        public AddContactAddressValidator() 
        {
            RuleFor(x => x.ContactId).GreaterThan(0).InclusiveBetween(1, long.MaxValue).WithMessage("{PropertyName} must be greater than 0");
        }
    }
}
