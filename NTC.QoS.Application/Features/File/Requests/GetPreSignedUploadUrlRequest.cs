using System;
using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Requests;

public class GetPreSignedUploadUrlRequest(string fileName, string fileType, Guid regionId) : IRequest<ApiResponse<PreSignedUrlDto>>
{
    public string FileName { get; set; } = fileName;
    public string FileType { get; set; } = fileType;
    public Guid RegionId { get; set; } = regionId;
}
