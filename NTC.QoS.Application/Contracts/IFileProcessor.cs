using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Models.DTOs.File;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Contracts;

public interface IFileProcessor
{
    //Task ProcessFileAsync(Domain.Entities.File file);
    Task<ApiResponse<ProcessingResultDto>> ProcessFileAsync(Domain.Entities.File file);
    FileTypeEnum FileType { get; }
}
