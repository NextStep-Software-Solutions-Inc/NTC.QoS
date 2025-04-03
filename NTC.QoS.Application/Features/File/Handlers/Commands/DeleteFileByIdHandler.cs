using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File.Handlers.Commands;

public class DeleteFileByIdHandler(IFileRepository repository) : IRequestHandler<DeleteFileByIdRequest, ApiResponse<bool>>
{
    private readonly IFileRepository _repository = repository;
    public async Task<ApiResponse<bool>> Handle(DeleteFileByIdRequest request, CancellationToken cancellationToken)
    {
        var file = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (file is null)
        {
            return new ApiResponse<bool>
            {
                Message = "The file to delete does not exist.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullFile", ["File not found."]
                    }
                }
            };
        }
        _repository.Delete(file);
        await _repository.SaveChangesAsync();

        return new ApiResponse<bool>
        {
            Message = "File deleted successfully.",
            Succeeded = true
        };
    }
}
