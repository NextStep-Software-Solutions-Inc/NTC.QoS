using NTC.QoS.Domain.Models.DTOs.Municipality;
using NTC.QoS.Domain.Models.DTOs.Study;

namespace NTC.QoS.Domain.Models.DTOs.StudyMunicipality;

public class StudyMunicipalityDto
{
    public Guid Id { get; set; }
    public MunicipalityDto? Municipality { get; set; }
    public StudyDto? Study { get; set; }
}