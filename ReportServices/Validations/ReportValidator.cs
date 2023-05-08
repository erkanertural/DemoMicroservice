using FluentValidation;
using ReportEntities;

namespace ContactServices.Validations
{
    public class ReportValidator: AbstractValidator<Report>
    {
        public ReportValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("{PropertyName} not null");
               
        }
    }
}
