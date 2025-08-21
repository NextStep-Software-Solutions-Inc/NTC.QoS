using FluentValidation;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Features.Analytics.Handlers.Validators;

public class GetPointBroadbandInfoRequestValidator : AbstractValidator<GetPointBroadbandInfoRequest>
{
    public GetPointBroadbandInfoRequestValidator()
    {
        RuleFor(x => x.PointRadius)
            .NotNull()
            .WithMessage("Point radius information is required.")
            .SetValidator(new PointRadiusDtoValidator());
    }
}

public class PointRadiusDtoValidator : AbstractValidator<PointRadiusDto>
{
    public PointRadiusDtoValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(GeoSpatialConstants.MinSupportedLatitude, GeoSpatialConstants.MaxSupportedLatitude)
            .WithMessage($"Latitude must be between {GeoSpatialConstants.MinSupportedLatitude}° and {GeoSpatialConstants.MaxSupportedLatitude}°");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(GeoSpatialConstants.MinSupportedLongitude, GeoSpatialConstants.MaxSupportedLongitude)
            .WithMessage($"Longitude must be between {GeoSpatialConstants.MinSupportedLongitude}° and {GeoSpatialConstants.MaxSupportedLongitude}°");

        RuleFor(x => x.RadiusMeters)
            .GreaterThan(0)
            .WithMessage("Radius must be greater than 0 meters")
            .LessThanOrEqualTo(50000) // 50km max radius
            .WithMessage("Radius cannot exceed 50,000 meters (50km) for performance reasons");
    }
}