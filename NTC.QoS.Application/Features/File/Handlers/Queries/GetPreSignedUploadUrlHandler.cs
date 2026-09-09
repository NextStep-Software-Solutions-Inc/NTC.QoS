using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.File.Requests;
using NTC.QoS.Application.Helpers;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Queries;

public class GetPreSignedUploadUrlHandler(
    IBlobStorageService blobStorageService,
    IUserAuthorizationService userAuthorizationService,
    IRegionRepository regionRepository,
    IConfiguration configuration,
    ILogger<GetPreSignedUploadUrlHandler> logger) : IRequestHandler<GetPreSignedUploadUrlRequest, ApiResponse<PreSignedUrlDto>>
{
    private readonly IBlobStorageService _blobStorageService = blobStorageService;
    private readonly IUserAuthorizationService _userAuthorizationService = userAuthorizationService;
    private readonly IRegionRepository _regionRepository = regionRepository;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<GetPreSignedUploadUrlHandler> _logger = logger;

    public async Task<ApiResponse<PreSignedUrlDto>> Handle(GetPreSignedUploadUrlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            FileTypeEnum fileType = Enum.TryParse<FileTypeEnum>(request.FileType, out var parsedType) ? parsedType : FileTypeEnum.Unknown;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string originalFileName = request.FileName;
            string fileExtension = Path.GetExtension(originalFileName);
            string fileGuid = Guid.NewGuid().ToString("N");
            string newFileName = $"{timestamp}_{fileGuid}{fileExtension}";

            var regionCode = await GetRegionCodeAsync(request.RegionId, cancellationToken);
            if (string.IsNullOrEmpty(regionCode))
            {
                return new ApiResponse<PreSignedUrlDto>
                {
                    Message = "Invalid region or user does not have access to the specified region.",
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "Region", ["Invalid region or insufficient region access."]}
                    }
                };
            }

            string containerName = BlobStoragePathHelper.GetContainerName(_configuration["ASPNETCORE_ENVIRONMENT"]);
            string blobPath = BlobStoragePathHelper.GetBlobPathWithRegion(fileType, regionCode, newFileName);

            TimeSpan expiry = TimeSpan.FromMinutes(1);
            string sasUrl = _blobStorageService.GeneratePreSignedUploadUrl(containerName, blobPath, expiry);

            return new ApiResponse<PreSignedUrlDto>
            {
                Data = new PreSignedUrlDto
                {
                    Url = sasUrl,
                    BlobPath = blobPath,
                    ExpiresAtUtc = DateTime.UtcNow.Add(expiry)
                },
                Message = "Successfully generated 1-minute pre-signed upload URL.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed upload URL for file {FileName}", request.FileName);
            return new ApiResponse<PreSignedUrlDto>
            {
                Message = $"Failed to generate pre-signed upload URL: {ex.Message}",
                Succeeded = false
            };
        }
    }

    private async Task<string?> GetRegionCodeAsync(Guid regionId, CancellationToken cancellationToken)
    {
        var hasAccess = await _userAuthorizationService.HasRegionAccessAsync(regionId, cancellationToken);
        if (!hasAccess)
        {
            _logger.LogWarning("User does not have access to region: {RegionId}", regionId);
            return null;
        }

        var regionQuery = _regionRepository.Query(r => r.Id == regionId).Select(r => new { r.Id, r.Code });
        var region = await _regionRepository.FirstOrDefaultAsync(regionQuery, cancellationToken);

        if (region is null)
        {
            _logger.LogWarning("Region not found: {RegionId}", regionId);
            return null;
        }

        return BlobStoragePathHelper.SanitizeRegionCode(region.Code);
    }
}
