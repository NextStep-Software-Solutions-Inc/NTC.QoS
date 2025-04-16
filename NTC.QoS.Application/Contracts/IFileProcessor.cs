using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Application.Contracts;

public interface IFileProcessor
{
    Task ProcessFileAsync(Domain.Entities.File file);
    FileTypeEnum FileType { get; }
}
