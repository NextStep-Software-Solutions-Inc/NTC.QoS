using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public class CreateFileRequest(CreateFileDto file) : IRequest<ApiResponse<FileDto>>
{
    public CreateFileDto File { get; set; } = file;
}