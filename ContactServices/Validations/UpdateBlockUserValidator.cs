using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactEntities;

namespace ContactServices.Validations
{
    public class UpdateBlockUserValidator: AbstractValidator<Contact>
    {
        public UpdateBlockUserValidator()
        {
            RuleFor(x => x.Id).NotNull().InclusiveBetween(1, long.MaxValue).WithMessage("{PropertyName} must be greater than 0");
            RuleFor(x => x.Name).NotEmpty().WithMessage("{PropertyName} not null");
           
        }
    }
}
