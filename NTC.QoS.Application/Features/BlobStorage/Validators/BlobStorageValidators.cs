using FluentValidation;
using NTC.QoS.Application.Features.BlobStorage.Requests;

namespace NTC.QoS.Application.Features.BlobStorage.Validators;

/// <summary>
/// Validator for UploadBlobRequest
/// </summary>
public class UploadBlobRequestValidator : AbstractValidator<UploadBlobRequest>
{
    public UploadBlobRequestValidator()
    {
        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("Container name is required")
            .Matches("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")
            .WithMessage("Container name must be 3-63 characters long, lowercase alphanumeric with hyphens, and cannot start or end with hyphen or contain consecutive hyphens");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .MaximumLength(1024)
            .WithMessage("File name cannot exceed 1024 characters");

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("Content type is required");

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File stream is required")
            .Must(stream => stream != Stream.Null && stream.Length > 0)
            .WithMessage("File stream cannot be empty");
    }
}

/// <summary>
/// Validator for DownloadBlobRequest
/// </summary>
public class DownloadBlobRequestValidator : AbstractValidator<DownloadBlobRequest>
{
    public DownloadBlobRequestValidator()
    {
        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("Container name is required")
            .Matches("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")
            .WithMessage("Container name must be 3-63 characters long, lowercase alphanumeric with hyphens, and cannot start or end with hyphen or contain consecutive hyphens");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .MaximumLength(1024)
            .WithMessage("File name cannot exceed 1024 characters");
    }
}

/// <summary>
/// Validator for DownloadBlobAsBytesRequest
/// </summary>
public class DownloadBlobAsBytesRequestValidator : AbstractValidator<DownloadBlobAsBytesRequest>
{
    public DownloadBlobAsBytesRequestValidator()
    {
        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("Container name is required")
            .Matches("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")
            .WithMessage("Container name must be 3-63 characters long, lowercase alphanumeric with hyphens, and cannot start or end with hyphen or contain consecutive hyphens");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .MaximumLength(1024)
            .WithMessage("File name cannot exceed 1024 characters");
    }
}

/// <summary>
/// Validator for DeleteBlobRequest
/// </summary>
public class DeleteBlobRequestValidator : AbstractValidator<DeleteBlobRequest>
{
    public DeleteBlobRequestValidator()
    {
        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("Container name is required")
            .Matches("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")
            .WithMessage("Container name must be 3-63 characters long, lowercase alphanumeric with hyphens, and cannot start or end with hyphen or contain consecutive hyphens");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .MaximumLength(1024)
            .WithMessage("File name cannot exceed 1024 characters");
    }
}

/// <summary>
/// Validator for BlobExistsRequest
/// </summary>
public class BlobExistsRequestValidator : AbstractValidator<BlobExistsRequest>
{
    public BlobExistsRequestValidator()
    {
        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("Container name is required")
            .Matches("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")
            .WithMessage("Container name must be 3-63 characters long, lowercase alphanumeric with hyphens, and cannot start or end with hyphen or contain consecutive hyphens");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("File name is required")
            .MaximumLength(1024)
            .WithMessage("File name cannot exceed 1024 characters");
    }
}

/// <summary>
/// Validator for ListBlobsRequest
/// </summary>
public class ListBlobsRequestValidator : AbstractValidator<ListBlobsRequest>
{
    public ListBlobsRequestValidator()
    {
        RuleFor(x => x.ContainerName)
            .NotEmpty()
            .WithMessage("Container name is required")
            .Matches("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")
            .WithMessage("Container name must be 3-63 characters long, lowercase alphanumeric with hyphens, and cannot start or end with hyphen or contain consecutive hyphens");

        RuleFor(x => x.Prefix)
            .MaximumLength(256)
            .WithMessage("Prefix cannot exceed 256 characters")
            .When(x => !string.IsNullOrEmpty(x.Prefix));
    }
}