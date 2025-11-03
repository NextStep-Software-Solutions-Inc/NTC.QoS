using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("admin")]
// [Authorize(Policy = "AdminOnly")] // Commented out for development - REMOVE IN PRODUCTION!
[AllowAnonymous] // Allow access without authentication during development
public class AdminController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;

    public AdminController(IBackgroundJobService backgroundJobService)
    {
        _backgroundJobService = backgroundJobService;
    }

    /// <summary>
    /// Schedules a recurring job to pre-calculate province ISP counts every 12 hours
    /// This prevents expensive spatial queries on every API request
    /// </summary>
    [HttpPost("schedule-province-isp-count-job")]
    public IActionResult ScheduleProvinceISPCountJob()
    {
        // Schedule job to run every 12 hours (at 2 AM and 2 PM)
        _backgroundJobService.AddOrUpdateRecurringJob<ICalculateProvinceISPCountsJob>(
            "calculate-province-isp-counts",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 2,14 * * *" // Runs at 2:00 AM and 2:00 PM every day
        );

        return Ok(new
        {
            message = "Province ISP count calculation job scheduled successfully",
            schedule = "Every 12 hours (2:00 AM and 2:00 PM)",
            nextRun = "Check Hangfire dashboard for next execution time"
        });
    }

    /// <summary>
    /// Manually triggers the province ISP count calculation job immediately
    /// </summary>
    [HttpPost("trigger-province-isp-count-job")]
    public IActionResult TriggerProvinceISPCountJob()
    {
        var jobId = _backgroundJobService.Enqueue<ICalculateProvinceISPCountsJob>(
            job => job.ExecuteAsync(CancellationToken.None)
        );

        return Ok(new
        {
            message = "Province ISP count calculation job triggered successfully",
            jobId,
            note = "Check Hangfire dashboard to monitor progress"
        });
    }

    /// <summary>
    /// Removes the recurring province ISP count job
    /// </summary>
    [HttpDelete("remove-province-isp-count-job")]
    public IActionResult RemoveProvinceISPCountJob()
    {
        _backgroundJobService.RemoveRecurringJob("calculate-province-isp-counts");

        return Ok(new
        {
            message = "Province ISP count calculation recurring job removed successfully"
        });
    }
}
