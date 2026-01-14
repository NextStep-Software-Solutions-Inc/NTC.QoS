using Hangfire;
using Hangfire.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NTC.QoS.API.Authorization;
using NTC.QoS.API.Models;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Infrastructure.Configuration;

namespace NTC.QoS.API.Controllers;

/// <summary>
/// Controller for managing database seeding operations
/// </summary>
[ApiController]
[Route("seed", Name = "seed")]
[Authorize]
public class SeedController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<SeedController> _logger;
    private readonly DatabaseOptions _databaseOptions;

    public SeedController(
        IBackgroundJobService backgroundJobService,
        ILogger<SeedController> logger,
        IOptions<DatabaseOptions> databaseOptions)
    {
        _backgroundJobService = backgroundJobService;
        _logger = logger;
        _databaseOptions = databaseOptions.Value;
    }

    /// <summary>
    /// Reseed specific location entities based on the request payload.
    /// This endpoint queues a background job for seeding operations.
    /// </summary>
    /// <param name="request">The seed request containing which entities to reseed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job ID for tracking the seeding operation</returns>
    [HttpPost]
    [RequirePermission(Permissions.SystemAdministration)]
    public IActionResult ReseedEntities([FromBody] SeedRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Reseed request received - Region: {Region}, Province: {Province}, Municipality: {Municipality}, Barangay: {Barangay}, ForceReseed: {ForceReseed}",
            request.RegionEntity, request.ProvinceEntity, request.MunicipalityEntity, request.BarangayEntity, request.ForceReseed);

        if (!request.RegionEntity && !request.ProvinceEntity && !request.MunicipalityEntity && !request.BarangayEntity)
        {
            return BadRequest(new
            {
                success = false,
                message = "At least one entity type must be selected for reseeding.",
                timestamp = DateTime.UtcNow
            });
        }

        try
        {
            var parameters = new LocationSeedParametersDto
            {
                RegionEntity = request.RegionEntity,
                ProvinceEntity = request.ProvinceEntity,
                MunicipalityEntity = request.MunicipalityEntity,
                BarangayEntity = request.BarangayEntity,
                ForceReseed = request.ForceReseed,
                UseOptimizedSeeder = _databaseOptions.UseOptimizedLocationSeeder ?? true
            };

            // Queue the background job
            var jobId = _backgroundJobService.Enqueue<IProcessLocationSeedJob>(job => job.ExecuteAsync(parameters, cancellationToken));

            _logger.LogInformation("Location seeding job queued with ID: {JobId}", jobId);

            return Ok(new
            {
                success = true,
                message = "Reseeding job has been queued successfully",
                jobId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue reseeding job");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to queue reseeding job",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Get the status of a specific seeding job
    /// </summary>
    /// <param name="jobId">The Hangfire job ID</param>
    /// <returns>Job status information</returns>
    [HttpGet("status/{jobId}")]
    [RequirePermission(Permissions.SystemAdministration)]
    public IActionResult GetJobStatus(string jobId)
    {
        try
        {
            using var connection = JobStorage.Current.GetConnection();
            var jobData = connection.GetJobData(jobId);

            if (jobData == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Job with ID '{jobId}' not found",
                    timestamp = DateTime.UtcNow
                });
            }

            // Extract parameters from job arguments
            SeedJobParameters? parameters = null;
            if (jobData.Job?.Args?.Count > 0 && jobData.Job.Args[0] is LocationSeedParametersDto seedParams)
            {
                parameters = new SeedJobParameters
                {
                    RegionEntity = seedParams.RegionEntity,
                    ProvinceEntity = seedParams.ProvinceEntity,
                    MunicipalityEntity = seedParams.MunicipalityEntity,
                    BarangayEntity = seedParams.BarangayEntity,
                    ForceReseed = seedParams.ForceReseed
                };
            }

            var response = new SeedJobStatusResponse
            {
                JobId = jobId,
                State = jobData.State ?? "Unknown",
                CreatedAt = jobData.CreatedAt,
                Parameters = parameters
            };

            // Get state-specific details
            var stateData = connection.GetStateData(jobId);
            if (stateData != null)
            {
                if (stateData.Name == "Processing" && stateData.Data.ContainsKey("StartedAt"))
                {
                    response.StartedAt = JobHelper.DeserializeDateTime(stateData.Data["StartedAt"]);
                }
                else if (stateData.Name == "Succeeded")
                {
                    if (stateData.Data.ContainsKey("SucceededAt"))
                    {
                        response.FinishedAt = JobHelper.DeserializeDateTime(stateData.Data["SucceededAt"]);
                    }
                    if (stateData.Data.ContainsKey("Result"))
                    {
                        response.Reason = stateData.Data["Result"];
                    }
                }
                else if (stateData.Name == "Failed")
                {
                    if (stateData.Data.ContainsKey("FailedAt"))
                    {
                        response.FinishedAt = JobHelper.DeserializeDateTime(stateData.Data["FailedAt"]);
                    }
                    if (stateData.Data.ContainsKey("ExceptionMessage"))
                    {
                        response.ExceptionMessage = stateData.Data["ExceptionMessage"];
                    }
                }
            }

            return Ok(new
            {
                success = true,
                data = response,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get job status for job ID: {JobId}", jobId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to retrieve job status",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
