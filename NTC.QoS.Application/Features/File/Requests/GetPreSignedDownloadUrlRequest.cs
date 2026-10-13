using System;
using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public class GetPreSignedDownloadUrlRequest(Guid fileId) : IRequest<ApiResponse<PreSignedUrlDto>>
{
    public Guid FileId { get; set; } = fileId;
    public bool IsErrorLog { get; set; } = false;
}
