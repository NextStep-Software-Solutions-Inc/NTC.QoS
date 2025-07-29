using MediatR;
using NTC.QoS.Application.Features.TechnologyBand.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.TechnologyBand;
using NTC.QoS.Domain.Models.DTOs.Band;
using NTC.QoS.Domain.Models.DTOs.Technology;

namespace NTC.QoS.Application.Features.TechnologyBand.Handlers.Commands;

public class CreateTechnologyBandHandler(
    ITechnologyBandRepository repository,
    IBandRepository bandRepository,
    ITechnologyRepository technologyRepository)
    : IRequestHandler<CreateTechnologyBandRequest, ApiResponse<TechnologyBandDto>>
{
    private readonly ITechnologyBandRepository _repository = repository;
    private readonly IBandRepository _bandRepository = bandRepository;
    private readonly ITechnologyRepository _technologyRepository = technologyRepository;

    public async Task<ApiResponse<TechnologyBandDto>> Handle(CreateTechnologyBandRequest request, CancellationToken cancellationToken)
    {
        var bandQuery = _bandRepository.Query(b => b.Id == request.TechnologyBand.BandId);
        var band = await _bandRepository.FirstOrDefaultAsync(bandQuery, cancellationToken);
        if (band is null)
        {
            return new ApiResponse<TechnologyBandDto>
            {
                Succeeded = false,
                Message = "Band not found."
            };
        }

        var technologyQuery = _technologyRepository.Query(t => t.Id == request.TechnologyBand.TechnologyId);
        var technology = await _technologyRepository.FirstOrDefaultAsync(technologyQuery, cancellationToken);
        if (technology is null)
        {
            return new ApiResponse<TechnologyBandDto>
            {
                Succeeded = false,
                Message = "Technology not found."
            };
        }

        var existingQuery = _repository.QueryWithIgnoreQueryFilters(tb =>
            tb.BandId == request.TechnologyBand.BandId &&
            tb.TechnologyId == request.TechnologyBand.TechnologyId);
        var existing = await _repository.FirstOrDefaultAsync(existingQuery, cancellationToken);

        if (existing is not null && existing.IsDeleted)
        {
            existing.IsDeleted = false;
            _repository.Update(existing);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<TechnologyBandDto>
            {
                Data = new TechnologyBandDto
                {
                    Id = existing.Id,
                    Band = new BandDto
                    {
                        Id = band.Id,
                        Name = band.Name,
                        Description = band.Description
                    },
                    Technology = new TechnologyDto
                    {
                        Id = technology.Id,
                        Standard = technology.Standard,
                        Description = technology.Description,
                        Generation = new()
                        {
                            Id = technology.Generation.Id,
                            Name = technology.Generation.Name,
                            Description = technology.Generation.Description
                        }
                    }
                },
                Succeeded = true,
                Message = "Technology Band reactivated successfully."
            };
        }

        if (existing is not null && !existing.IsDeleted)
        {
            return new ApiResponse<TechnologyBandDto>
            {
                Succeeded = false,
                Message = "Technology Band combination already exists."
            };
        }

        var newTechnologyBand = new Domain.Aggregates.TechnologyBand
        {
            BandId = request.TechnologyBand.BandId,
            TechnologyId = request.TechnologyBand.TechnologyId
        };

        _repository.Add(newTechnologyBand);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<TechnologyBandDto>
        {
            Data = new TechnologyBandDto
            {
                Id = newTechnologyBand.Id,
                Band = new BandDto
                {
                    Id = band.Id,
                    Name = band.Name,
                    Description = band.Description
                },
                Technology = new TechnologyDto
                {
                    Id = technology.Id,
                    Standard = technology.Standard,
                    Description = technology.Description,
                    Generation = new()
                    {
                        Id = technology.Generation.Id,
                        Name = technology.Generation.Name,
                        Description = technology.Generation.Description
                    }
                }
            },
            Succeeded = true,
            Message = "Technology Band created successfully."
        };
    }
}