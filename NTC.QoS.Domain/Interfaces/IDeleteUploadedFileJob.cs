using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.File;

namespace NTC.QoS.Infrastructure.BackgroundTasks;

public interface IDeleteUploadedFileJob: IBackgroundJob<FileDto>
{
}