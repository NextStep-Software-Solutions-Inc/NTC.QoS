using FluentValidation;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Models.DTOs.File;

public class FileUploadValidator : AbstractValidator<UploadFileDto>
{
    public FileUploadValidator()
    {
        RuleFor(p => p.Type)
            .NotEmpty().WithMessage("Document type is required.")
            .Must(doc => Enum.IsDefined(typeof(FileTypeEnum), doc))
            .WithMessage("Document type must be one of: NAP, CELL, SPEEDTEST, CELLULAR.");

        RuleFor(p => p.File)
            .NotNull().WithMessage("File is required.")
            .Must(file => file.Length > 0).WithMessage("File cannot be empty.");
    }
    private bool BeAValidDocType(string docType)
    {
        return Enum.TryParse(typeof(FileTypeEnum), docType, true, out _);
    }
}
