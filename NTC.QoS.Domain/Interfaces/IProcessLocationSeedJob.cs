using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Domain.Interfaces;

/// <summary>
/// Background job interface for processing location seeding operations
/// </summary>
public interface IProcessLocationSeedJob : IBackgroundJob<LocationSeedParametersDto>
{
}
