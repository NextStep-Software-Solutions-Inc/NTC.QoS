using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public sealed class ProcessNAPFileRequest(UploadFileDto file) : IRequest<bool>
{
    public UploadFileDto File { get; set; } = file;
}