using MediatR;
using NTC.QoS.Application.Features.Municipality.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.Municipality;

namespace NTC.QoS.Application.Features.Municipality.Handlers.Commands;

public class CreateMunicipalityHandler(IRepository<Domain.Entities.Municipality> repository) : IRequestHandler<CreateMunicipalityRequest, ApiResponse<CreateMunicipalityDto>>
{
    private readonly IRepository<Domain.Entities.Municipality> _repository = repository;

    public async Task<ApiResponse<CreateMunicipalityDto>> Handle(CreateMunicipalityRequest request, CancellationToken cancellationToken)
    {
        var existingMunicipality = await _repository.FindAsync(m => m.Name == request.Municipality.Name && m.ProvinceId == request.Municipality.ProvinceId && !m.IsDeleted, cancellationToken);

        if (existingMunicipality != null)
        {
            return new ApiResponse<CreateMunicipalityDto>
            {
                Message = "Municipality already exists.",
                Succeeded = false
            };
        }

        var newMunicipality = new Domain.Entities.Municipality
        {
            ProvinceId = request.Municipality.ProvinceId,
            Name = request.Municipality.Name,
            Description = request.Municipality.Description,
            IsCity = request.Municipality.IsCity,
        };

        _repository.Add(newMunicipality);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<CreateMunicipalityDto>
        {
            Data = new CreateMunicipalityDto
            {
                ProvinceId = newMunicipality.ProvinceId,
                Name = newMunicipality.Name,
                Description = newMunicipality.Description,
                IsCity = newMunicipality.IsCity,
            },

            Message = "Municipality created successfully.",
            Succeeded = true
        };
    }
}