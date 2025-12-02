using FluentValidation;
using NTC.QoS.Application.Features.CellularCoverage.Requests;

namespace NTC.QoS.Application.Features.CellularCoverage.Validators;

/// <summary>
/// Validator for UploadCellularCoverageV2Command
/// ServiceProviderId and TechnologyId are derived from Network, not validated here
/// </summary>
public class UploadCellularCoverageV2Validator : AbstractValidator<UploadCellularCoverageV2Command>
{
    public UploadCellularCoverageV2Validator()
    {
        RuleFor(x => x.NetworkId)
            .NotEmpty()
            .WithMessage("NetworkId is required");

        RuleFor(x => x.BandId)
            .NotEmpty()
            .WithMessage("BandId is required");

        RuleFor(x => x.MunicipalityId)
            .NotEmpty()
            .WithMessage("MunicipalityId is required");

        RuleFor(x => x.CsvFile)
            .NotNull()
            .WithMessage("CSV file is required");

        RuleFor(x => x.CsvFile.Length)
            .GreaterThan(0)
            .WithMessage("CSV file must not be empty")
            .LessThan(100 * 1024 * 1024) // 100 MB max
            .WithMessage("CSV file size must not exceed 100 MB")
            .When(x => x.CsvFile != null);

        RuleFor(x => x.CsvFile.ContentType)
            .Must(ct => ct == "text/csv" || ct == "application/vnd.ms-excel" || ct == "application/csv")
            .WithMessage("File must be a CSV file")
            .When(x => x.CsvFile != null);

        RuleFor(x => x.CsvFile.FileName)
            .Must(fn => fn.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("File must have .csv extension")
            .When(x => x.CsvFile != null);
    }
}
