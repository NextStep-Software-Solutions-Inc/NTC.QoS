using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Domain.Interfaces;

public interface IProcessCellJob : IBackgroundJob<FileDto>
{
}
