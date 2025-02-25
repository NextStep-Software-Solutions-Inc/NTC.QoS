using MediatR;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File.Handlers.Queries;

public class GetByIdFileHandler(IRepository<Domain.Entities.File> repository) : IRequestHandler<GetFileByIdRequest, ApiResponse<Domain.Entities.File>>
{
    private readonly IRepository<Domain.Entities.File> _repository = repository;
    public async Task<ApiResponse<Domain.Entities.File>> Handle(GetFileByIdRequest request, CancellationToken cancellationToken)
    {
        var fileExist = await _repository.FindAsync(f => f.Id == request.Id && !f.IsDeleted, cancellationToken);
        if (fileExist == null)
        {
            return new ApiResponse<Domain.Entities.File>
            {
                Data = null,
                Message = "File not found",
                Succeeded = false
            };
        }
        return new ApiResponse<Domain.Entities.File>
        {
            Data = fileExist,
            Message = "File found",
            Succeeded = true
        };
    }
}