using Core.Message.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactEntities;

namespace ContactServices.Validations
{
    public class AddContactValidator: AbstractValidator<Contact>
    {
        public AddContactValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("{PropertyName} not null");
               
        }
    }
}
