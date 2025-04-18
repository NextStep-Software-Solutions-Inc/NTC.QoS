using MediatR;
using NTC.QoS.Application.Features.NAP.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.NAP;

namespace NTC.QoS.Application.Features.NAP.Handlers.Queries;

public class GetAllNAPsHandler(INAPRepository NAPRepository) : IRequestHandler<GetAllNAPsRequest, PaginatedResponse<NAPDto>>
{
    private readonly INAPRepository _NAPRepository = NAPRepository;

    public async Task<PaginatedResponse<NAPDto>> Handle(GetAllNAPsRequest request, CancellationToken cancellationToken)
    {
        var allNAPsQuery = _NAPRepository.Query(n => !n.IsDeleted);

        var NAPsDtoQuery = allNAPsQuery
            .OrderBy(n => n.CreatedAt)
            .Select(n => new NAPDto
            {
                Id = n.Id,
                ServiceProvider = new()
                {
                    Id = n.ServiceProvider.Id,
                    Service = new()
                    {
                        Id = n.ServiceProvider.Service.Id,
                        Name = n.ServiceProvider.Service.Name,
                        Type = n.ServiceProvider.Service.Type.ToString(),
                    },
                    Provider = new()
                    {
                        Id = n.ServiceProvider.Provider.Id,
                        Name = n.ServiceProvider.Provider.Name,
                        Color = n.ServiceProvider.Provider.Color,
                    }
                },
                Zone = n.Zone != null ? new()
                {
                    Id = n.Zone.Id,
                    Name = n.Zone.Name,
                    Barangay = new()
                    {
                        Id = n.Zone.Barangay.Id,
                        Name = n.Zone.Barangay.Name,
                        Description = n.Zone.Barangay.Description,
                        Municipality = new()
                        {
                            Id = n.Zone.Barangay.Municipality.Id,
                            Name = n.Zone.Barangay.Municipality.Name,
                            Description = n.Zone.Barangay.Description,
                            IsCity = n.Zone.Barangay.Municipality.IsCity,
                            Province = new()
                            {
                                Id = n.Zone.Barangay.Municipality.Province.Id,
                                Name = n.Zone.Barangay.Municipality.Province.Name,
                                Description = n.Zone.Barangay.Municipality.Province.Description,
                                Region = new()
                                {
                                    Id = n.Zone.Barangay.Municipality.Province.Region.Id,
                                    Name = n.Zone.Barangay.Municipality.Province.Region.Name,
                                    Description = n.Zone.Barangay.Municipality.Province.Region.Description
                                }
                            }
                        }
                    }
                } : null,
                File = n.File != null ? new()
                {
                    Id = n.File.Id,
                    Name = n.File.Name,
                    Path = n.File.Path,
                } : null,
                CoorLatitude = n.CoorLatitude,
                CoorLongitude = n.CoorLongitude,
            });

        NAPsDtoQuery = NAPsDtoQuery
            .Where(n => !request.ServiceProviderId.HasValue || n.ServiceProvider.Id == request.ServiceProviderId)
            .Where(n => !request.ZoneId.HasValue || n.Zone == null || n.Zone.Id == request.ZoneId)
            .Where(n => !request.FileId.HasValue || n.File == null || n.File.Id == request.FileId);


        return await _NAPRepository.PaginateAsync(NAPsDtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    
    }
}
