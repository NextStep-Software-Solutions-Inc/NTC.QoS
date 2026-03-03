using MediatR;
using NTC.QoS.Application.Features.FrequencyAllocation.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.FrequencyAllocation;
using NTC.QoS.Domain.Models.DTOs.Link;
using NTC.QoS.Domain.Models.DTOs.Provider;
using NTC.QoS.Domain.Models.DTOs.Service;
using NTC.QoS.Domain.Models.DTOs.ServiceProvider;

namespace NTC.QoS.Application.Features.FrequencyAllocation.Handlers.Queries;

public class GetAllFrequencyAllocationsHandler(IFrequencyAllocationRepository repository)
    : IRequestHandler<GetAllFrequencyAllocationsRequest, PaginatedResponse<FrequencyAllocationDto>>
{
    private readonly IFrequencyAllocationRepository _repository = repository;

    public async Task<PaginatedResponse<FrequencyAllocationDto>> Handle(
        GetAllFrequencyAllocationsRequest request,
        CancellationToken cancellationToken)
    {
        var query = _repository.Query();

        if (request.ProviderIds is { Count: > 0 })
            query = query.Where(fa => request.ProviderIds.Contains(fa.ProviderId));

        if (request.IsActive.HasValue)
            query = query.Where(fa => fa.IsActive == request.IsActive.Value);

        if (!string.IsNullOrEmpty(request.Search))
            query = _repository.ApplySearch(query, request.Search, fa => fa.Name, fa => fa.Provider.Name);

        var dtoQuery = query
            .OrderBy(fa => fa.Provider.Name)
            .ThenBy(fa => fa.Name)
            .Select(fa => new FrequencyAllocationDto
            {
                Id       = fa.Id,
                Name     = fa.Name,
                Notes    = fa.Notes,
                IsActive = fa.IsActive,
                Provider = new ProviderDto
                {
                    Id            = fa.Provider.Id,
                    Name          = fa.Provider.Name,
                    Color         = fa.Provider.Color,
                    LicenseNumber = fa.Provider.LicenseNumber,
                },
                UpLink = new LinkDto
                {
                    Id   = fa.UpLink.Id,
                    Type = fa.UpLink.Type.ToString(),
                    Band = new BandDto { Id = fa.UpLink.Band.Id, Name = fa.UpLink.Band.Name, Description = fa.UpLink.Band.Description },
                    ServiceProvider = new ServiceProviderDto
                    {
                        Id       = fa.UpLink.ServiceProvider.Id,
                        MCC      = fa.UpLink.ServiceProvider.MCC,
                        MNC      = fa.UpLink.ServiceProvider.MNC,
                        Provider = new ProviderDto { Id = fa.UpLink.ServiceProvider.Provider.Id, Name = fa.UpLink.ServiceProvider.Provider.Name },
                        Service  = new ServiceDto  { Id = fa.UpLink.ServiceProvider.Service.Id,  Name = fa.UpLink.ServiceProvider.Service.Name, Type = fa.UpLink.ServiceProvider.Service.Type.ToString() },
                    },
                    CenterFrequency = fa.UpLink.CenterFrequency,
                    LowerFrequency  = fa.UpLink.LowerFrequency,
                    UpperFrequency  = fa.UpLink.UpperFrequency,
                    RxThreshold     = fa.UpLink.RxThreshold,
                },
                DownLink = new LinkDto
                {
                    Id   = fa.DownLink.Id,
                    Type = fa.DownLink.Type.ToString(),
                    Band = new BandDto { Id = fa.DownLink.Band.Id, Name = fa.DownLink.Band.Name, Description = fa.DownLink.Band.Description },
                    ServiceProvider = new ServiceProviderDto
                    {
                        Id       = fa.DownLink.ServiceProvider.Id,
                        MCC      = fa.DownLink.ServiceProvider.MCC,
                        MNC      = fa.DownLink.ServiceProvider.MNC,
                        Provider = new ProviderDto { Id = fa.DownLink.ServiceProvider.Provider.Id, Name = fa.DownLink.ServiceProvider.Provider.Name },
                        Service  = new ServiceDto  { Id = fa.DownLink.ServiceProvider.Service.Id,  Name = fa.DownLink.ServiceProvider.Service.Name, Type = fa.DownLink.ServiceProvider.Service.Type.ToString() },
                    },
                    CenterFrequency = fa.DownLink.CenterFrequency,
                    LowerFrequency  = fa.DownLink.LowerFrequency,
                    UpperFrequency  = fa.DownLink.UpperFrequency,
                    RxThreshold     = fa.DownLink.RxThreshold,
                },
            });

        return await _repository.PaginateAsync(dtoQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
