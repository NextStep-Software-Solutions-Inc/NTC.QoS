using FluentValidation;
using NTC.QoS.Application.Features.BlobStorage.Requests;

namespace NTC.QoS.Application.Features.BlobStorage.Validators;

/// <summary>
/// Common validation rules for blob storage operations
/// </summary>
public static class BlobValidationRules
{
    private const long MaxFileSizeInBytes = 1_073_741_824; // 1GB
    private const int MaxFileNameLength = 1024;
    private const int MaxPrefixLength = 256;
    private const int MinContainerNameLength = 3;
    private const int MaxContainerNameLength = 63;

    /// <summary>
    /// Validates Azure Blob Storage container name
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidateContainerName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Container name is required")
            .Length(MinContainerNameLength, MaxContainerNameLength)
            .WithMessage($"Container name must be between {MinContainerNameLength} and {MaxContainerNameLength} characters")
            .Matches("^[a-z0-9](?!.*--)[a-z0-9-]{1,61}[a-z0-9]$")
            .WithMessage("Container name must be lowercase alphanumeric with hyphens, and cannot start or end with hyphen or contain consecutive hyphens");
    }

    /// <summary>
    /// Validates blob file name
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidateFileName<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("File name is required")
            .MaximumLength(MaxFileNameLength)
            .WithMessage($"File name cannot exceed {MaxFileNameLength} characters");
    }

    /// <summary>
    /// Validates content type (MIME type)
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidateContentType<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .WithMessage("Content type is required")
            .Matches(@"^[\w-]+/[\w\-\+\.]+$")
            .WithMessage("Content type must be a valid MIME type (e.g., image/jpeg, application/pdf)");
    }

    /// <summary>
    /// Validates file stream for upload
    /// </summary>
    public static IRuleBuilderOptions<T, Stream> ValidateFileStream<T>(this IRuleBuilder<T, Stream> ruleBuilder)
    {
        return ruleBuilder
            .NotNull()
            .WithMessage("File stream is required")
            .Must(stream => stream != Stream.Null && stream.Length > 0)
            .WithMessage("File stream cannot be empty")
            .Must(stream => stream.Length <= MaxFileSizeInBytes)
            .WithMessage($"File size cannot exceed {MaxFileSizeInBytes / 1_073_741_824}GB");
    }

    /// <summary>
    /// Validates prefix for blob listing
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidatePrefix<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(MaxPrefixLength)
            .WithMessage($"Prefix cannot exceed {MaxPrefixLength} characters");
    }
}

/// <summary>
/// Validator for UploadBlobRequest
/// </summary>
public class UploadBlobRequestValidator : AbstractValidator<UploadBlobRequest>
{
    public UploadBlobRequestValidator()
    {
        RuleFor(x => x.ContainerName).ValidateContainerName();
        RuleFor(x => x.FileName).ValidateFileName();
        RuleFor(x => x.ContentType).ValidateContentType();
        RuleFor(x => x.FileStream).ValidateFileStream();
    }
}

/// <summary>
/// Validator for DownloadBlobRequest
/// </summary>
public class DownloadBlobRequestValidator : AbstractValidator<DownloadBlobRequest>
{
    public DownloadBlobRequestValidator()
    {
        RuleFor(x => x.ContainerName).ValidateContainerName();
        RuleFor(x => x.FileName).ValidateFileName();
    }
}

/// <summary>
/// Validator for DownloadBlobAsBytesRequest
/// </summary>
public class DownloadBlobAsBytesRequestValidator : AbstractValidator<DownloadBlobAsBytesRequest>
{
    public DownloadBlobAsBytesRequestValidator()
    {
        RuleFor(x => x.ContainerName).ValidateContainerName();
        RuleFor(x => x.FileName).ValidateFileName();
    }
}

/// <summary>
/// Validator for DeleteBlobRequest
/// </summary>
public class DeleteBlobRequestValidator : AbstractValidator<DeleteBlobRequest>
{
    public DeleteBlobRequestValidator()
    {
        RuleFor(x => x.ContainerName).ValidateContainerName();
        RuleFor(x => x.FileName).ValidateFileName();
    }
}

/// <summary>
/// Validator for BlobExistsRequest
/// </summary>
public class BlobExistsRequestValidator : AbstractValidator<BlobExistsRequest>
{
    public BlobExistsRequestValidator()
    {
        RuleFor(x => x.ContainerName).ValidateContainerName();
        RuleFor(x => x.FileName).ValidateFileName();
    }
}

/// <summary>
/// Validator for ListBlobsRequest
/// </summary>
public class ListBlobsRequestValidator : AbstractValidator<ListBlobsRequest>
{
    public ListBlobsRequestValidator()
    {
        RuleFor(x => x.ContainerName).ValidateContainerName();

        RuleFor(static x => x.Prefix)
            .ValidatePrefix()
            .When(x => !string.IsNullOrEmpty(x.Prefix));
    }
}