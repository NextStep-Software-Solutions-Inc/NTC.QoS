using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Handlers.Commands;

public class UpdateLinkByIdHandler(IRepository<Domain.Aggregates.Link> repository) : IRequestHandler<UpdateLinkByIdRequest, ApiResponse<UpdateLinkDto>>
{
    private readonly IRepository<Domain.Aggregates.Link> _repository = repository;
    public async Task<ApiResponse<UpdateLinkDto>> Handle(UpdateLinkByIdRequest request,
            CancellationToken cancellationToken)
    {
        var link = await _repository.FindAsync(l => l.Id == request.Id && !l.IsDeleted, cancellationToken);

        if (link is null)
        {
            return new ApiResponse<UpdateLinkDto>
            {
                Data = null,
                Succeeded = false,
                Error = new[] { "Link not found." },
                Message = "The link you are trying to edit does not exist."
            };
        }
        link.Type = EnumHelper<LinkTypeEnum>.GetValue(request.Link.Type);
        link.ServiceProviderId = request.Link.ServiceProviderId;
        link.BandId = request.Link.BandId;
        link.CenterFrequency = request.Link.CenterFrequency;
        link.LowerFrequency = request.Link.LowerFrequency;
        link.UpperFrequency = request.Link.UpperFrequency;
        link.RxThreshold = request.Link.RxThreshold;    

        _repository.Update(link);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<UpdateLinkDto>
        {
            Succeeded = true,
            Error = new[] {""},
            Data = new UpdateLinkDto()
            {
                Type = EnumHelper<LinkTypeEnum>.GetDescription(link.Type),
                ServiceProviderId = link.ServiceProviderId,
                BandId = link.BandId,
                CenterFrequency = link.CenterFrequency,
                LowerFrequency = link.LowerFrequency,
                UpperFrequency = link.UpperFrequency,
                RxThreshold = link.RxThreshold
            },
            Message = "Request successful! Your changes have been saved."
        };
    }
}