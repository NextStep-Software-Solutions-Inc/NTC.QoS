namespace NTC.QoS.Domain.Entities
{
    public class Provider : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LicenseNumber { get; set; }
    }

}


