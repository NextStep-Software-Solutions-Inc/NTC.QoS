namespace NTC.QoS.Domain.Models.DTOs.NAP
{
    public class NAPZoneDto
    {
        public Guid? Id { get; set; }
        public required string Name { get; set; }
        public BarangayNAPDto? Barangay { get; set; }
        public MunicipalityNAPDto? Municipality { get; set; }
        public ProvinceNAPDto? Province { get; set; }
        public RegionNAPDto? Region { get; set; }
    }

    public class BarangayNAPDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
    }

    public class MunicipalityNAPDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public bool IsCity { get; set; }
    }

    public class ProvinceNAPDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
    }

    public class RegionNAPDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
    }
}
