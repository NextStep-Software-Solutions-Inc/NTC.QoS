using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetProviderByIdRequestHandler(IRepository<Domain.Entities.Provider> repository)
    : IRequestHandler<GetProviderByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;

    public async Task<ApiResponse<ProviderDto>> Handle(GetProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var provider = await _repository
            .Find(p => p.Id == request.Id && !p.IsDeleted)
            .Select(p => new ProviderDto
            {
                Id = p.Id,
                Name = p.Name,
                LicenseNumber = p.LicenseNumber,
                CreatedBy = p.CreatedBy,
                CreatedDate = p.CreatedAt
            }).FirstOrDefaultAsync(cancellationToken);

        if (provider == null)
        {
            return new ApiResponse<ProviderDto>("No provider found.");
        }

        return new ApiResponse<ProviderDto>(provider, true, "Retrieved successfully.");
    }
}

