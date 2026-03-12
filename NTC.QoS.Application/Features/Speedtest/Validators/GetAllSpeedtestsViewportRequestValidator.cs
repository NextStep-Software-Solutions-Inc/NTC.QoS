using FluentValidation;
using NTC.QoS.Application.Features.Speedtest.Requests;

namespace NTC.QoS.Application.Features.Speedtest.Validators;

public class GetAllSpeedtestsViewportRequestValidator : AbstractValidator<GetAllSpeedtestsViewportRequest>
{
    public GetAllSpeedtestsViewportRequestValidator()
    {
        // Viewport bounds validation
        RuleFor(x => x.QueryParams)
            .Must(q => q.IsViewportValid())
            .WithMessage("Invalid viewport bounds: minLatitude must be less than maxLatitude, and minLongitude must be less than maxLongitude");

        // Download speed validations
        RuleFor(x => x.MinDownloadSpeed)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinDownloadSpeed must be greater than or equal to 0")
            .When(x => x.MinDownloadSpeed.HasValue);

        RuleFor(x => x.MaxDownloadSpeed)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxDownloadSpeed must be greater than or equal to 0")
            .When(x => x.MaxDownloadSpeed.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinDownloadSpeed.HasValue || !x.MaxDownloadSpeed.HasValue 
                || x.MinDownloadSpeed <= x.MaxDownloadSpeed)
            .WithMessage("MinDownloadSpeed must not exceed MaxDownloadSpeed")
            .When(x => x.MinDownloadSpeed.HasValue && x.MaxDownloadSpeed.HasValue);

        // Upload speed validations
        RuleFor(x => x.MinUploadSpeed)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinUploadSpeed must be greater than or equal to 0")
            .When(x => x.MinUploadSpeed.HasValue);

        RuleFor(x => x.MaxUploadSpeed)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MaxUploadSpeed must be greater than or equal to 0")
            .When(x => x.MaxUploadSpeed.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinUploadSpeed.HasValue || !x.MaxUploadSpeed.HasValue 
                || x.MinUploadSpeed <= x.MaxUploadSpeed)
            .WithMessage("MinUploadSpeed must not exceed MaxUploadSpeed")
            .When(x => x.MinUploadSpeed.HasValue && x.MaxUploadSpeed.HasValue);

        // Date range validation
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue 
                || x.StartDate <= x.EndDate)
            .WithMessage("StartDate must not exceed EndDate")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
