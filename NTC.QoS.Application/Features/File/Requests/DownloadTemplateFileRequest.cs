using MediatR;

namespace NTC.QoS.Application.Features.File.Requests;

public sealed class DownloadTemplateFileRequest : IRequest<Stream>
{
    public DownloadTemplateFileRequest(string type)
    {
        Type = type;
    }
    public string Type { get; set; }
}