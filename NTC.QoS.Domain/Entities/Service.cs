using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;

namespace NTC.QoS.Domain.Entities;


public class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ServiceTypeEnum Type { get; set; } = ServiceTypeEnum.Unknown;
    public virtual List<ServiceProvider> ServiceProviders { get; set; } = new();

}
