// Example usage of the FluentValidation for GetAreaBroadbandInfo
// This demonstrates how the validation works in practice

using FluentValidation.TestHelper;
using NTC.QoS.Application.Features.Analytics.Handlers.Validators;
using NTC.QoS.Application.Features.Analytics.Requests;
using NTC.QoS.Domain.Constants;
using NTC.QoS.Domain.Models.DTOs.Analytics;

namespace NTC.QoS.Application.Examples;

/// <summary>
/// Example class demonstrating the FluentValidation usage for GetAreaBroadbandInfo
/// This would typically be in a test project, but serves as documentation here
/// </summary>
public class GetAreaBroadbandInfoValidationExamples
{
    private readonly GetAreaBroadbandInfoRequestValidator _validator;
    private readonly BoundingBoxDtoValidator _boundingBoxValidator;

    public GetAreaBroadbandInfoValidationExamples()
    {
        _validator = new GetAreaBroadbandInfoRequestValidator();
        _boundingBoxValidator = new BoundingBoxDtoValidator();
    }

    public void ValidRequest_ShouldPass()
    {
        // Arrange - Valid Metro Manila area
        var request = new GetAreaBroadbandInfoRequest
        {
            BoundingBox = new BoundingBoxDto
            {
                MinLatitude = 14.4,
                MaxLatitude = 14.8,
                MinLongitude = 120.9,
                MaxLongitude = 121.2
            }
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        // result.ShouldNotHaveAnyValidationErrors();
    }

    public void NullBoundingBox_ShouldFail()
    {
        // Arrange
        var request = new GetAreaBroadbandInfoRequest
        {
            BoundingBox = null!
        };

        // Act & Assert
        var result = _validator.TestValidate(request);
        // result.ShouldHaveValidationErrorFor(x => x.BoundingBox)
        //       .WithErrorMessage("Bounding box is required.");
    }

    public void InvalidCoordinateRange_ShouldFail()
    {
        // Arrange - Coordinates outside supported region
        var boundingBox = new BoundingBoxDto
        {
            MinLatitude = 50.0, // Too far north
            MaxLatitude = 60.0,
            MinLongitude = 120.0,
            MaxLongitude = 121.0
        };

        // Act & Assert
        var result = _boundingBoxValidator.TestValidate(boundingBox);
        // result.ShouldHaveValidationErrorFor(x => x.MinLatitude)
        //       .WithErrorMessage($"Minimum latitude must not exceed {GeoSpatialConstants.MaxSupportedLatitude}°.");
    }

    public void InvalidBoundingBoxOrder_ShouldFail()
    {
        // Arrange - Min values greater than max values
        var boundingBox = new BoundingBoxDto
        {
            MinLatitude = 15.0,
            MaxLatitude = 14.0, // Max less than min
            MinLongitude = 122.0,
            MaxLongitude = 121.0 // Max less than min
        };

        // Act & Assert
        var result = _boundingBoxValidator.TestValidate(boundingBox);
        // result.ShouldHaveValidationErrorFor(x => x.MinLatitude)
        //       .WithErrorMessage("Minimum latitude must be less than maximum latitude.");
        // result.ShouldHaveValidationErrorFor(x => x.MinLongitude)
        //       .WithErrorMessage("Minimum longitude must be less than maximum longitude.");
    }

    public void TooLargeArea_ShouldFail()
    {
        // Arrange - Area exceeding size limits
        var boundingBox = new BoundingBoxDto
        {
            MinLatitude = 10.0,
            MaxLatitude = 25.0, // Difference of 15° > MaxLatitudeDifference (10°)
            MinLongitude = 110.0,
            MaxLongitude = 135.0 // Difference of 25° > MaxLongitudeDifference (15°)
        };

        // Act & Assert
        var result = _boundingBoxValidator.TestValidate(boundingBox);
        // result.ShouldHaveValidationErrorFor(x => x)
        //       .WithErrorMessage(GeoSpatialConstants.GetAreaSizeErrorMessage(15.0, 25.0));
    }

    public void ValidPhilippinesCities_ShouldPass()
    {
        var validBoundingBoxes = new[]
        {
            // Metro Manila
            new BoundingBoxDto { MinLatitude = 14.4, MaxLatitude = 14.8, MinLongitude = 120.9, MaxLongitude = 121.2 },
            // Cebu City
            new BoundingBoxDto { MinLatitude = 10.2, MaxLatitude = 10.4, MinLongitude = 123.8, MaxLongitude = 124.0 },
            // Davao City
            new BoundingBoxDto { MinLatitude = 7.0, MaxLatitude = 7.2, MinLongitude = 125.5, MaxLongitude = 125.7 },
            // Baguio City
            new BoundingBoxDto { MinLatitude = 16.3, MaxLatitude = 16.5, MinLongitude = 120.5, MaxLongitude = 120.7 }
        };

        foreach (var boundingBox in validBoundingBoxes)
        {
            var result = _boundingBoxValidator.TestValidate(boundingBox);
            // result.ShouldNotHaveAnyValidationErrors();
        }
    }
}