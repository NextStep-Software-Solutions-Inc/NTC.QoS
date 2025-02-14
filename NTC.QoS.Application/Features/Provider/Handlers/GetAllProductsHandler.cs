using MediatR;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Application.Features.Provider.Queries;

namespace NTC.QoS.Application.Features.Provider.Handler;

public class GetAllProvidersHandler(IProviderRepository productRepository) : IRequestHandler<GetAllProvidersQuery, List<NTC.QoS.Domain.Entities.Provider>>
{
    private readonly IProviderRepository _productRepository = productRepository;

    public async Task<List<NTC.QoS.Domain.Entities.Provider>> Handle(GetAllProvidersQuery request, CancellationToken cancellationToken)
    {
        return await _productRepository.GetAllAsync();
    }
}

