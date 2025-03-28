using FluentValidation;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    private readonly long _maxFileSizeInBytes = 10 * 1024 * 1024;

    public UploadFileRequestValidator()
    {
        RuleFor(request => request.File)
            .NotNull()
            .WithMessage("Please upload a file.");

        When(request => request.File != null, () =>
        {
            RuleFor(request => request.File.File.Length)
                .GreaterThan(0)
                .WithMessage("File cannot be empty.")
                .LessThanOrEqualTo(_maxFileSizeInBytes)
                .WithMessage($"File size must not exceed {_maxFileSizeInBytes / (1024 * 1024)} MB.");

            RuleFor(request => request.File.File.ContentType)
                .Must(BeSupportedFileType)
                .WithMessage("Only PNG, PDF, and Excel files are allowed.");

            RuleFor(request => request.File.Type)
                .NotEmpty()
                .WithMessage("Document type is required.")
                .Must(BeValidFileType)
                .WithMessage($"Document type must be one of: {string.Join(", ", Enum.GetNames(typeof(FileTypeEnum)).Where(n => n != FileTypeEnum.Unknown.ToString()))}.");
        });
    }

    private bool BeSupportedFileType(string contentType)
    {
        return contentType switch
        {
            "image/png" => true,
            "application/pdf" => true,
            "application/vnd.ms-excel" => true,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => true,
            _ => false,
        };
    }

    private bool BeValidFileType(string fileType)
    {
        return Enum.IsDefined(typeof(FileTypeEnum), fileType);
    }
}