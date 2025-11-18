using FluentValidation;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.File.Handlers.Validators;

public class DownloadTemplateFileRequestValidator : AbstractValidator<DownloadTemplateFileRequest>
{

    public DownloadTemplateFileRequestValidator()
    {
        RuleFor(x => x.Type)
             .NotEmpty()
             .WithMessage("File type is required.")
             .Must(BeValidFileType)
             .WithMessage($"Invalid file type. Must be one of {GetValidFileTypes()}");
    }

    private bool BeValidFileType(string fileType)
    {
        return Enum.IsDefined(typeof(FileTypeEnum), fileType);
    }

    private string GetValidFileTypes()
    {
        var validFileTypes = Enum.GetNames(typeof(FileTypeEnum));
        return string.Join(", ", validFileTypes);
    }
}