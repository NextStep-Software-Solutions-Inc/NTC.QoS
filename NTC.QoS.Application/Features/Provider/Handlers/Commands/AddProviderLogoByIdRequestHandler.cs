using MediatR;
using Microsoft.Extensions.Logging;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class AddProviderLogoByIdHandler(
    IRepository<Domain.Entities.Provider> repository,
    ILogger<AddProviderLogoByIdHandler> logger)
    : IRequestHandler<AddProviderLogoByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;
    private readonly ILogger<AddProviderLogoByIdHandler> _logger = logger;

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
            // Convert IFormFile to byte array
            using var memoryStream = new MemoryStream();
            await request.Logo.LogoFile.CopyToAsync(memoryStream, cancellationToken);
            var logoData = memoryStream.ToArray();

            // Validate file size after conversion (additional safety check)
            if (logoData.Length == 0)
            {
                return new ApiResponse<ProviderDto>
                {
                    Succeeded = false,
                    Errors = new Dictionary<string, string[]>
                    {
                        { "EmptyFile", new[] { "Logo file is empty." } }
                    },
                    Message = "Logo file cannot be empty."
                };
            }

            _logger.LogInformation("Successfully processed logo file for provider {ProviderId}. File size: {FileSize} bytes", 
                provider.Id, logoData.Length);

            // Update provider with new logo data and metadata
            provider.LogoData = logoData;
            provider.LogoContentType = request.Logo.LogoFile.ContentType;
            provider.LogoFileName = request.Logo.LogoFile.FileName;
            provider.LogoUploadedAt = DateTime.UtcNow;

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
                    HasLogo = provider.LogoData != null && provider.LogoData.Length > 0,
                    LogoContentType = provider.LogoContentType,
                    LogoFileName = provider.LogoFileName,
                    LogoSizeBytes = provider.LogoData?.Length,
                    LogoUploadedAt = provider.LogoUploadedAt
                },
                Message = "Provider logo uploaded successfully."
            };
        }
        catch (OutOfMemoryException ex)
        {
            _logger.LogError(ex, "Out of memory when processing logo file for provider {ProviderId}", provider.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "OutOfMemory", new[] { "Logo file is too large to process." } }
                },
                Message = "Logo file is too large."
            };
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "IO error when processing logo file for provider {ProviderId}", provider.Id);
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "FileReadError", new[] { $"Failed to read logo file." } }
                },
                Message = "An error occurred while reading the logo file."
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
}