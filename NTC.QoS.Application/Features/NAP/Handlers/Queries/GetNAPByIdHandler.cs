using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetNAPByIdHandler(INAPRepository NAPRepository) : IRequestHandler<GetNAPByIdRequest, ApiResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<ApiResponse<NAPDto>> Handle(GetNAPByIdRequest request, CancellationToken cancellationToken)
    {
        var NAPQuery = _NAPRepository.Query(n => n.Id.Equals(request.Id));

        var NAPDtoQuery = NAPQuery.Select(NAP => new NAPDto()
        {
            Id = NAP.Id,
            File = NAP.File != null ? new()
            {
                Id = NAP.File.Id,
                Name = NAP.File.Name,
                Path = NAP.File.Path,
            }: null,
            ServiceProvider = NAP.ServiceProvider != null ? new()
            {
                Id = NAP.ServiceProvider.Id,
                Service = NAP.ServiceProvider.Service != null ? new()
                {
                    Id = NAP.ServiceProvider.Service.Id,
                    Name = NAP.ServiceProvider.Service.Name,
                    Type = EnumHelper<ServiceTypeEnum>.GetName(NAP.ServiceProvider.Service.Type)
                }: null,
                Provider = NAP.ServiceProvider.Provider != null ? new()
                {
                    Id = NAP.ServiceProvider.Provider.Id,
                    Name = NAP.ServiceProvider.Provider.Name,
                    Color = NAP.ServiceProvider.Provider.Color
                }: null
            }: null,
            Zone = NAP.Zone != null ? new()
            {
                Id = NAP.Zone.Id,
                Name = NAP.Zone.Name,
                Barangay = NAP.Zone.Barangay != null ? new()
                {
                    Id = NAP.Zone.Barangay.Id,
                    Name = NAP.Zone.Barangay.Name,
                    Description = NAP.Zone.Barangay.Description,
                    Municipality = NAP.Zone.Barangay.Municipality != null ? new()
                    {
                        Id = NAP.Zone.Barangay.Municipality.Id,
                        IsCity = NAP.Zone.Barangay.Municipality.IsCity,
                        Name = NAP.Zone.Barangay.Municipality.Name,
                        Description = NAP.Zone.Barangay.Municipality.Description,
                        Province = NAP.Zone.Barangay.Municipality.Province != null ? new()
                        {
                            Id = NAP.Zone.Barangay.Municipality.Province.Id,
                            Name = NAP.Zone.Barangay.Municipality.Province.Name,
                            Description = NAP.Zone.Barangay.Municipality.Province.Description,
                            Region = NAP.Zone.Barangay.Municipality.Province.Region != null ? new ()
                            {
                                Id = NAP.Zone.Barangay.Municipality.Province.Region!.Id,
                                Name = NAP.Zone.Barangay.Municipality.Province.Region!.Name,
                                Description = NAP.Zone.Barangay.Municipality.Province.Region!.Description
                            } : null
                        } : null
                    } : null
                } : null
            } : null,
            CoorLatitude = NAP.CoorLatitude ?? string.Empty,
            CoorLongitude = NAP.CoorLongitude ?? string.Empty,
        });

        var NAP = await _NAPRepository.FirstOrDefaultAsync(NAPDtoQuery, cancellationToken);

        if(NAP is null)
        {
            return new ApiResponse<NAPDto>("NAP not found");
        }

        return new ApiResponse<NAPDto>
        {
            Succeeded = true,
            Message = "NAP details retrieved successfully.",
            Data = NAP

        };
    }
}
