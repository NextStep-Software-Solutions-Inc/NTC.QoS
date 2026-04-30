using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.Reports.Connectivity.Handlers.Queries;
using NTC.QoS.Application.Features.Reports.Connectivity.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Reports.Connectivity;

public class GetConnectivityReportQueryHandlerTests
{
    private readonly Mock<IConnectivityAssessmentRepository> _assessmentRepositoryMock = new();
    private readonly Mock<IBarangayRepository> _barangayRepositoryMock = new();
    private readonly Mock<IMunicipalityRepository> _municipalityRepositoryMock = new();
    private readonly Mock<IProvinceRepository> _provinceRepositoryMock = new();
    private readonly Mock<IRegionRepository> _regionRepositoryMock = new();
    private readonly Mock<IBarangayCoverageRepository> _barangayCoverageRepositoryMock = new();
    private readonly Mock<ILogger<GetConnectivityReportQueryHandler>> _loggerMock = new();

    private readonly GetConnectivityReportQueryHandler _handler;

    private readonly Guid _barangayId1 = Guid.NewGuid();
    private readonly Guid _barangayId2 = Guid.NewGuid();
    private readonly Guid _barangayId3 = Guid.NewGuid();

    public GetConnectivityReportQueryHandlerTests()
    {
        _handler = new GetConnectivityReportQueryHandler(
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
            CreateAssessment(_barangayId1, ConnectivityStatusEnum.Served, totalScore: 12, population: 100),
            CreateAssessment(_barangayId2, ConnectivityStatusEnum.Underserved, totalScore: 8, population: 200),
            CreateAssessment(_barangayId3, ConnectivityStatusEnum.Unserved, totalScore: 3, population: 300),
        };

        var coverages = new List<BarangayCoverage>
        {
            new() { Id = Guid.NewGuid(), BarangayId = _barangayId1 },
            new() { Id = Guid.NewGuid(), BarangayId = _barangayId2 },
            new() { Id = Guid.NewGuid(), BarangayId = _barangayId3 },
        };

        var province = new Province { Id = Guid.NewGuid(), Name = "Test Province" };
        var municipality = new Municipality { Id = Guid.NewGuid(), Name = "Test Municipality", Province = province, ProvinceId = province.Id };

        var barangays = new List<Barangay>
        {
            new() { Id = _barangayId1, Name = "Barangay 1", Municipality = municipality, MunicipalityId = municipality.Id },
            new() { Id = _barangayId2, Name = "Barangay 2", Municipality = municipality, MunicipalityId = municipality.Id },
            new() { Id = _barangayId3, Name = "Barangay 3", Municipality = municipality, MunicipalityId = municipality.Id },
        };

        // Assessment repository
        _assessmentRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<ConnectivityAssessment, bool>>>()))
            .Returns<Expression<Func<ConnectivityAssessment, bool>>>(expr =>
                assessments.AsQueryable().Where(expr));

        // CountAsync — executes the in-memory queryable count
        _assessmentRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<IQueryable<ConnectivityAssessment>>(), It.IsAny<CancellationToken>()))
            .Returns<IQueryable<ConnectivityAssessment>, CancellationToken>((q, ct) =>
                Task.FromResult(q.Count()));

        // Generic ToListAsync — handles ConnectivityAssessment entities, projection types, and Guid lists
        _assessmentRepositoryMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .Returns(new InvocationFunc(invocation =>
            {
                var enumerable = (System.Collections.IEnumerable)invocation.Arguments[0];
                var elementType = invocation.Method.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                foreach (var item in enumerable)
                    list.Add(item);
                return typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(listType)
                    .Invoke(null, new object[] { list })!;
            }));

        // Coverage repository — returns all coverage records for any expression (including null)
        _barangayCoverageRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<BarangayCoverage, bool>>>()))
            .Returns(coverages.AsQueryable());

        // Barangay repository — used for covered-barangay ID lookup
        _barangayRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<Barangay, bool>>>()))
            .Returns<Expression<Func<Barangay, bool>>>(expr =>
                barangays.AsQueryable().Where(expr));

        // QueryAsNoTracking used for name lookups — return matching barangays in-memory
        _barangayRepositoryMock
            .Setup(r => r.QueryAsNoTracking(It.IsAny<Expression<Func<Barangay, bool>>>()))
            .Returns<Expression<Func<Barangay, bool>>>(expr =>
                barangays.AsQueryable().Where(expr));

        // Generic ToListAsync — handles Guid projections, anonymous-type projections, and entity lists
        _barangayRepositoryMock
            .Setup(r => r.ToListAsync(It.IsAny<IQueryable<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .Returns(new InvocationFunc(invocation =>
            {
                var enumerable = (System.Collections.IEnumerable)invocation.Arguments[0];
                var elementType = invocation.Method.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                foreach (var item in enumerable)
                    list.Add(item);
                return typeof(Task)
                    .GetMethod(nameof(Task.FromResult))!
                    .MakeGenericMethod(listType)
                    .Invoke(null, new object[] { list })!;
            }));
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
    public async Task Handle_WithNoStatusFilter_ReturnsAllAreas()
    {
        var query = new GetConnectivityReportQuery { PageNumber = 1, PageSize = 100 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalAreas.Should().Be(3);
        result.Data.Areas.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoStatusFilter_SummaryCountsAllStatuses()
    {
        var query = new GetConnectivityReportQuery { PageNumber = 1, PageSize = 100 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalServedAreas.Should().Be(1);
        result.Data.TotalUnderservedAreas.Should().Be(1);
        result.Data.TotalUnservedAreas.Should().Be(1);
        result.Data.ServedPopulation.Should().Be(100);
        result.Data.UnderservedPopulation.Should().Be(200);
        result.Data.UnservedPopulation.Should().Be(300);
    }

    [Fact]
    public async Task Handle_WithServedFilter_ReturnsOnlyServedAreas()
    {
        var query = new GetConnectivityReportQuery
        {
            FilterByStatus = ConnectivityStatusEnum.Served,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalAreas.Should().Be(1);
        result.Data.Areas.Should().HaveCount(1);
        result.Data.Areas[0].ConnectivityStatus.Should().Be(ConnectivityStatusEnum.Served);
    }

    [Fact]
    public async Task Handle_WithUnderservedFilter_ReturnsOnlyUnderservedAreas()
    {
        var query = new GetConnectivityReportQuery
        {
            FilterByStatus = ConnectivityStatusEnum.Underserved,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalAreas.Should().Be(1);
        result.Data.Areas[0].ConnectivityStatus.Should().Be(ConnectivityStatusEnum.Underserved);
    }

    [Fact]
    public async Task Handle_WithUnservedFilter_ReturnsOnlyUnservedAreas()
    {
        var query = new GetConnectivityReportQuery
        {
            FilterByStatus = ConnectivityStatusEnum.Unserved,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalAreas.Should().Be(1);
        result.Data.Areas[0].ConnectivityStatus.Should().Be(ConnectivityStatusEnum.Unserved);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_SummaryStillReflectsFullDataset()
    {
        // Even when filtering by Served, summary counts should reflect the full unfiltered dataset
        var query = new GetConnectivityReportQuery
        {
            FilterByStatus = ConnectivityStatusEnum.Served,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalServedAreas.Should().Be(1);
        result.Data.TotalUnderservedAreas.Should().Be(1);
        result.Data.TotalUnservedAreas.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Pagination_LimitsResultsPerPage()
    {
        var query = new GetConnectivityReportQuery { PageNumber = 1, PageSize = 2 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Areas.Should().HaveCount(2);
        result.Data.TotalAreas.Should().Be(3);
        result.Data.TotalPages.Should().Be(2);
        result.Data.PageSize.Should().Be(2);
        result.Data.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Pagination_SecondPageReturnsRemainingItems()
    {
        var query = new GetConnectivityReportQuery { PageNumber = 2, PageSize = 2 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Areas.Should().HaveCount(1);
        result.Data.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithInvalidPageNumber_ReturnsValidationError()
    {
        var query = new GetConnectivityReportQuery { PageNumber = 0, PageSize = 10 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("PageNumber");
    }

    [Fact]
    public async Task Handle_WithInvalidPageSize_ReturnsValidationError()
    {
        var query = new GetConnectivityReportQuery { PageNumber = 1, PageSize = 200 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("PageSize");
    }

    [Fact]
    public async Task Handle_WithUndefinedFilterByStatus_ReturnsValidationError()
    {
        var query = new GetConnectivityReportQuery
        {
            FilterByStatus = (ConnectivityStatusEnum)999,
            PageNumber = 1,
            PageSize = 100
        };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainKey("FilterByStatus");
    }

    [Fact]
    public async Task Handle_AreaDetails_IncludeConnectivityStatusField()
    {
        var query = new GetConnectivityReportQuery { PageNumber = 1, PageSize = 100 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var statuses = result.Data!.Areas.Select(a => a.ConnectivityStatus).Distinct().ToList();
        statuses.Should().Contain(ConnectivityStatusEnum.Served);
        statuses.Should().Contain(ConnectivityStatusEnum.Underserved);
        statuses.Should().Contain(ConnectivityStatusEnum.Unserved);
    }

    [Fact]
    public async Task Handle_EmptyResults_ReturnSucceededWithZeroCounts()
    {
        // Override mock to return no assessments
        _assessmentRepositoryMock
            .Setup(r => r.Query(It.IsAny<Expression<Func<ConnectivityAssessment, bool>>>()))
            .Returns(Enumerable.Empty<ConnectivityAssessment>().AsQueryable());

        _assessmentRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<IQueryable<ConnectivityAssessment>>(), It.IsAny<CancellationToken>()))
            .Returns<IQueryable<ConnectivityAssessment>, CancellationToken>((q, ct) =>
                Task.FromResult(q.Count()));

        var query = new GetConnectivityReportQuery { PageNumber = 1, PageSize = 10 };

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.TotalAreas.Should().Be(0);
        result.Data.Areas.Should().BeEmpty();
        result.Data.TotalPages.Should().Be(0);
        result.Data.TotalServedAreas.Should().Be(0);
        result.Data.TotalUnderservedAreas.Should().Be(0);
        result.Data.TotalUnservedAreas.Should().Be(0);
    }
}
