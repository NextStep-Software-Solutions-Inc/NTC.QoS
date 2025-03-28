using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public class UploadFileRequest(UploadFileDto file) : IRequest<ApiResponse<FileDto>>
{
    public UploadFileDto File { get; set; } = file;
}