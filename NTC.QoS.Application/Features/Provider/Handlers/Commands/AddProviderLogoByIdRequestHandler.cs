using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class AddProviderLogoByIdHandler(
    IRepository<Domain.Entities.Provider> repository,
    ILogger<AddProviderLogoByIdHandler> logger,
    IConfiguration configuration)
    : IRequestHandler<AddProviderLogoByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    private readonly ILogger<AddProviderLogoByIdHandler> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task<ApiResponse<ProviderDto>> Handle(AddProviderLogoByIdRequest request, CancellationToken cancellationToken)
    {
        // Find the provider
        var providerQuery = _repository.Query(p => p.Id == request.Id);
        var provider = await _repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is null)
        {
            _logger.LogWarning("Provider with ID {ProviderId} not found", request.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "ProviderNotFound", new[] { "Provider not found." } }
                },
                Message = "The provider you are trying to update does not exist."
            };
        }

        try
        {
            // Generate unique filename with fallback
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var sanitizedProviderName = SanitizeProviderName(provider.Name, provider.Id);
            var fileExtension = Path.GetExtension(request.Logo.LogoFile.FileName).ToLowerInvariant();
            var fileName = $"provider_logo_{sanitizedProviderName}_{timestamp}{fileExtension}";

            // Get upload directory from configuration or use default
            var uploadsBasePath = _configuration["FileUpload:BasePath"] ?? "Uploads";
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), uploadsBasePath, "Provider", "Logos");
            
            // Store relative path for database (not absolute path)
            var relativePath = Path.Combine("Uploads", "Provider", "Logos", fileName);
            var fullFilePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            // Ensure the directory exists
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
                _logger.LogInformation("Created upload directory: {Directory}", uploadDirectory);
            }

            // Delete old logo file if it exists
            if (!string.IsNullOrEmpty(provider.LogoPath))
            {
                await DeleteOldLogoFileAsync(provider.LogoPath);
            }

            // Save the new logo file
            using (var fileStream = new FileStream(fullFilePath, FileMode.Create))
            {
                await request.Logo.LogoFile.CopyToAsync(fileStream, cancellationToken);
            }

            _logger.LogInformation("Successfully saved logo file for provider {ProviderId} to {FilePath}", 
                provider.Id, relativePath);

            // Update provider with new logo path (store relative path)
            provider.LogoPath = relativePath;
            _repository.Update(provider);
            await _repository.SaveChangesAsync(cancellationToken);

            return new ApiResponse<ProviderDto>
            {
                Succeeded = true,
                Data = new ProviderDto
                {
                    Id = provider.Id,
                    Name = provider.Name,
                    Description = provider.Description,
                    LicenseNumber = provider.LicenseNumber,
                    Color = provider.Color,
                    LogoPath = provider.LogoPath
                },
                Message = "Provider logo uploaded successfully."
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Unauthorized access when saving logo file for provider {ProviderId}", provider.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "UnauthorizedAccess", new[] { "Access denied when saving the logo file. Please check file permissions." } }
                },
                Message = "Access denied when saving the logo file."
            };
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex, "Directory not found when saving logo file for provider {ProviderId}", provider.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "DirectoryNotFound", new[] { "Upload directory could not be created or accessed." } }
                },
                Message = "Upload directory is not accessible."
            };
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error when saving logo file for provider {ProviderId}", provider.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "FileUploadError", new[] { $"Failed to save logo file: {ex.Message}" } }
                },
                Message = "An error occurred while uploading the logo."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when uploading logo for provider {ProviderId}", provider.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "UnexpectedError", new[] { "An unexpected error occurred while uploading the logo." } }
                },
                Message = "An unexpected error occurred while uploading the logo."
            };
        }
    }

    /// <summary>
    /// Sanitizes the provider name for use in filenames with fallback to provider ID
    /// </summary>
    /// <param name="providerName">The provider name to sanitize</param>
    /// <param name="providerId">The provider ID to use as fallback</param>
    /// <returns>A sanitized string safe for use in filenames</returns>
    private static string SanitizeProviderName(string providerName, Guid providerId)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return $"provider_{providerId:N}";
        }

        // Remove invalid filename characters and replace spaces with underscores
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("", providerName.Split(invalidChars)).Replace(" ", "_");

        // If sanitization resulted in empty string, use provider ID as fallback
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return $"provider_{providerId:N}";
        }

        // Limit length and ensure it's not empty
        var maxLength = 50;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength);
        }

        return sanitized;
    }

    /// <summary>
    /// Safely deletes the old logo file with proper validation and logging
    /// </summary>
    /// <param name="logoPath">The path to the logo file to delete</param>
    private async Task DeleteOldLogoFileAsync(string logoPath)
    {
        try
        {
            // Validate that the path is within our expected upload directory to prevent path traversal
            var fullPath = Path.IsPathRooted(logoPath) 
                ? logoPath 
                : Path.Combine(Directory.GetCurrentDirectory(), logoPath);

            var normalizedPath = Path.GetFullPath(fullPath);
            var expectedBasePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Provider", "Logos"));

            if (!normalizedPath.StartsWith(expectedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempted to delete file outside of allowed directory: {FilePath}", logoPath);
                return;
            }

            if (System.IO.File.Exists(normalizedPath))
            {
                await Task.Run(() => System.IO.File.Delete(normalizedPath));
                _logger.LogInformation("Successfully deleted old logo file: {FilePath}", logoPath);
            }
            else
            {
                _logger.LogWarning("Old logo file not found for deletion: {FilePath}", logoPath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied when trying to delete old logo file: {FilePath}", logoPath);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "IO error when trying to delete old logo file: {FilePath}", logoPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error when trying to delete old logo file: {FilePath}", logoPath);
        }
    }
}