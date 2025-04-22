using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public sealed class ProcessNAPFileRequest(FileDto file) : IRequest<bool>
{
    public FileDto File { get; set; } = file;
}