using FluentValidation;
using NTC.QoS.Application.Features.GenAI.Requests;

namespace NTC.QoS.Application.Features.GenAI.Validators;

/// <summary>
/// Validator for chat requests
/// </summary>
public class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Request.Prompt)
            .NotEmpty().WithMessage("Prompt is required")
            .MaximumLength(10000).WithMessage("Prompt cannot exceed 10,000 characters");
    }
}
