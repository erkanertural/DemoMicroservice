using FluentValidation;
using ReportEntities;

namespace ContactServices.Validations
{
    public class ReportValidator: AbstractValidator<Report>
    {
        public ReportValidator()
        {
            RuleFor(x => x.ReportDate).NotEmpty().WithMessage("{PropertyName} not null");
               
        }
    }
}
