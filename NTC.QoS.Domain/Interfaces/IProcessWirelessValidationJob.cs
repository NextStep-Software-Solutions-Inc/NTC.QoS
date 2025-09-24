using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Domain.Interfaces;

public interface IProcessWirelessValidationJob : IBackgroundJob<FileDto>
{
}
