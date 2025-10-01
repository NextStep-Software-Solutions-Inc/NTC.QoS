using FluentValidation;
using NTC.QoS.Application.Features.Reports.WiredBroadband.Requests;

namespace NTC.QoS.Application.Features.Reports.WiredBroadband.Handlers.Validators;

public class GetWiredBroadbandReportQueryValidator : AbstractValidator<GetWiredBroadbandReportQuery>
{
    public GetWiredBroadbandReportQueryValidator()
    {
        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("Start date must be before end date.");

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.EndDate.HasValue)
            .WithMessage("End date cannot be in the future.");

        // Validate that region, province, and municipality hierarchy is respected
        RuleFor(x => x)
            .Must(request => request.RegionId.HasValue || request.ProvinceId.HasValue || request.MunicipalityId.HasValue || true)
            .WithMessage("At least one geographic filter should be specified for optimal performance, though it's not required.");

        // Logical validation: if municipality is specified, province and region should not conflict
        When(x => x.MunicipalityId.HasValue, () => {
            RuleFor(x => x.ProvinceId)
                .Empty()
                .WithMessage("When Municipality ID is specified, Province ID should not be provided as it will be inferred.");
            
            RuleFor(x => x.RegionId)
                .Empty()
                .WithMessage("When Municipality ID is specified, Region ID should not be provided as it will be inferred.");
        });

        // Logical validation: if province is specified, region should not conflict  
        When(x => x.ProvinceId.HasValue && !x.MunicipalityId.HasValue, () => {
            RuleFor(x => x.RegionId)
                .Empty()
                .WithMessage("When Province ID is specified, Region ID should not be provided as it will be inferred.");
        });
    }
}