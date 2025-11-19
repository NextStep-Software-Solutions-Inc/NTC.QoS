using FluentValidation;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Validators;

public class BoundingBoxDtoValidator : AbstractValidator<BoundingBoxDto>
{
    public BoundingBoxDtoValidator()
    {
        RuleFor(b => b.MinLatitude)
            .GreaterThanOrEqualTo(GeoSpatialConstants.MinSupportedLatitude)
            .WithMessage($"Minimum latitude must be at least {GeoSpatialConstants.MinSupportedLatitude}°.")
            .LessThanOrEqualTo(GeoSpatialConstants.MaxSupportedLatitude)
            .WithMessage($"Minimum latitude must not exceed {GeoSpatialConstants.MaxSupportedLatitude}°.");

        RuleFor(b => b.MaxLatitude)
            .GreaterThanOrEqualTo(GeoSpatialConstants.MinSupportedLatitude)
            .WithMessage($"Maximum latitude must be at least {GeoSpatialConstants.MinSupportedLatitude}°.")
            .LessThanOrEqualTo(GeoSpatialConstants.MaxSupportedLatitude)
            .WithMessage($"Maximum latitude must not exceed {GeoSpatialConstants.MaxSupportedLatitude}°.");

        RuleFor(b => b.MinLongitude)
            .GreaterThanOrEqualTo(GeoSpatialConstants.MinSupportedLongitude)
            .WithMessage($"Minimum longitude must be at least {GeoSpatialConstants.MinSupportedLongitude}°.")
            .LessThanOrEqualTo(GeoSpatialConstants.MaxSupportedLongitude)
            .WithMessage($"Minimum longitude must not exceed {GeoSpatialConstants.MaxSupportedLongitude}°.");

        RuleFor(b => b.MaxLongitude)
            .GreaterThanOrEqualTo(GeoSpatialConstants.MinSupportedLongitude)
            .WithMessage($"Maximum longitude must be at least {GeoSpatialConstants.MinSupportedLongitude}°.")
            .LessThanOrEqualTo(GeoSpatialConstants.MaxSupportedLongitude)
            .WithMessage($"Maximum longitude must not exceed {GeoSpatialConstants.MaxSupportedLongitude}°.");

        // Validate that min coordinates are less than max coordinates
        RuleFor(b => b.MinLatitude)
            .LessThan(b => b.MaxLatitude)
            .WithMessage("Minimum latitude must be less than maximum latitude.");

        RuleFor(b => b.MinLongitude)
            .LessThan(b => b.MaxLongitude)
            .WithMessage("Minimum longitude must be less than maximum longitude.");

        // Validate area size limits
        RuleFor(b => b)
            .Must(HaveValidAreaSize)
            .WithMessage(GetAreaSizeValidationMessage);
    }

    private static bool HaveValidAreaSize(BoundingBoxDto boundingBox)
    {
        var latDiff = boundingBox.MaxLatitude - boundingBox.MinLatitude;
        var lonDiff = boundingBox.MaxLongitude - boundingBox.MinLongitude;

        return latDiff <= GeoSpatialConstants.MaxLatitudeDifference &&
               lonDiff <= GeoSpatialConstants.MaxLongitudeDifference;
    }

    private static string GetAreaSizeValidationMessage(BoundingBoxDto boundingBox)
    {
        var latDiff = boundingBox.MaxLatitude - boundingBox.MinLatitude;
        var lonDiff = boundingBox.MaxLongitude - boundingBox.MinLongitude;

        return GeoSpatialConstants.GetAreaSizeErrorMessage(latDiff, lonDiff);
    }
}