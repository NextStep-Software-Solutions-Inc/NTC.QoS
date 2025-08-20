using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public class UpdateFileByIdRequest : IRequest<ApiResponse<FileDto>>
{
    public Guid Id { get; set; }
    public UpdateFileDto File { get; set; }
    public UpdateFileByIdRequest(Guid id, UpdateFileDto file)
    {
        Id = id;
        File = file;
    }
}
