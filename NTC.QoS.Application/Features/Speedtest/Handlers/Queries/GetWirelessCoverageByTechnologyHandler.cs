//// Services/WirelessCoverageCalculator.cs
//using Microsoft.EntityFrameworkCore;
//using NTC.QoS.Domain.Entities;
//using NTC.QoS.Domain.Interfaces;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//// Assuming your Speedtest model has a 'Location' property of type 'Point'
//// For example: public Point Location { get; set; } (where Point has Latitude, Longitude)
//// Or, if you have separate Latitude/Longitude properties directly in Speedtest:
//// public double Latitude { get; set; }
//// public double Longitude { get; set; }


//public class WirelessCoverageCalculator
//{
//    private readonly ISpeedtestRepository _speedtestRepository;

//    public WirelessCoverageCalculator(ISpeedtestRepository speedtestRepository)
//    {
//        _speedtestRepository = speedtestRepository;
//    }

//    public async Task<WirelessCoverageResponse> CalculateWirelessCoverage(WirelessCoverageRequest request)
//    {
//        IQueryable<Speedtest> query = _speedtestRepository.GetAllSpeedtestsWithTechnology();

//        // Apply Bounding Box Filters
//        if (request.MinLatitude.HasValue && request.MaxLatitude.HasValue &&
//            request.MinLongitude.HasValue && request.MaxLongitude.HasValue)
//        {
//            query = query.Where(s =>
//                s.Location != null && // Ensure Location is not null
//                s.Location.Latitude >= request.MinLatitude.Value &&
//                s.Location.Latitude <= request.MaxLatitude.Value &&
//                s.Location.Longitude >= request.MinLongitude.Value &&
//                s.Location.Longitude <= request.MaxLongitude.Value
//            );
//        }
//        // else if you have separate Latitude/Longitude properties in Speedtest:
//        // {
//        //     query = query.Where(s =>
//        //         s.Latitude >= request.MinLatitude.Value &&
//        //         s.Latitude <= request.MaxLatitude.Value &&
//        //         s.Longitude >= request.MinLongitude.Value &&
//        //         s.Longitude <= request.MaxLongitude.Value
//        //     );
//        // }


//        // Apply other filters (ServiceProviderId, StartDate, EndDate) - these remain the same
//        if (request.ServiceProviderId.HasValue)
//        {
//            query = query.Where(s => s.ServiceProviderId == request.ServiceProviderId.Value);
//        }
//        if (request.StartDate.HasValue)
//        {
//            query = query.Where(s => s.DateTime >= request.StartDate.Value);
//        }
//        if (request.EndDate.HasValue)
//        {
//            query = query.Where(s => s.DateTime < request.EndDate.Value.AddDays(1));
//        }

//        // --- Perform calculations for each technology (same as before) ---
//        var totalSpeedtests = await query.CountAsync();

//        if (totalSpeedtests == 0)
//        {
//            return new WirelessCoverageResponse
//            {
//                LteAnd5GPercentage = 0.0,
//                NR5GPercentage = 0.0,
//                LtePercentage = 0.0,
//                HspapPercentage = 0.0,
//                EdgePercentage = 0.0,
//                CoveredRegionOrArea = "No specific area filtered, or no data found", // Default message
//                Message = "No speed test records found for the specified criteria."
//            };
//        }

//        var nr5GSpeedtests = await query.Where(s => s.Generation != null && s.Generation.TechnologyId != null && s.Generation.Technology.Standard == "NR").CountAsync();
//        var lteSpeedtests = await query.Where(s => s.Generation != null && s.Generation.Technology != null && s.Generation.Technology.Standard == "LTE").CountAsync();
//        var hspapSpeedtests = await query.Where(s => s.Generation != null && s.Generation.Technology != null && s.Generation.Technology.Standard == "HSPAP").CountAsync();
//        var edgeSpeedtests = await query.Where(s => s.Generation != null && s.Generation.Technology != null && s.Generation.Technology.Standard == "EDGE").CountAsync();

//        string areaDescription = "Global";
//        if (request.MinLatitude.HasValue) // Indicates a bounding box was provided
//        {
//            areaDescription = $"Lat: {request.MinLatitude}-{request.MaxLatitude}, Lon: {request.MinLongitude}-{request.MaxLongitude}";
//        }


//        return new WirelessCoverageResponse
//        {
//            LteAnd5GPercentage = (double)(lteSpeedtests + nr5GSpeedtests) / totalSpeedtests * 100.0,
//            NR5GPercentage = (double)nr5GSpeedtests / totalSpeedtests * 100.0,
//            LtePercentage = (double)lteSpeedtests / totalSpeedtests * 100.0,
//            HspapPercentage = (double)hspapSpeedtests / totalSpeedtests * 100.0,
//            EdgePercentage = (double)edgeSpeedtests / totalSpeedtests * 100.0,
//            CoveredRegionOrArea = areaDescription,
//            Message = "Wireless coverage calculated successfully for specified area."
//        };
//    }
//}