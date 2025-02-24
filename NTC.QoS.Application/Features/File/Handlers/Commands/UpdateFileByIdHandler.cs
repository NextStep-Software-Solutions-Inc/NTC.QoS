using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class UpdateFileByIdHandler(IRepository<Domain.Entities.File> repository) : IRequestHandler<UpdateFileByIdRequest, ApiResponse<FileDto>>
{
    private readonly IRepository<Domain.Entities.File> _repository = repository;
    public async Task<ApiResponse<FileDto>> Handle(UpdateFileByIdRequest request, CancellationToken cancellationToken)
    {
        var fileExist = await _repository.FindAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);
        if (fileExist == null)
        {
            return new ApiResponse<FileDto>
            {
                Message = "File not found",
                Succeeded = false
            };
        }
        fileExist.Name = request.File.Name;
        _repository.Update(fileExist);
        await _repository.SaveChangesAsync();
        return new ApiResponse<FileDto>
        {
            Data = new()
            {
                Id = fileExist.Id,
                Name = fileExist.Name,
                Path = fileExist.Path
            },
            Message = "File updated",
            Succeeded = true
        };
    }
}
