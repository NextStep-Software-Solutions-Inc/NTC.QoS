using FluentValidation;
using NTC.QoS.Application.Features.GenAI.Requests;

namespace NTC.QoS.Application.Features.GenAI.Validators;

/// <summary>
/// Validator for report generation requests
/// </summary>
public class GenerateReportRequestValidator : AbstractValidator<GenerateReportRequest>
{
    public GenerateReportRequestValidator()
    {
        RuleFor(x => x.Request.Prompt)
            .NotEmpty().WithMessage("Report instructions are required")
            .MaximumLength(10000).WithMessage("Instructions cannot exceed 10,000 characters");

        RuleFor(x => x.Request.Files)
            .Must(files => files == null || files.Count <= 10)
            .WithMessage("Cannot upload more than 10 files");
    }
}
