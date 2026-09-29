using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.API.Controllers;
using NTC.QoS.API.Filters;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;
using NTC.QoS.Domain.Models.DTOs.User;
using NTC.QoS.Tests.Helpers;
using Xunit;

namespace NTC.QoS.Tests.API.Controllers;

public class WebhookControllerIntegrationTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IClerkUserService> _clerkUserServiceMock = new();
    private readonly Mock<ILogger<UserProvisioningService>> _loggerMock = new();
    private readonly UserProvisioningService _userProvisioningService;
    private readonly WebhookController _controller;
    private readonly ClerkWebhookVerificationAttribute _verificationFilter = new();

    public WebhookControllerIntegrationTests()
    {
        _userProvisioningService = new UserProvisioningService(
            _mediatorMock.Object,
            _clerkUserServiceMock.Object,
            _loggerMock.Object);

        _controller = new WebhookController(_mediatorMock.Object, _userProvisioningService);
    }

    private DefaultHttpContext CreateHttpContextWithFilter(
        string body,
        string secret = SvixTestHelper.TestSecret,
        bool omitHeaders = false)
    {
        var services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Clerk:WebhookSecret", secret }
            })
            .Build();
        services.AddSingleton(config);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var bytes = System.Text.Encoding.UTF8.GetBytes(body);
        httpContext.Request.Body = new MemoryStream(bytes);
        httpContext.Request.ContentType = "application/json";

        if (!omitHeaders)
        {
            var (svixId, svixTimestamp, svixSignature) = SvixTestHelper.GenerateSvixHeaders(body, secret);
            httpContext.Request.Headers["svix-id"] = svixId;
            httpContext.Request.Headers["svix-timestamp"] = svixTimestamp;
            httpContext.Request.Headers["svix-signature"] = svixSignature;
        }

        return httpContext;
    }

    private async Task<IActionResult> ExecuteWebhookPipelineAsync(DefaultHttpContext httpContext)
    {
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var executingContext = new Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            _controller);

        IActionResult? finalResult = null;
        Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next = async () =>
        {
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            finalResult = await _controller.HandleWebhook();
            return new Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext(actionContext, [], _controller);
        };

        await _verificationFilter.OnActionExecutionAsync(executingContext, next);

        return executingContext.Result ?? finalResult ?? new StatusCodeResult(500);
    }

    [Fact]
    public async Task PostAcceptance_WhenInvitedUserAcceptsOnClerk_UserCreatedWebhookLinksAuthUserIdAndUpdatesNames()
    {
        // Arrange: Pre-existing invited user record in database
        var invitedUserId = Guid.NewGuid();
        const string invitedEmail = "regional.officer@domain.gov.ph";
        const string clerkAuthUserId = "user_clerk_accepted_987";
        var roleId = Guid.NewGuid();

        var existingInvitedUser = new UserDto
        {
            Id = invitedUserId,
            AuthUserId = string.Empty, // Empty AuthUserId for invited user
            Email = invitedEmail,
            FirstName = "",
            LastName = "",
            Role = new RoleDto { Id = roleId, Name = "RegionalStaff", Key = "regional_staff", Scope = "Regional" }
        };

        // 1. GetUserByAuthUserId returns not found initially
        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == clerkAuthUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = false, Message = "User not found." });

        // 2. GetAllUsersRequest(email) finds the invited user
        _mediatorMock.Setup(m => m.Send(
            It.Is<GetAllUsersRequest>(r => r.Email == invitedEmail),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<UserDto>([existingInvitedUser], 1, 1, 1));

        // 3. UpdateUserByIdRequest links AuthUserId and sets first/last name
        var updatedUserDto = new UserDto
        {
            Id = invitedUserId,
            AuthUserId = clerkAuthUserId,
            Email = invitedEmail,
            FirstName = "Maria",
            LastName = "Santos",
            Role = existingInvitedUser.Role
        };

        _mediatorMock.Setup(m => m.Send(
            It.Is<UpdateUserByIdRequest>(r =>
                r.Id == invitedUserId &&
                r.User.AuthUserId == clerkAuthUserId &&
                r.User.FirstName == "Maria" &&
                r.User.LastName == "Santos" &&
                r.User.Email == invitedEmail),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = updatedUserDto });

        var webhookBody = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = clerkAuthUserId,
                first_name = "Maria",
                last_name = "Santos",
                email_addresses = new[] { new { email_address = invitedEmail } }
            }
        });

        var httpContext = CreateHttpContextWithFilter(webhookBody);

        // Act
        var result = await ExecuteWebhookPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<UserDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Succeeded.Should().BeTrue();
        apiResponse.Data!.AuthUserId.Should().Be(clerkAuthUserId);
        apiResponse.Data.Email.Should().Be(invitedEmail);
        apiResponse.Data.FirstName.Should().Be("Maria");
        apiResponse.Data.LastName.Should().Be("Santos");

        // Verify that CreateUserRequest was NOT called (we linked existing invited user)
        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        // Verify UpdateUserByIdRequest was called once with the linked AuthUserId
        _mediatorMock.Verify(m => m.Send(It.Is<UpdateUserByIdRequest>(r => r.Id == invitedUserId && r.User.AuthUserId == clerkAuthUserId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAcceptance_WhenSessionCreatedFires_EnsuresUserAndDispatchesCreateSessionRequestToSyncClerkMetadata()
    {
        // Arrange
        const string clerkAuthUserId = "user_clerk_session_456";
        var existingUser = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = clerkAuthUserId,
            Email = "session.user@domain.gov.ph",
            FirstName = "Alex",
            LastName = "Cruz",
            Role = new RoleDto { Id = Guid.NewGuid(), Name = "RegionalStaff", Key = "regional_staff", Scope = "Regional" }
        };

        // User exists in DB
        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == clerkAuthUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = existingUser });

        // Session creation request succeeds
        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateSessionRequest>(r => r.AuthUserId == clerkAuthUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<bool> { Succeeded = true, Data = true, Message = "Session synchronized" });

        var webhookBody = JsonSerializer.Serialize(new
        {
            type = "session.created",
            data = new
            {
                id = "sess_abc123",
                user_id = clerkAuthUserId
            }
        });

        var httpContext = CreateHttpContextWithFilter(webhookBody);

        // Act
        var result = await ExecuteWebhookPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<UserDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Succeeded.Should().BeTrue();
        apiResponse.Data.Should().BeEquivalentTo(existingUser);

        // Verify session was synchronized
        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateSessionRequest>(r => r.AuthUserId == clerkAuthUserId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JITProvisioning_WhenUserCreatedWithPublicMetadata_CreatesUserWithAssignedRoleAndRegions()
    {
        // Arrange: User with publicMetadata specifying role and regional assignments
        const string clerkAuthUserId = "user_clerk_jit_789";
        const string email = "new.engineer@domain.gov.ph";
        var targetRoleId = Guid.NewGuid();
        var targetRegionId = Guid.NewGuid();
        var regionalRoleId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == clerkAuthUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = false });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetAllUsersRequest>(r => r.Email == email),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<UserDto>([], 1, 1, 0));

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = clerkAuthUserId,
            Email = email,
            FirstName = "Pedro",
            LastName = "Penduko",
            Role = new RoleDto { Id = targetRoleId, Name = "FieldEngineer", Key = "field_engineer", Scope = "Regional" }
        };

        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == clerkAuthUserId &&
                r.User.RoleId == targetRoleId &&
                r.User.Regions != null &&
                r.User.Regions.Count == 1 &&
                r.User.Regions[0].RegionId == targetRegionId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = createdUserDto });

        var webhookBody = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = clerkAuthUserId,
                first_name = "Pedro",
                last_name = "Penduko",
                email_addresses = new[] { new { email_address = email } },
                public_metadata = new
                {
                    role_id = targetRoleId.ToString(),
                    regions = new[]
                    {
                        new
                        {
                            region_id = targetRegionId.ToString(),
                            role_id = regionalRoleId.ToString(),
                            status = "Active"
                        }
                    }
                }
            }
        });

        var httpContext = CreateHttpContextWithFilter(webhookBody);

        // Act
        var result = await ExecuteWebhookPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<UserDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Succeeded.Should().BeTrue();
        apiResponse.Data!.Role!.Id.Should().Be(targetRoleId);

        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == clerkAuthUserId &&
                r.User.RoleId == targetRoleId &&
                r.User.Regions!.Any(reg => reg.RegionId == targetRegionId && reg.RoleId == regionalRoleId)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserUpdated_WhenUserExists_UpdatesUserNamesAndEmail()
    {
        // Arrange
        const string clerkAuthUserId = "user_clerk_existing_update";
        var existingUserId = Guid.NewGuid();

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == clerkAuthUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Data = new UserDto
                {
                    Id = existingUserId,
                    AuthUserId = clerkAuthUserId,
                    FirstName = "OldFirst",
                    LastName = "OldLast",
                    Email = "old@domain.gov.ph",
                    Role = new RoleDto { Id = Guid.NewGuid(), Name = "RegionalStaff", Key = "regional_staff", Scope = "Regional" }
                }
            });

        _mediatorMock.Setup(m => m.Send(
            It.Is<UpdateUserByIdRequest>(r =>
                r.Id == existingUserId &&
                r.User.FirstName == "NewFirst" &&
                r.User.LastName == "NewLast" &&
                r.User.Email == "new@domain.gov.ph"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Data = new UserDto
                {
                    Id = existingUserId,
                    AuthUserId = clerkAuthUserId,
                    FirstName = "NewFirst",
                    LastName = "NewLast",
                    Email = "new@domain.gov.ph",
                    Role = new RoleDto { Id = Guid.NewGuid(), Name = "RegionalStaff", Key = "regional_staff", Scope = "Regional" }
                }
            });

        var webhookBody = JsonSerializer.Serialize(new
        {
            type = "user.updated",
            data = new
            {
                id = clerkAuthUserId,
                first_name = "NewFirst",
                last_name = "NewLast",
                email_addresses = new[] { new { email_address = "new@domain.gov.ph" } }
            }
        });

        var httpContext = CreateHttpContextWithFilter(webhookBody);

        // Act
        var result = await ExecuteWebhookPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(
            It.Is<UpdateUserByIdRequest>(r => r.Id == existingUserId && r.User.FirstName == "NewFirst"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("session.ended")]
    [InlineData("session.removed")]
    public async Task SessionLifecycleEvents_WhenTriggered_HandledCleanlyAndReturnOk(string eventType)
    {
        // Arrange
        const string clerkAuthUserId = "user_clerk_session_lifecycle";

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == clerkAuthUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto>
            {
                Succeeded = true,
                Data = new UserDto
                {
                    Id = Guid.NewGuid(),
                    AuthUserId = clerkAuthUserId,
                    Email = "lifecycle@domain.gov.ph",
                    Role = null
                }
            });

        var webhookBody = JsonSerializer.Serialize(new
        {
            type = eventType,
            data = new
            {
                id = "sess_terminated",
                user_id = clerkAuthUserId
            }
        });

        var httpContext = CreateHttpContextWithFilter(webhookBody);

        // Act
        var result = await ExecuteWebhookPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<UserDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UnhandledEventType_ReturnsOkWithInformativeMessage()
    {
        // Arrange
        var webhookBody = JsonSerializer.Serialize(new
        {
            type = "organization.created",
            data = new { id = "org_123" }
        });

        var httpContext = CreateHttpContextWithFilter(webhookBody);

        // Act
        var result = await ExecuteWebhookPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var apiResponse = okResult.Value as ApiResponse<UserDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Succeeded.Should().BeTrue();
        apiResponse.Message.Should().Contain("No action required for event type 'organization.created'");
    }

    [Fact]
    public async Task SecurityVerification_WhenSvixSignatureIsInvalid_ReturnsUnauthorizedAndRejectsProcessing()
    {
        // Arrange: Valid payload with tampered signature
        var webhookBody = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new { id = "user_clerk_malicious" }
        });

        var httpContext = CreateHttpContextWithFilter(webhookBody);
        httpContext.Request.Headers["svix-signature"] = "v1,tampered_signature_value==";

        // Act
        var result = await ExecuteWebhookPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
        // Verify no DB / mediator actions took place
        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserByIdRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
