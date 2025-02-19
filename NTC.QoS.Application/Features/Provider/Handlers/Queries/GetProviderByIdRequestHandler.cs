using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Queries;

public class GetProviderByIdRequestHandler(IRepository<ProviderDto> repository) : IRequestHandler<GetProviderByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<ProviderDto> _repository = repository;

    public async Task<ApiResponse<ProviderDto>> Handle(GetProviderByIdRequest request, CancellationToken cancellationToken)
    {
        var provider = await _repository.Find(p => p.Id == request.Id && p.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);

        return new ApiResponse<ProviderDto>
        {
            Succeeded = true,
            Message = "Provider retrieved successfully",
            Data = provider
        };
    }
}

