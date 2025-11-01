using MediatR;
using NTC.QoS.Domain.Models.DTOs.Common;
using NTC.QoS.Domain.Models.DTOs.NAP;
using NTC.QoS.Domain.Models.RequestParams;

namespace NTC.QoS.Application.Features.NAP.Requests;

/// <summary>
/// Request for retrieving NAPs within a map viewport bounding box.
/// Optimized for v2 API - viewport-based spatial filtering to prevent OOM and improve UI performance.
/// This request uses rectangular viewport bounds instead of radius-based distance filtering.
/// Returns lightweight NAPViewportDto without Guid IDs for optimal payload size.
/// </summary>
public class GetAllNAPsViewportRequest : IRequest<PaginatedResponse<NAPViewportDto>>
{
    /// <summary>
    /// Page number for pagination (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Viewport bounding box defining the visible map area.
    /// Only NAPs within this viewport will be returned.
    /// </summary>
    public required ViewportBoundsDto Viewport { get; set; }

    /// <summary>
    /// Optional: Filter by specific service provider IDs.
    /// If null or empty, all service providers are included.
    /// </summary>
    public List<Guid>? ServiceProviderIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific barangay IDs.
    /// Applies administrative boundary filtering in addition to viewport.
    /// </summary>
    public List<Guid>? BarangayIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific municipality IDs.
    /// Applies administrative boundary filtering in addition to viewport.
    /// </summary>
    public List<Guid>? MunicipalityIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific province IDs.
    /// Applies administrative boundary filtering in addition to viewport.
    /// </summary>
    public List<Guid>? ProvinceIds { get; set; }

    /// <summary>
    /// Optional: Filter by specific region IDs.
    /// Applies administrative boundary filtering in addition to viewport.
    /// </summary>
    public List<Guid>? RegionIds { get; set; }

    public GetAllNAPsViewportRequest(int pageNumber, int pageSize, ViewportBoundsDto viewport)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        Viewport = viewport;
    }
}
