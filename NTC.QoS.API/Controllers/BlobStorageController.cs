using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NTC.QoS.Application.Features.BlobStorage.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.BlobStorage;
using NTC.QoS.Infrastructure.Configuration;
using System.ComponentModel.DataAnnotations;
using Azure.Storage.Blobs;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Controller for Azure Blob Storage operations
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class BlobStorageController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BlobStorageController> _logger;
    private readonly IBlobStorageService _blobStorageService;
    private readonly BlobStorageOptions _blobStorageOptions;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageController(
        IMediator mediator, 
        ILogger<BlobStorageController> logger,
        IBlobStorageService blobStorageService,
        IOptions<BlobStorageOptions> blobStorageOptions,
        BlobServiceClient blobServiceClient)
    {
        _mediator = mediator;
        _logger = logger;
        _blobStorageService = blobStorageService;
        _blobStorageOptions = blobStorageOptions.Value;
        _blobServiceClient = blobServiceClient;
    }

    /// <summary>
    /// Test blob storage connection and configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connection test result</returns>
    [HttpGet("test-connection")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 500)]
    public async Task<ActionResult<ApiResponse<object>>> TestConnection(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing blob storage connection");

            // Enhanced configuration debugging
            var configurationDebugging = new
            {
                ConfigurationValues = new
                {
                    ConnectionStringFromOptions = _blobStorageOptions.ConnectionString ?? "NULL",
                    ConnectionStringLength = _blobStorageOptions.ConnectionString?.Length ?? 0,
                    ConnectionStringMasked = !string.IsNullOrEmpty(_blobStorageOptions.ConnectionString) 
                        ? $"{_blobStorageOptions.ConnectionString[..Math.Min(20, _blobStorageOptions.ConnectionString.Length)]}..."
                        : "NULL",
                    UseDevelopmentStorage = _blobStorageOptions.UseDevelopmentStorage,
                    DefaultContainer = _blobStorageOptions.DefaultContainer,
                    MaxFileSizeMB = _blobStorageOptions.MaxFileSizeMB,
                    AccessLevel = _blobStorageOptions.AccessLevel,
                    AllowedContentTypesCount = _blobStorageOptions.AllowedContentTypes?.Length ?? 0
                },
                ConfigurationStatus = !string.IsNullOrEmpty(_blobStorageOptions.ConnectionString) || _blobStorageOptions.UseDevelopmentStorage ? "OK" : "Missing Configuration"
            };

            var testResult = new
            {
                ConfigurationStatus = configurationDebugging.ConfigurationStatus,
                ConnectionString = !string.IsNullOrEmpty(_blobStorageOptions.ConnectionString) ? "Configured" : "Missing",
                ConnectionStringSource = string.IsNullOrEmpty(_blobStorageOptions.ConnectionString) ? "Not Found" : "User Secrets / Configuration",
                UseDevelopmentStorage = _blobStorageOptions.UseDevelopmentStorage,
                DefaultContainer = _blobStorageOptions.DefaultContainer,
                MaxFileSizeMB = _blobStorageOptions.MaxFileSizeMB,
                AllowedContentTypes = _blobStorageOptions.AllowedContentTypes,
                AccessLevel = _blobStorageOptions.AccessLevel,
                StorageAccountProperties = (object?)null,
                ConnectionTest = "Failed",
                ConfigurationDebugging = configurationDebugging
            };

            // Test actual connection to Azure Blob Storage
            try
            {
                var accountInfo = await _blobServiceClient.GetAccountInfoAsync(cancellationToken);
                var serviceProperties = await _blobServiceClient.GetPropertiesAsync(cancellationToken);

                testResult = testResult with
                {
                    StorageAccountProperties = new
                    {
                        AccountKind = accountInfo.Value.AccountKind.ToString(),
                        SkuName = accountInfo.Value.SkuName.ToString(),
                        DefaultServiceVersion = serviceProperties.Value.DefaultServiceVersion,
                        DeleteRetentionPolicy = serviceProperties.Value.DeleteRetentionPolicy?.Enabled ?? false
                    },
                    ConnectionTest = "Success"
                };

                _logger.LogInformation("Blob storage connection test successful");
            }
            catch (Exception connectionEx)
            {
                _logger.LogError(connectionEx, "Blob storage connection test failed");
                
                testResult = testResult with
                {
                    ConnectionTest = $"Failed: {connectionEx.Message}"
                };
            }

            return Ok(new ApiResponse<object>
            {
                Data = testResult,
                Message = "Blob storage connection test completed",
                Succeeded = testResult.ConnectionTest == "Success"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during blob storage connection test");
            return StatusCode(500, new ApiResponse<object>
            {
                Message = $"Error during connection test: {ex.Message}",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Upload a file to Azure Blob Storage
    /// </summary>
    /// <param name="request">The upload request containing container name, file, and optional file name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload response with blob URL</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<BlobUploadResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<BlobUploadResponseDto>), 400)]
    public async Task<ActionResult<ApiResponse<BlobUploadResponseDto>>> UploadBlob(
        [FromForm] BlobUploadFormDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.File.Length == 0)
            {
                return BadRequest(new ApiResponse<BlobUploadResponseDto>
                {
                    Message = "File is empty",
                    Succeeded = false
                });
            }

            var blobFileName = request.FileName ?? request.File.FileName;
            
            // Add timestamp to prevent conflicts
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(blobFileName);
            var fileExtension = Path.GetExtension(blobFileName);
            blobFileName = $"{fileNameWithoutExtension}_{timestamp}{fileExtension}";

            using var stream = request.File.OpenReadStream();
            
            var uploadRequest = new UploadBlobRequest
            {
                ContainerName = request.ContainerName.ToLowerInvariant(),
                FileName = blobFileName,
                FileStream = stream,
                ContentType = request.File.ContentType
            };

            var result = await _mediator.Send(uploadRequest, cancellationToken);
            
            if (result.Succeeded)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading blob to container {ContainerName}", request.ContainerName);
            return StatusCode(500, new ApiResponse<BlobUploadResponseDto>
            {
                Message = "Internal server error occurred while uploading blob",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Download a file from Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="fileName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream</returns>
    [HttpGet("download/{containerName}/{fileName}")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<BlobDownloadDto>), 404)]
    public async Task<IActionResult> DownloadBlob(
        [FromRoute, Required] string containerName,
        [FromRoute, Required] string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DownloadBlobRequest
            {
                ContainerName = containerName.ToLowerInvariant(),
                FileName = fileName
            };

            var result = await _mediator.Send(request, cancellationToken);
            
            if (result.Succeeded && result.Data != null)
            {
                return File(result.Data.FileStream, result.Data.ContentType, result.Data.FileName);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob {FileName} from container {ContainerName}", fileName, containerName);
            return StatusCode(500, new ApiResponse<BlobDownloadDto>
            {
                Message = "Internal server error occurred while downloading blob",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Download a file as byte array from Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="fileName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Byte array response</returns>
    [HttpGet("download-bytes/{containerName}/{fileName}")]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 200)]
    [ProducesResponseType(typeof(ApiResponse<byte[]>), 404)]
    public async Task<ActionResult<ApiResponse<byte[]>>> DownloadBlobAsBytes(
        [FromRoute, Required] string containerName,
        [FromRoute, Required] string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DownloadBlobAsBytesRequest
            {
                ContainerName = containerName.ToLowerInvariant(),
                FileName = fileName
            };

            var result = await _mediator.Send(request, cancellationToken);
            
            if (result.Succeeded)
            {
                return Ok(result);
            }

            return NotFound(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading blob {FileName} as bytes from container {ContainerName}", fileName, containerName);
            return StatusCode(500, new ApiResponse<byte[]>
            {
                Message = "Internal server error occurred while downloading blob",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Delete a file from Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="fileName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{containerName}/{fileName}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBlob(
        [FromRoute, Required] string containerName,
        [FromRoute, Required] string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new DeleteBlobRequest
            {
                ContainerName = containerName.ToLowerInvariant(),
                FileName = fileName
            };

            var result = await _mediator.Send(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blob {FileName} from container {ContainerName}", fileName, containerName);
            return StatusCode(500, new ApiResponse<bool>
            {
                Message = "Internal server error occurred while deleting blob",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Check if a blob exists in Azure Blob Storage
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="fileName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Existence result</returns>
    [HttpHead("{containerName}/{fileName}")]
    [HttpGet("exists/{containerName}/{fileName}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    public async Task<ActionResult<ApiResponse<bool>>> BlobExists(
        [FromRoute, Required] string containerName,
        [FromRoute, Required] string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new BlobExistsRequest
            {
                ContainerName = containerName.ToLowerInvariant(),
                FileName = fileName
            };

            var result = await _mediator.Send(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of blob {FileName} in container {ContainerName}", fileName, containerName);
            return StatusCode(500, new ApiResponse<bool>
            {
                Message = "Internal server error occurred while checking blob existence",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// List blobs in a container
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="prefix">Optional prefix to filter blobs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of blob information</returns>
    [HttpGet("list/{containerName}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BlobInfoDto>>), 200)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BlobInfoDto>>>> ListBlobs(
        [FromRoute, Required] string containerName,
        [FromQuery] string? prefix = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ListBlobsRequest
            {
                ContainerName = containerName.ToLowerInvariant(),
                Prefix = prefix
            };

            var result = await _mediator.Send(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing blobs in container {ContainerName}", containerName);
            return StatusCode(500, new ApiResponse<IEnumerable<BlobInfoDto>>
            {
                Message = "Internal server error occurred while listing blobs",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Get blob URL
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="fileName">The blob name</param>
    /// <returns>Blob URL</returns>
    [HttpGet("url/{containerName}/{fileName}")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public ActionResult<ApiResponse<string>> GetBlobUrl(
        [FromRoute, Required] string containerName,
        [FromRoute, Required] string fileName)
    {
        try
        {
            // This is a simple URL construction without checking existence
            // In a real scenario, you might want to verify the blob exists first
            var url = $"https://[storageaccount].blob.core.windows.net/{containerName.ToLowerInvariant()}/{fileName}";
            
            return Ok(new ApiResponse<string>
            {
                Data = url,
                Message = "Blob URL generated successfully",
                Succeeded = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating URL for blob {FileName} in container {ContainerName}", fileName, containerName);
            return StatusCode(500, new ApiResponse<string>
            {
                Message = "Internal server error occurred while generating blob URL",
                Succeeded = false
            });
        }
    }

    /// <summary>
    /// Debug configuration values (Development only)
    /// </summary>
    /// <returns>Configuration debug information</returns>
    [HttpGet("debug-config")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public ActionResult<ApiResponse<object>> DebugConfiguration()
    {
        // Only allow in development
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var configuration = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            
            var debugInfo = new
            {
                Environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
                UserSecretsConfiguration = new
                {
                    AzureBlobStorageSection = configuration.GetSection("AzureBlobStorage").Exists(),
                    ConnectionStringExists = !string.IsNullOrEmpty(configuration["AzureBlobStorage:ConnectionString"]),
                    ConnectionStringValue = !string.IsNullOrEmpty(configuration["AzureBlobStorage:ConnectionString"]) 
                        ? $"{configuration["AzureBlobStorage:ConnectionString"]?[..20]}..." : "NULL",
                    UseDevelopmentStorage = configuration.GetValue<bool>("AzureBlobStorage:UseDevelopmentStorage"),
                    DefaultContainer = configuration["AzureBlobStorage:DefaultContainer"],
                    MaxFileSizeMB = configuration.GetValue<int>("AzureBlobStorage:MaxFileSizeMB"),
                    AccessLevel = configuration["AzureBlobStorage:AccessLevel"]
                },
                BlobStorageOptions = new
                {
                    ConnectionString = !string.IsNullOrEmpty(_blobStorageOptions.ConnectionString) 
                        ? $"{_blobStorageOptions.ConnectionString[..20]}..." : "NULL",
                    UseDevelopmentStorage = _blobStorageOptions.UseDevelopmentStorage,
                    DefaultContainer = _blobStorageOptions.DefaultContainer,
                    MaxFileSizeMB = _blobStorageOptions.MaxFileSizeMB,
                    AccessLevel = _blobStorageOptions.AccessLevel,
                    AllowedContentTypesCount = _blobStorageOptions.AllowedContentTypes?.Length ?? 0
                },
                ConfigurationProviders = configuration.AsEnumerable()
                    .Where(kvp => kvp.Key.StartsWith("AzureBlobStorage:", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Key.Contains("ConnectionString") 
                        ? (string.IsNullOrEmpty(kvp.Value) ? "NULL" : $"{kvp.Value[..Math.Min(20, kvp.Value.Length)]}...")
                        : kvp.Value)
            };

            return Ok(new ApiResponse<object>
            {
                Data = debugInfo,
                Message = "Configuration debugging information",
                Succeeded = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration debug information");
            return StatusCode(500, new ApiResponse<object>
            {
                Message = $"Error retrieving debug info: {ex.Message}",
                Succeeded = false
            });
        }
    }
}