using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.User.Handlers.Commands;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.User;
using System.Text.Json;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

public class ResendInvitationHandlerTests
{
    private readonly Mock<IClerkUserService> _clerkUserServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ResendInvitationHandler>> _loggerMock;
    private readonly ResendInvitationHandler _handler;

    public ResendInvitationHandlerTests()
    {
        _clerkUserServiceMock = new Mock<IClerkUserService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ResendInvitationHandler>>();

        _handler = new ResendInvitationHandler(
            _clerkUserServiceMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidPendingInvitation_DelegatesToInviteUserHandler()
    {
        // Arrange
        var invitationId = "inv_resend_123";
        var roleId = Guid.NewGuid();
        var regionId = Guid.NewGuid();

        var metaJson = JsonSerializer.SerializeToElement(new
        {
            role_id = roleId.ToString(),
            regions = new[]
            {
                new { region_id = regionId.ToString(), role_id = roleId.ToString(), status = "Active" }
            }
        });

        var pendingInvite = new ClerkInvitationResponse
        {
            Id = invitationId,
            EmailAddress = "resend_test@example.com",
            Status = "pending",
            PublicMetadata = metaJson
        };

        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync("pending"))
            .ReturnsAsync(new List<ClerkInvitationResponse> { pendingInvite });

        _mediatorMock.Setup(m => m.Send(It.IsAny<InviteUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Message = "Invitation sent successfully via Clerk."
            });

        // Act
        var result = await _handler.Handle(new ResendInvitationRequest(invitationId), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Contain("resent successfully");
        _mediatorMock.Verify(m => m.Send(It.Is<InviteUserRequest>(req =>
            req.User.Email == "resend_test@example.com" &&
            req.User.RoleId == roleId &&
            req.User.Regions != null &&
            req.User.Regions.Count == 1 &&
            req.User.Regions[0].RegionId == regionId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvitationNotFound_ReturnsError()
    {
        // Arrange
        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync("pending"))
            .ReturnsAsync(new List<ClerkInvitationResponse>());

        // Act
        var result = await _handler.Handle(new ResendInvitationRequest("non_existent"), CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("not found");
        _mediatorMock.Verify(m => m.Send(It.IsAny<InviteUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
