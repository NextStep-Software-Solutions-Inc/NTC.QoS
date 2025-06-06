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
        var linkQuery = _repository.Query(l => l.Id == request.Id && !l.IsDeleted);
        var link = await _repository.FirstOrDefaultAsync(linkQuery, cancellationToken);

        if (link is null)
        {
            return new ApiResponse<UpdateLinkDto>
            {
                Data = null,
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Link not found."] }
                },
                Message = "The link you are trying to edit does not exist."
            };
        }
        link.Type = EnumHelper<LinkTypeEnum>.ParseUserInput(request.Link.Type);
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
            Errors = new Dictionary<string, string[]>
            {
                { "Link", ["Link data is required."]   }
            },
            Data = new UpdateLinkDto()
            {
                Type = EnumHelper<LinkTypeEnum>.GetDescription(link.Type),
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