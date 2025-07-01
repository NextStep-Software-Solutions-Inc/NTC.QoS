using MediatR;
using NTC.QoS.Application.Features.Link.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Link;

namespace NTC.QoS.Application.Features.Link.Handlers.Commands;

public class UpdateLinkByIdHandler(ILinkRepository repository) : IRequestHandler<UpdateLinkByIdRequest, ApiResponse<LinkDto>>
{
    private readonly ILinkRepository _repository = repository;

    public async Task<ApiResponse<LinkDto>> Handle(UpdateLinkByIdRequest request,
            CancellationToken cancellationToken)
    {
        var linkQuery = _repository.Query(l => l.Id == request.Id);
        var link = await _repository.FirstOrDefaultAsync(linkQuery, cancellationToken);

        if (link is null)
        {
            return new ApiResponse<LinkDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "Id", ["Link not found."] }
                },
                Message = "The link you are trying to edit does not exist."
            };
        }

        if(!string.IsNullOrEmpty(request.Link.Type)) link.Type = EnumHelper<LinkTypeEnum>.ParseUserInput(request.Link.Type);
        link.CenterFrequency = request.Link.CenterFrequency ?? link.CenterFrequency;
        link.LowerFrequency = request.Link.LowerFrequency ?? link.LowerFrequency;
        link.UpperFrequency = request.Link.UpperFrequency ?? link.UpperFrequency;
        link.RxThreshold = request.Link.RxThreshold ?? link.RxThreshold;

        _repository.Update(link);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<LinkDto>
        {
            Succeeded = true,
            Data = new LinkDto()
            {
                Id = link.Id,
                Type = link.Type.ToString(),
                ServiceProvider = new()
                {
                    Id = link.ServiceProviderId,
                    Provider = new()
                    {
                        Id = link.ServiceProvider.Provider.Id,
                        Name = link.ServiceProvider.Provider.Name,
                        Description = link.ServiceProvider.Provider.Description,
                        Color = link.ServiceProvider.Provider.Color,
                        LicenseNumber = link.ServiceProvider.Provider.LicenseNumber
                    },
                    Service = new()
                    {
                        Id = link.ServiceProvider.ServiceId,
                        Name = link.ServiceProvider.Service.Name,
                        Type = link.ServiceProvider.Service.Type.ToString(),
                    }
                },
                Band = new()
                {
                    Id = link.Band.Id,
                    Name = link.Band.Name,
                    Description = link.Band.Description,
                },
                CenterFrequency = link.CenterFrequency,
                LowerFrequency = link.LowerFrequency,
                UpperFrequency = link.UpperFrequency,
                RxThreshold = link.RxThreshold
            },
            Message = "Link updated successfully."
        };
    }
}
