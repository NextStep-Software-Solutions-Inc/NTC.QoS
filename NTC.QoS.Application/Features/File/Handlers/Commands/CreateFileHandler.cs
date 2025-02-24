using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class CreateFileHandler(IRepository<Domain.Entities.File> repository) : IRequestHandler<CreateFileRequest, ApiResponse<Domain.Entities.File>>
{
    private readonly IRepository<Domain.Entities.File> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.File>> Handle(CreateFileRequest request, CancellationToken cancellationToken)
    {
        var file = new Domain.Entities.File
        {
            Name = request.Name,
            Path = $"files/{request.Name}",
            Type = EnumHelper<FileTypeEnum>.GetName( request.Type)

        };
        await _repository.Add(file);
        return new ApiResponse<Domain.Entities.File>(file);
    }
}