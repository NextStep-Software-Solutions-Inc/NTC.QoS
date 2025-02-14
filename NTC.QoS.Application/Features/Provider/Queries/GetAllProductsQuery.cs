using MediatR;
namespace NTC.QoS.Application.Features.Provider.Queries;

public class GetAllProvidersQuery : IRequest<List<NTC.QoS.Domain.Entities.Provider>> { }


