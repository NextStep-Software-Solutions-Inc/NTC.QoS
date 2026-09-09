using FluentAssertions;
using Moq;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Interfaces;
using System.Linq.Expressions;
using Xunit;

namespace NTC.QoS.Tests.Application.Services;

public class UserRegionServiceTests
{
    private readonly Mock<IUserRegionRepository> _userRegionRepositoryMock;
    private readonly Mock<IRepository<Region>> _regionRepositoryMock;
    private readonly UserRegionService _service;

    public UserRegionServiceTests()
    {
        _userRegionRepositoryMock = new Mock<IUserRegionRepository>();
        _regionRepositoryMock = new Mock<IRepository<Region>>();
        _service = new UserRegionService(_userRegionRepositoryMock.Object, _regionRepositoryMock.Object);
    }

    [Fact]
    public async Task AssignAllRegionsToUserAsync_WhenNoRegionsInDatabase_DoesNothing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(new List<Region>().AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Region>());

        // Act
        await _service.AssignAllRegionsToUserAsync(userId);

        // Assert
        _userRegionRepositoryMock.Verify(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()), Times.Never);
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Never);
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignAllRegionsToUserAsync_WhenMissingRegionsExist_AddsThemWithActiveStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var region1 = new Region { Id = Guid.NewGuid(), Name = "NCR", Code = "NCR", IsDeleted = false };
        var region2 = new Region { Id = Guid.NewGuid(), Name = "Region I", Code = "R01", IsDeleted = false };
        var allRegions = new List<Region> { region1, region2 };

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(allRegions.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRegions);

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(new List<UserRegion>().AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRegion>());

        List<UserRegion>? addedUserRegions = null;
        _userRegionRepositoryMock.Setup(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()))
            .Callback<IEnumerable<UserRegion>>(list => addedUserRegions = list.ToList());

        // Act
        await _service.AssignAllRegionsToUserAsync(userId);

        // Assert
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Once);
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        addedUserRegions.Should().NotBeNull();
        addedUserRegions!.Count.Should().Be(2);
        addedUserRegions.Should().OnlyContain(ur => ur.UserId == userId && ur.Status == UserRegionStatus.Active.ToString());
        addedUserRegions.Select(ur => ur.RegionId).Should().BeEquivalentTo(new[] { region1.Id, region2.Id });
    }

    [Fact]
    public async Task AssignAllRegionsToUserAsync_WhenPendingOrInactiveRegionsExist_UpdatesThemToActive()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var region1 = new Region { Id = Guid.NewGuid(), Name = "NCR", Code = "NCR", IsDeleted = false };
        var region2 = new Region { Id = Guid.NewGuid(), Name = "Region I", Code = "R01", IsDeleted = false };
        var allRegions = new List<Region> { region1, region2 };

        var existingUserRegion1 = new UserRegion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RegionId = region1.Id,
            Status = UserRegionStatus.Pending.ToString(),
            IsDeleted = false
        };

        var existingUserRegion2 = new UserRegion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RegionId = region2.Id,
            Status = UserRegionStatus.Inactive.ToString(),
            IsDeleted = false
        };

        var existingList = new List<UserRegion> { existingUserRegion1, existingUserRegion2 };

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(allRegions.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRegions);

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(existingList.AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingList);

        // Act
        await _service.AssignAllRegionsToUserAsync(userId);

        // Assert
        existingUserRegion1.Status.Should().Be(UserRegionStatus.Active.ToString());
        existingUserRegion2.Status.Should().Be(UserRegionStatus.Active.ToString());

        _userRegionRepositoryMock.Verify(r => r.Update(existingUserRegion1), Times.Once);
        _userRegionRepositoryMock.Verify(r => r.Update(existingUserRegion2), Times.Once);
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Never);
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignAllRegionsToUserAsync_WhenAllRegionsAlreadyActive_PerformsNoUpdatesOrAdditions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var region1 = new Region { Id = Guid.NewGuid(), Name = "NCR", Code = "NCR", IsDeleted = false };
        var allRegions = new List<Region> { region1 };

        var existingUserRegion1 = new UserRegion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RegionId = region1.Id,
            Status = UserRegionStatus.Active.ToString(),
            IsDeleted = false
        };

        var existingList = new List<UserRegion> { existingUserRegion1 };

        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<Region, bool>>>()))
            .Returns(allRegions.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allRegions);

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(existingList.AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingList);

        // Act
        await _service.AssignAllRegionsToUserAsync(userId);

        // Assert
        _userRegionRepositoryMock.Verify(r => r.Update(It.IsAny<UserRegion>()), Times.Never);
        _userRegionRepositoryMock.Verify(r => r.AddRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Never);
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAllRegionsForUserAsync_WhenUserHasRegions_RemovesAllRegions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userRegion1 = new UserRegion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RegionId = Guid.NewGuid(),
            Status = UserRegionStatus.Active.ToString(),
            IsDeleted = false
        };

        var userRegion2 = new UserRegion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RegionId = Guid.NewGuid(),
            Status = UserRegionStatus.Inactive.ToString(),
            IsDeleted = false
        };

        var existingList = new List<UserRegion> { userRegion1, userRegion2 };

        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(existingList.AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingList);

        // Act
        await _service.DeleteAllRegionsForUserAsync(userId);

        // Assert
        _userRegionRepositoryMock.Verify(r => r.RemoveRange(existingList), Times.Once);
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAllRegionsForUserAsync_WhenUserHasNoRegions_DoesNothing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRegionRepositoryMock.Setup(r => r.Query(It.IsAny<Expression<Func<UserRegion, bool>>>()))
            .Returns(new List<UserRegion>().AsQueryable());
        _userRegionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<UserRegion>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRegion>());

        // Act
        await _service.DeleteAllRegionsForUserAsync(userId);

        // Assert
        _userRegionRepositoryMock.Verify(r => r.RemoveRange(It.IsAny<IEnumerable<UserRegion>>()), Times.Never);
        _userRegionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
