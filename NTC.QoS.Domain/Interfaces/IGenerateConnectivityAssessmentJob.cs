using NTC.QoS.Domain.Interfaces;

namespace NTC.QoS.Domain.Interfaces;

public interface IGenerateConnectivityAssessmentJob : IBackgroundJob<Guid>
{
}