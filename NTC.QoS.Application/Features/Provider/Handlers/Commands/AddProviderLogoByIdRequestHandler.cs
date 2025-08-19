using MediatR;
using NTC.QoS.Application.Features.Provider.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Provider;
using System.IO;

namespace NTC.QoS.Application.Features.Provider.Handlers.Commands;

public class AddProviderLogoByIdHandler(IRepository<Domain.Entities.Provider> repository)
    : IRequestHandler<AddProviderLogoByIdRequest, ApiResponse<ProviderDto>>
{
    private readonly IRepository<Domain.Entities.Provider> _repository = repository;

    public async Task<ApiResponse<ProviderDto>> Handle(AddProviderLogoByIdRequest request, CancellationToken cancellationToken)
    {
        // Find the provider
        var providerQuery = _repository.Query(p => p.Id == request.Id);
        var provider = await _repository.FirstOrDefaultAsync(providerQuery, cancellationToken);

        if (provider is null)
        {
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
            // Generate unique filename
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var sanitizedProviderName = string.Join("", provider.Name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "-");
            var fileExtension = Path.GetExtension(request.Logo.LogoFile.FileName).ToLowerInvariant();
            var newFileName = $"provider_logo_{sanitizedProviderName}_{timestamp}{fileExtension}";

            // Define the save path
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Provider", "Logos");
            var filePath = Path.Combine(uploadDirectory, newFileName);

            // Ensure the directory exists
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            // Delete old logo file if it exists
            if (!string.IsNullOrEmpty(provider.LogoPath) && System.IO.File.Exists(provider.LogoPath))
            {
                try
                {
                    System.IO.File.Delete(provider.LogoPath);
                }
                catch (Exception)
                {
                    // Log the error but don't fail the operation
                    // Consider using a proper logging framework here
                }
            }

            // Save the new logo file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.Logo.LogoFile.CopyToAsync(fileStream, cancellationToken);
            }

            // Update provider with new logo path
            provider.LogoPath = filePath;
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
        catch (IOException ex)
        {
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
            return new ApiResponse<ProviderDto>
            {
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    { "UnexpectedError", new[] { $"An unexpected error occurred: {ex.Message}" } }
                },
                Message = "An unexpected error occurred while uploading the logo."
            };
        }
    }
}
