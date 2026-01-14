using MediatR;
using NTC.QoS.Application.Features.Band.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Band;

namespace NTC.QoS.Application.Features.Band.Handlers.Commands;

public class CreateBandHandler(IRepository<Domain.Entities.Band> repository) : IRequestHandler<CreateBandRequest, ApiResponse<BandDto>>
{
    private readonly IRepository<Domain.Entities.Band> _repository = repository;

    public async Task<ApiResponse<BandDto>> Handle(CreateBandRequest request, CancellationToken cancellationToken)
    {
        var bandQueryAll = _repository.QueryWithIgnoreQueryFilters(b => b.Name == request.Band.Name);
        var existing = await _repository.FirstOrDefaultAsync(bandQueryAll, cancellationToken);

        if (existing != null && existing.IsDeleted)
        {
            var restoreQuery = _repository.QueryWithIgnoreQueryFilters(b => b.Id == existing.Id && b.IsDeleted);
            await _repository.RestoreAsync(restoreQuery, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return new ApiResponse<BandDto>
            {
                Data = new BandDto
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    Description = existing.Description,
                },
                Succeeded = true,
                Message = "Band restored successfully."
            };
        }

        if (existing != null)
        {
            return new ApiResponse<BandDto>
            {
                Succeeded = false,
                Message = "Band already exists.",
                Errors = new Dictionary<string, string[]>
                {
                    {"Duplicate", ["Band with the same name already exists."]}
                }
            };
        }

        var newBand = new Domain.Entities.Band
        {
            Name = request.Band.Name,
            Description = request.Band.Description,
        };

        _repository.Add(newBand);
        await _repository.SaveChangesAsync(cancellationToken);

        return new ApiResponse<BandDto>
        {
            Data = new BandDto
            {
                Id = newBand.Id,
                Name = newBand.Name,
                Description = newBand.Description,
            },
            Succeeded = true,
            Message = "Band created successfully.",
        };
    }
}