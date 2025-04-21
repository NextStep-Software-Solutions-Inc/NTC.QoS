using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Features.File.Requests;

public sealed class DownloadTemplateFileRequest : IRequest<ApiResponse<Stream>>
{
    public DownloadTemplateFileRequest(string type)
    {
        Type = type;
    }
    public string Type { get; set; }
}