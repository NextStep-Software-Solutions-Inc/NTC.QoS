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
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Application.Features.File.Handlers.Queries;

public class GetPreSignedDownloadUrlHandler(
    IFileRepository fileRepository,
    IBlobStorageService blobStorageService,
    IConfiguration configuration,
    ILogger<GetPreSignedDownloadUrlHandler> logger) : IRequestHandler<GetPreSignedDownloadUrlRequest, ApiResponse<PreSignedUrlDto>>
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IBlobStorageService _blobStorageService = blobStorageService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<GetPreSignedDownloadUrlHandler> _logger = logger;

    public async Task<ApiResponse<PreSignedUrlDto>> Handle(GetPreSignedDownloadUrlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var fileQuery = _fileRepository.Query(f => !f.IsDeleted && f.Id.Equals(request.FileId));
            var file = await _fileRepository.FirstOrDefaultAsync(fileQuery, cancellationToken);
            if (file == null)
            {
                return new ApiResponse<PreSignedUrlDto>
                {
                    Message = $"File with ID '{request.FileId}' not found.",
                    Succeeded = false
                };
            }

            var targetPath = request.IsErrorLog ? file.ErrorLogPath : file.Path;
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return new ApiResponse<PreSignedUrlDto>
                {
                    Message = request.IsErrorLog
                        ? $"No error report exists for file '{file.Name}'."
                        : $"File path is empty for file with ID '{request.FileId}'.",
                    Succeeded = false
                };
            }

            string containerName = GetContainerName();
            string blobPath = GetBlobNameFromPath(targetPath, containerName);

            TimeSpan expiry = TimeSpan.FromMinutes(request.IsErrorLog ? 15 : 1);
            string sasUrl = _blobStorageService.GeneratePreSignedDownloadUrl(containerName, blobPath, expiry);

            return new ApiResponse<PreSignedUrlDto>
            {
                Data = new PreSignedUrlDto
                {
                    Url = sasUrl,
                    BlobPath = blobPath,
                    ExpiresAtUtc = DateTime.UtcNow.Add(expiry)
                },
                Message = request.IsErrorLog
                    ? "Successfully generated error report download URL."
                    : "Successfully generated 1-minute pre-signed download URL.",
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate pre-signed download URL for file ID {FileId}", request.FileId);
            return new ApiResponse<PreSignedUrlDto>
            {
                Message = $"Failed to generate pre-signed download URL: {ex.Message}",
                Succeeded = false
            };
        }
    }

    private string GetContainerName()
    {
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
        return environment?.ToLowerInvariant() switch
        {
            "production" => "production",
            "staging" => "production",
            _ => "development"
        };
    }

    private static string GetBlobNameFromPath(string path, string containerName)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            var segments = uri.LocalPath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var containerIdx = Array.FindIndex(segments, s => string.Equals(s, containerName, StringComparison.OrdinalIgnoreCase));
            if (containerIdx >= 0 && containerIdx + 1 < segments.Length)
            {
                return string.Join("/", segments.Skip(containerIdx + 1));
            }

            if (segments.Length > 1)
            {
                return string.Join("/", segments.Skip(1));
            }

            return uri.LocalPath.TrimStart('/');
        }

        return path;
    }
}
