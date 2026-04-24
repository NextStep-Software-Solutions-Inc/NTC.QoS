using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.Reports.ConnectivityGap.Handlers.Queries;
using NTC.QoS.Application.Features.Reports.ConnectivityGap.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Reports.ConnectivityGap;

public class GetConnectivityGapReportQueryHandlerTests
{
    private readonly Mock<IConnectivityAssessmentRepository> _assessmentRepositoryMock = new();
    private readonly Mock<IBarangayRepository> _barangayRepositoryMock = new();
    private readonly Mock<IMunicipalityRepository> _municipalityRepositoryMock = new();
    private readonly Mock<IProvinceRepository> _provinceRepositoryMock = new();
    private readonly Mock<IRegionRepository> _regionRepositoryMock = new();
    private readonly Mock<IBarangayCoverageRepository> _barangayCoverageRepositoryMock = new();
    private readonly Mock<ILogger<GetConnectivityGapReportQueryHandler>> _loggerMock = new();

    private readonly GetConnectivityGapReportQueryHandler _handler;

    private readonly Guid _barangayId1 = Guid.NewGuid();
    private readonly Guid _barangayId2 = Guid.NewGuid();
    private readonly Guid _barangayId3 = Guid.NewGuid();

    public GetConnectivityGapReportQueryHandlerTests()
    {
        _handler = new GetConnectivityGapReportQueryHandler(
            _assessmentRepositoryMock.Object,
            _barangayRepositoryMock.Object,
            _municipalityRepositoryMock.Object,
            _provinceRepositoryMock.Object,
            _regionRepositoryMock.Object,
            _barangayCoverageRepositoryMock.Object,
            _loggerMock.Object);

        SetupTestData();
    }

    private void SetupTestData()
    {
        var assessments = new List<ConnectivityAssessment>
        {
            CreateAssessment(_barangayId1, ConnectivityStatusEnum.Served,      totalScore: 12, population: 100),
            CreateAssessment(_barangayId2, ConnectivityStatusEnum.Underserved,  totalScore: 8,  population: 200),
            CreateAssessment(_barangayId3, ConnectivityStatusEnum.Unserved,     totalScore: 3,  population: 300),
        };

        var coverages = new List<BarangayCoverage>
        {
            new() { Id = Guid.NewGuid(), BarangayId = _barangayId1 },
            new() { Id = Guid.NewGuid(), BarangayId = _barangayId2 },
            new() { Id = Guid.NewGuid(), BarangayId = _barangayId3 },
        };

        var barangays = new List<Barangay>
        {
            new() { Id = _barangayId1, Name = "Barangay 1" },
            new() { Id = _barangayId2, Name = "Barangay 2" },
            new() { Id = _barangayId3, Name = "Barangay 3" },
        };

        _assessmentRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<ConnectivityAssessment, bool>>>()))
            .Returns<Expression<Func<ConnectivityAssessment, bool>>>(expr =>
                assessments.AsQueryable().Where(expr));

        _assessmentRepositoryMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<ConnectivityAssessment>>(), It.IsAny<CancellationToken>()))
            .Returns<IQueryable<ConnectivityAssessment>, CancellationToken>((q, ct) =>
                Task.FromResult(q.ToList()));

        _barangayCoverageRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<BarangayCoverage, bool>>>()))
            .Returns(coverages.AsQueryable());

        _barangayRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<Barangay, bool>>>()))
            .Returns<Expression<Func<Barangay, bool>>>(expr =>
                barangays.AsQueryable().Where(expr));

        _barangayRepositoryMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns<IQueryable<Guid>, CancellationToken>((q, ct) =>
                Task.FromResult(q.ToList()));

        _barangayRepositoryMock
            .Setup(r => r.QueryAsNoTracking(It.IsAny<Expression<Func<Barangay, bool>>>()))
            .Returns<Expression<Func<Barangay, bool>>>(expr =>
                barangays.AsQueryable().Where(expr));
    }

    private static ConnectivityAssessment CreateAssessment(
        Guid locationId,
        ConnectivityStatusEnum status,
        int totalScore,
        int? population)
    {
        return new ConnectivityAssessment
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            LocationType = LocationTypeEnum.Barangay,
            ConnectivityStatus = status,
            TotalScore = totalScore,
            Population = population,
            AssessmentDate = DateTime.UtcNow.AddDays(-1),
        };
    }

    [Fact]
    public async Task Handle_WithNoStatusFilter_ReturnsAllGapAreas()
    {
        var query = new GetConnectivityGapReportQuery { PageNumber = 1, PageSize = 100 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        // Gap report excludes Served; 2 areas (Underserved + Unserved) should be returned
        result.Data!.TotalPriorityAreas.Should().Be(2);
        result.Data.PriorityAreas.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoStatusFilter_SummaryCountsBothGapStatuses()
    {
        var query = new GetConnectivityGapReportQuery { PageNumber = 1, PageSize = 100 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Summary.TotalUnservedAreas.Should().Be(1);
        result.Data.Summary.TotalUnderservedAreas.Should().Be(1);
        result.Data.Summary.UnservedPopulation.Should().Be(300);
        result.Data.Summary.UnderservedPopulation.Should().Be(200);
    }

    [Fact]
    public async Task Handle_WithUnservedFilter_ReturnsOnlyUnservedAreas()
    {
        var query = new GetConnectivityGapReportQuery
        {
            FilterByStatus = ConnectivityStatusEnum.Unserved,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalPriorityAreas.Should().Be(1);
        result.Data.PriorityAreas.Should().HaveCount(1);
        result.Data.PriorityAreas[0].ConnectivityStatus.Should().Be(ConnectivityStatusEnum.Unserved);
    }

    [Fact]
    public async Task Handle_WithUnderservedFilter_ReturnsOnlyUnderservedAreas()
    {
        var query = new GetConnectivityGapReportQuery
        {
            FilterByStatus = ConnectivityStatusEnum.Underserved,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalPriorityAreas.Should().Be(1);
        result.Data.PriorityAreas.Should().HaveCount(1);
        result.Data.PriorityAreas[0].ConnectivityStatus.Should().Be(ConnectivityStatusEnum.Underserved);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_SummaryStillReflectsAllGapAreas()
    {
        // When filtering by Unserved only, summary should still show both Unserved and Underserved totals
        var query = new GetConnectivityGapReportQuery
        {
            FilterByStatus = ConnectivityStatusEnum.Unserved,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Summary.TotalUnservedAreas.Should().Be(1);
        result.Data.Summary.TotalUnderservedAreas.Should().Be(1);
        result.Data.Summary.UnservedPopulation.Should().Be(300);
        result.Data.Summary.UnderservedPopulation.Should().Be(200);
    }

    [Fact]
    public async Task Handle_ServedAreaIsNeverIncluded()
    {
        // Gap report should never include served areas regardless of filter
        var query = new GetConnectivityGapReportQuery { PageNumber = 1, PageSize = 100 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PriorityAreas.Should().NotContain(a => a.ConnectivityStatus == ConnectivityStatusEnum.Served);
    }

    [Fact]
    public async Task Handle_Pagination_LimitsResultsPerPage()
    {
        var query = new GetConnectivityGapReportQuery { PageNumber = 1, PageSize = 1 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.PriorityAreas.Should().HaveCount(1);
        result.Data.TotalPriorityAreas.Should().Be(2);
        result.Data.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithInvalidPageNumber_ReturnsValidationError()
    {
        var query = new GetConnectivityGapReportQuery { PageNumber = 0, PageSize = 10 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("PageNumber");
    }

    [Fact]
    public async Task Handle_WithInvalidPageSize_ReturnsValidationError()
    {
        var query = new GetConnectivityGapReportQuery { PageNumber = 1, PageSize = 200 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("PageSize");
    }
}
