using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class DeleteFileByIdHandler(IRepository<Domain.Entities.File> repository) : IRequestHandler<DeleteFileByIdRequest, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.File> _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteFileByIdRequest request, CancellationToken cancellationToken)
    {
        var fileExist = await _repository.FindAsync(p => p.Id == request.Id && !p.IsDeleted, cancellationToken);
        if (fileExist == null)
        {
            return new ApiResponse<bool>
            {
                Message = "File not found",
                Succeeded = false
            };
        }
        _repository.Delete(fileExist);
        await _repository.SaveChangesAsync();
        return new ApiResponse<bool>
        {
            Data = true,
            Message = "File deleted",
            Succeeded = true
        };
    }
}
