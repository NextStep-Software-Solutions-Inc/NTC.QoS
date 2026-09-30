using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.API.Controllers;
using NTC.QoS.API.Extensions;
using NTC.QoS.API.Filters;
using NTC.QoS.Application.Features.User.Handlers.Commands;
using NTC.QoS.Application.Features.User.Requests;
using NTC.QoS.Application.Services;
using NTC.QoS.Domain.Authorization;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Role;
using NTC.QoS.Domain.Models.DTOs.User;
using NTC.QoS.Tests.Helpers;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.Users;

/// <summary>
/// End-to-end integration tests verifying the full Clerk webhook lifecycle:
/// 1. Admin invitation creation with role & regional jurisdiction
/// 2. Post-acceptance webhook delivery with Svix cryptographic signature verification
/// 3. Just-In-Time (JIT) database user provisioning and regional role assignment
/// 4. Session synchronization & metadata propagation to Clerk
/// 5. Security tampering rejection and external API failure resilience
/// </summary>
public class ClerkWebhookLifecycleIntegrationTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IClerkUserService> _clerkUserServiceMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IRepository<Role>> _roleRepositoryMock = new();
    private readonly Mock<IRepository<Region>> _regionRepositoryMock = new();
    private readonly Mock<IUserAuthorizationService> _authorizationServiceMock = new();
    private readonly Mock<ILogger<UserProvisioningService>> _provisioningLoggerMock = new();
    private readonly Mock<ILogger<InviteUserHandler>> _inviteLoggerMock = new();
    private readonly Mock<ILogger<CreateSessionHandler>> _sessionLoggerMock = new();
    private readonly Mock<ILogger<ClerkWebhookVerificationAttribute>> _webhookFilterLoggerMock = new();

    private readonly IConfiguration _configuration;
    private readonly UserProvisioningService _userProvisioningService;
    private readonly WebhookController _webhookController;
    private readonly ClerkWebhookVerificationAttribute _verificationFilter = new();

    public ClerkWebhookLifecycleIntegrationTests()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Clerk:WebhookSecret", SvixTestHelper.TestSecret },
            { "Clerk:FrontendUrl", "http://localhost:3000/select-region" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _userProvisioningService = new UserProvisioningService(
            _mediatorMock.Object,
            _clerkUserServiceMock.Object,
            _provisioningLoggerMock.Object);

        _webhookController = new WebhookController(_mediatorMock.Object, _userProvisioningService);
    }

    private DefaultHttpContext CreateHttpContext(string body, string? signature = null, bool omitHeaders = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_configuration);
        services.AddSingleton(_webhookFilterLoggerMock.Object);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var bytes = Encoding.UTF8.GetBytes(body);
        httpContext.Request.Body = new MemoryStream(bytes);
        httpContext.Request.ContentType = "application/json";

        if (!omitHeaders)
        {
            var (svixId, svixTimestamp, validSignature) = SvixTestHelper.GenerateSvixHeaders(body);
            httpContext.Request.Headers["svix-id"] = svixId;
            httpContext.Request.Headers["svix-timestamp"] = svixTimestamp;
            httpContext.Request.Headers["svix-signature"] = signature ?? validSignature;
        }

        return httpContext;
    }

    private async Task<IActionResult> DispatchWebhookThroughPipelineAsync(DefaultHttpContext httpContext)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            _webhookController);

        IActionResult? executedResult = null;
        ActionExecutionDelegate next = async () =>
        {
            _webhookController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            executedResult = await _webhookController.HandleWebhook();
            return new ActionExecutedContext(actionContext, [], _webhookController);
        };

        await _verificationFilter.OnActionExecutionAsync(executingContext, next);
        return executingContext.Result ?? executedResult ?? new StatusCodeResult(500);
    }

    [Fact]
    public async Task FullLifecycle_FromInvitationGenerationToJITProvisioning_SucceedsEndToEnd()
    {
        // -------------------------------------------------------------
        // STEP 1: Admin creates invitation for a new Regional Personnel
        // -------------------------------------------------------------
        var targetRoleId = Guid.NewGuid();
        var targetRegionId = Guid.NewGuid();
        const string candidateEmail = "field.officer@domain.gov.ph";
        const string generatedClerkInviteId = "inv_clerk_prod_101";

        var regionalRole = new Role
        {
            Id = targetRoleId,
            Key = Roles.FieldEngineer,
            Name = "Field Engineer",
            Scope = Roles.ScopeRegional
        };

        var targetRegion = new Region
        {
            Id = targetRegionId,
            Name = "Region VII - Central Visayas",
            Code = "R07",
            IsDeleted = false
        };

        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _roleRepositoryMock.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>()))
            .Returns(new List<Role> { regionalRole }.AsQueryable());
        _roleRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<Role>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(regionalRole);
        _regionRepositoryMock.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<Region, bool>>>()))
            .Returns(new List<Region> { targetRegion }.AsQueryable());
        _regionRepositoryMock.Setup(r => r.ToListAsync(It.IsAny<IQueryable<Region>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Region> { targetRegion });

        _authorizationServiceMock.Setup(a => a.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserDto
            {
                Id = Guid.NewGuid(),
                AuthUserId = "user_caller_admin",
                Email = "admin@domain.gov.ph",
                Role = new RoleDto { Id = Guid.NewGuid(), Key = Roles.SuperAdmin, Name = "Super Admin", Scope = Roles.ScopePlatform }
            });

        CreateClerkInvitationPayload? capturedInvitePayload = null;
        _clerkUserServiceMock.Setup(c => c.CreateInvitationAsync(It.IsAny<CreateClerkInvitationPayload>()))
            .Callback<CreateClerkInvitationPayload>(p => capturedInvitePayload = p)
            .ReturnsAsync(new ClerkInvitationResponse
            {
                Id = generatedClerkInviteId,
                EmailAddress = candidateEmail,
                Status = "pending"
            });

        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync("pending"))
            .ReturnsAsync(new List<ClerkInvitationResponse>());

        var inviteHandler = new InviteUserHandler(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _regionRepositoryMock.Object,
            _clerkUserServiceMock.Object,
            _configuration,
            _authorizationServiceMock.Object,
            _inviteLoggerMock.Object);

        var inviteRequest = new InviteUserRequest(new InviteUserDto
        {
            Email = candidateEmail,
            RoleId = targetRoleId,
            Regions =
            [
                new InviteUserRegionDto
                {
                    RegionId = targetRegionId,
                    RoleId = targetRoleId,
                    Status = "Active"
                }
            ]
        });

        var inviteResponse = await inviteHandler.Handle(inviteRequest, CancellationToken.None);

        // Verify Step 1: Clerk invitation created with embedded metadata
        inviteResponse.Succeeded.Should().BeTrue();
        capturedInvitePayload.Should().NotBeNull();
        capturedInvitePayload!.EmailAddress.Should().Be(candidateEmail);
        capturedInvitePayload.IgnoreExisting.Should().BeTrue();

        // -------------------------------------------------------------
        // STEP 2: Candidate accepts invite -> Clerk sends user.created
        // -------------------------------------------------------------
        const string candidateClerkAuthId = "user_clerk_accepted_101";

        var webhookBody = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = candidateClerkAuthId,
                first_name = "Juan",
                last_name = "Dela Cruz",
                email_addresses = new[] { new { email_address = candidateEmail } },
                public_metadata = new
                {
                    role_id = targetRoleId.ToString(),
                    regions = new[]
                    {
                        new
                        {
                            region_id = targetRegionId.ToString(),
                            role_id = targetRoleId.ToString(),
                            status = "Active"
                        }
                    }
                }
            }
        });

        // Set up Mediator expectations for JIT user provisioning
        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == candidateClerkAuthId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = false });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetAllUsersRequest>(r => r.Email == candidateEmail),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<UserDto>([], 1, 1, 0));

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = candidateClerkAuthId,
            Email = candidateEmail,
            FirstName = "Juan",
            LastName = "Dela Cruz",
            Role = new RoleDto { Id = targetRoleId, Key = Roles.FieldEngineer, Name = "Field Engineer" }
        };

        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == candidateClerkAuthId &&
                r.User.RoleId == targetRoleId &&
                r.User.Regions != null &&
                r.User.Regions.Count == 1 &&
                r.User.Regions[0].RegionId == targetRegionId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = createdUserDto });

        // Dispatch through Svix cryptographic filter into WebhookController
        var httpContext = CreateHttpContext(webhookBody);
        var webhookResult = await DispatchWebhookThroughPipelineAsync(httpContext);

        // Verify Step 2: Webhook signature verified and JIT user created
        webhookResult.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)webhookResult;
        var apiResponse = okResult.Value as ApiResponse<UserDto>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Succeeded.Should().BeTrue();
        apiResponse.Data!.AuthUserId.Should().Be(candidateClerkAuthId);
        apiResponse.Data.Role!.Key.Should().Be(Roles.FieldEngineer);

        // -------------------------------------------------------------
        // STEP 3: User logs in -> Clerk sends session.created webhook
        // -------------------------------------------------------------
        var sessionWebhookBody = JsonSerializer.Serialize(new
        {
            type = "session.created",
            data = new
            {
                id = "sess_prod_202",
                user_id = candidateClerkAuthId
            }
        });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == candidateClerkAuthId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = createdUserDto });

        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateSessionRequest>(r => r.AuthUserId == candidateClerkAuthId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<bool> { Succeeded = true, Data = true, Message = "Session synchronized" });

        var sessionHttpContext = CreateHttpContext(sessionWebhookBody);
        var sessionResult = await DispatchWebhookThroughPipelineAsync(sessionHttpContext);

        // Verify Step 3: Session synchronized and permissions updated
        sessionResult.Should().BeOfType<OkObjectResult>();
        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateSessionRequest>(r => r.AuthUserId == candidateClerkAuthId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Security_WhenWebhookPayloadIsForgedOrTampered_FilterRejectsWith401Unauthorized()
    {
        // Arrange: Valid payload signed with invalid signature
        var payload = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = "user_malicious_attacker",
                email_addresses = new[] { new { email_address = "hacker@evil.com" } }
            }
        });

        var httpContext = CreateHttpContext(payload, signature: "v1,tampered_unauthorized_hmac==");

        // Act
        var result = await DispatchWebhookThroughPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("Invalid signature");

        // Verify zero database or provisioning operations were initiated
        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserByIdRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Security_WhenRequiredSvixHeadersAreMissing_FilterRejectsWith401Unauthorized()
    {
        // Arrange
        var payload = JsonSerializer.Serialize(new { type = "user.created" });
        var httpContext = CreateHttpContext(payload, omitHeaders: true);

        // Act
        var result = await DispatchWebhookThroughPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("Invalid signature");

        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Security_WhenSvixTimestampIsExpired_FilterRejectsWith401Unauthorized()
    {
        // Arrange - Svix enforces a 5-minute replay tolerance window. 10 minutes in the past is expired.
        var expiredTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10);
        var payload = JsonSerializer.Serialize(new { type = "user.created" });
        var (svixId, svixTimestamp, svixSignature) = SvixTestHelper.GenerateSvixHeaders(payload, timestamp: expiredTimestamp);

        var httpContext = CreateHttpContext(payload, omitHeaders: true);
        httpContext.Request.Headers["svix-id"] = svixId;
        httpContext.Request.Headers["svix-timestamp"] = svixTimestamp;
        httpContext.Request.Headers["svix-signature"] = svixSignature;

        // Act
        var result = await DispatchWebhookThroughPipelineAsync(httpContext);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("Invalid signature");

        _mediatorMock.Verify(m => m.Send(It.IsAny<CreateUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Resilience_WhenClerkMetadataSyncThrows_SessionHandlerCatchesAndLogsWithoutBreaking()
    {
        // Arrange
        const string authUserId = "user_clerk_resilience_test";
        var user = new User
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = "resilience@ntc.gov.ph",
            Role = new Role { Id = Guid.NewGuid(), Key = Roles.RegionalStaff, Name = "Regional Staff", Permissions = ["read:stats"] },
            IsDeleted = false
        };

        _userRepositoryMock.Setup(r => r.Query(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Clerk API throws network failure
        _clerkUserServiceMock.Setup(c => c.UpdateUserMetadataAsync(authUserId, It.IsAny<UserMetadataPayload>()))
            .ThrowsAsync(new HttpRequestException("Clerk API connection timed out."));

        var sessionHandler = new CreateSessionHandler(
            _userRepositoryMock.Object,
            _clerkUserServiceMock.Object,
            _sessionLoggerMock.Object);

        // Act
        var result = await sessionHandler.Handle(new CreateSessionRequest { AuthUserId = authUserId }, CancellationToken.None);

        // Assert: Failure handled gracefully and reported without uncaught exception
        result.Succeeded.Should().BeFalse();
        result.Data.Should().BeFalse();
        result.Message.Should().Contain("Clerk API connection timed out.");
    }

    [Fact]
    public async Task WebhookAcceptance_WhenPayloadContainsFullClerkRealWorldSchemaWithExtraAttributes_ParsesSuccessfully()
    {
        // Real-world Clerk payload format with 25+ extra fields
        const string authUserId = "user_clerk_full_schema_999";
        const string email = "full.schema@ntc.gov.ph";
        var roleId = Guid.NewGuid();
        var regionId = Guid.NewGuid();

        var realWorldPayload = JsonSerializer.Serialize(new
        {
            @object = "event",
            type = "user.created",
            data = new
            {
                backup_codes = (object?)null,
                banned = false,
                create_organization_enabled = true,
                created_at = 1714000000000,
                delete_self_enabled = true,
                email_addresses = new[]
                {
                    new
                    {
                        created_at = 1714000000000,
                        email_address = email,
                        id = "idn_email_123",
                        linked_to = Array.Empty<object>(),
                        @object = "email_address",
                        verification = new { status = "verified", strategy = "from_oauth_google" }
                    }
                },
                first_name = "Real",
                has_image = true,
                id = authUserId,
                image_url = "https://img.clerk.com/preview.png",
                last_name = "World",
                last_sign_in_at = 1714000000000,
                locked = false,
                lockout_expires_in_seconds = (object?)null,
                mfa_disabled_at = (object?)null,
                mfa_enabled_at = (object?)null,
                @object = "user",
                passkeys = Array.Empty<object>(),
                password_enabled = false,
                phone_numbers = Array.Empty<object>(),
                primary_email_address_id = "idn_email_123",
                primary_phone_number_id = (object?)null,
                primary_web3_wallet_id = (object?)null,
                private_metadata = new { },
                profile_image_url = "https://images.clerk.dev/oauth_google/avatar.png",
                public_metadata = new
                {
                    role_id = roleId.ToString(),
                    regions = new[]
                    {
                        new
                        {
                            region_id = regionId.ToString(),
                            role_id = roleId.ToString(),
                            status = "Active"
                        }
                    }
                },
                two_factor_enabled = false,
                unsafe_metadata = new { },
                updated_at = 1714000000000,
                username = (object?)null,
                web3_wallets = Array.Empty<object>()
            },
            event_attributes = new { http_request = new { client_ip = "127.0.0.1", user_agent = "Clerk-Webhook/v1" } }
        });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = false });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetAllUsersRequest>(r => r.Email == email),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<UserDto>([], 1, 1, 0));

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = email,
            FirstName = "Real",
            LastName = "World",
            Role = new RoleDto { Id = roleId, Key = Roles.FieldEngineer, Name = "Field Engineer" }
        };

        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == authUserId &&
                r.User.RoleId == roleId &&
                r.User.Regions != null &&
                r.User.Regions.Any(reg => reg.RegionId == regionId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = createdUserDto });

        var httpContext = CreateHttpContext(realWorldPayload);
        var result = await DispatchWebhookThroughPipelineAsync(httpContext);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ApiResponse<UserDto>;
        response.Should().NotBeNull();
        response!.Succeeded.Should().BeTrue();
        response.Data!.AuthUserId.Should().Be(authUserId);
    }

    [Fact]
    public async Task WebhookAcceptance_WhenPublicMetadataIsStringifiedJson_RecoversRoleAndRegionsSafely()
    {
        // Edge case: Clerk metadata stored as serialized JSON string (common from direct dashboard edits)
        const string authUserId = "user_clerk_stringified_meta";
        const string email = "stringified@ntc.gov.ph";
        var roleId = Guid.NewGuid();
        var regionId = Guid.NewGuid();

        var stringifiedMetadata = JsonSerializer.Serialize(new
        {
            role_id = roleId.ToString(),
            regions = new[]
            {
                new { region_id = regionId.ToString(), role_id = roleId.ToString(), status = "Active" }
            }
        });

        var payload = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = authUserId,
                first_name = "Stringified",
                last_name = "Metadata",
                email_addresses = new[] { new { email_address = email } },
                public_metadata = stringifiedMetadata
            }
        });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = false });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetAllUsersRequest>(r => r.Email == email),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<UserDto>([], 1, 1, 0));

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = email,
            FirstName = "Stringified",
            LastName = "Metadata",
            Role = new RoleDto { Id = roleId, Key = Roles.RegionalStaff, Name = "Regional Staff" }
        };

        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == authUserId &&
                r.User.RoleId == roleId &&
                r.User.Regions != null &&
                r.User.Regions.Any(reg => reg.RegionId == regionId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = createdUserDto });

        var httpContext = CreateHttpContext(payload);
        var result = await DispatchWebhookThroughPipelineAsync(httpContext);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ApiResponse<UserDto>;
        response.Should().NotBeNull();
        response!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookAcceptance_WhenNamesAreNullAndUserHasMultipleEmails_ExtractsPrimaryEmailAndHandlesNullNames()
    {
        // Edge case: Null names and multiple emails
        const string authUserId = "user_clerk_null_names";
        const string primaryEmail = "first.email@ntc.gov.ph";
        var roleId = Guid.NewGuid();

        var payload = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = authUserId,
                first_name = (string?)null,
                last_name = (string?)null,
                email_addresses = new[]
                {
                    new { email_address = primaryEmail },
                    new { email_address = "secondary@ntc.gov.ph" }
                },
                public_metadata = new
                {
                    role_id = roleId.ToString()
                }
            }
        });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = false });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetAllUsersRequest>(r => r.Email == primaryEmail),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<UserDto>([], 1, 1, 0));

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = primaryEmail,
            FirstName = "",
            LastName = "",
            Role = new RoleDto { Id = roleId, Key = Roles.User, Name = "User" }
        };

        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == authUserId &&
                r.User.FirstName == "" &&
                r.User.LastName == "" &&
                r.User.Email == primaryEmail),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = createdUserDto });

        var httpContext = CreateHttpContext(payload);
        var result = await DispatchWebhookThroughPipelineAsync(httpContext);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ApiResponse<UserDto>;
        response.Should().NotBeNull();
        response!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookAcceptance_WhenPublicMetadataMissingOrEmpty_RecoversFromInvitationFallback()
    {
        // Edge case: public_metadata is empty {} (Clerk user signup directly)
        const string authUserId = "user_clerk_empty_meta";
        const string email = "recover.me@ntc.gov.ph";
        var recoveredRoleId = Guid.NewGuid();
        var recoveredRegionId = Guid.NewGuid();

        var payload = JsonSerializer.Serialize(new
        {
            type = "user.created",
            data = new
            {
                id = authUserId,
                first_name = "Recovered",
                last_name = "User",
                email_addresses = new[] { new { email_address = email } },
                public_metadata = new { }
            }
        });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetUserByAuthUserIdRequest>(r => r.AuthUserId == authUserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = false });

        _mediatorMock.Setup(m => m.Send(
            It.Is<GetAllUsersRequest>(r => r.Email == email),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<UserDto>([], 1, 1, 0));

        // Setup ClerkUserService to return prior invitation containing the metadata
        var invitationMetadataDoc = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            role_id = recoveredRoleId.ToString(),
            regions = new[]
            {
                new { region_id = recoveredRegionId.ToString(), role_id = recoveredRoleId.ToString(), status = "Active" }
            }
        }));

        var fallbackInvite = new ClerkInvitationResponse
        {
            Id = "inv_recovery_fallback",
            EmailAddress = email,
            Status = "pending",
            CreatedAt = 1714000000000,
            PublicMetadata = invitationMetadataDoc.RootElement
        };

        _clerkUserServiceMock.Setup(c => c.GetInvitationsAsync(null))
            .ReturnsAsync(new List<ClerkInvitationResponse> { fallbackInvite });

        var createdUserDto = new UserDto
        {
            Id = Guid.NewGuid(),
            AuthUserId = authUserId,
            Email = email,
            FirstName = "Recovered",
            LastName = "User",
            Role = new RoleDto { Id = recoveredRoleId, Key = Roles.FieldEngineer, Name = "Field Engineer" }
        };

        _mediatorMock.Setup(m => m.Send(
            It.Is<CreateUserRequest>(r =>
                r.User.AuthUserId == authUserId &&
                r.User.RoleId == recoveredRoleId &&
                r.User.Regions != null &&
                r.User.Regions.Any(reg => reg.RegionId == recoveredRegionId)),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Succeeded = true, Data = createdUserDto });

        var httpContext = CreateHttpContext(payload);
        var result = await DispatchWebhookThroughPipelineAsync(httpContext);

        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var response = okResult.Value as ApiResponse<UserDto>;
        response.Should().NotBeNull();
        response!.Succeeded.Should().BeTrue();

        // Verify that recovered metadata was synchronized to the Clerk user profile
        _clerkUserServiceMock.Verify(c => c.UpdateUserMetadataAsync(authUserId, It.IsAny<UserMetadataPayload>()), Times.Once);
    }

    [Theory]
    [InlineData(null, "whsec_test_valid_placeholder_secret", "https://clerk.accounts.dev", "http://localhost:3000", "Clerk:SecretKey is required", "Development")]
    [InlineData("invalid_key_no_sk", "whsec_test_valid_placeholder_secret", "https://clerk.accounts.dev", "http://localhost:3000", "Clerk:SecretKey is malformed", "Development")]
    [InlineData("sk_test_123", null, "https://clerk.accounts.dev", "http://localhost:3000", "Clerk:WebhookSecret is required", "Production")]
    [InlineData("sk_test_123", "invalid_no_whsec", "https://clerk.accounts.dev", "http://localhost:3000", "Clerk:WebhookSecret is malformed", "Development")]
    [InlineData("sk_test_123", "whsec_test_valid_placeholder_secret", null, "http://localhost:3000", "Clerk:Authority is required", "Development")]
    [InlineData("sk_test_123", "whsec_test_valid_placeholder_secret", "invalid_authority_uri", "http://localhost:3000", "not a valid absolute HTTP/HTTPS URI", "Development")]
    [InlineData("sk_test_123", "whsec_test_valid_placeholder_secret", "https://clerk.accounts.dev", null, "Neither 'Clerk:FrontendUrl' nor 'Clerk:AuthorizedParties' is configured", "Development")]
    public void StartupValidation_WhenConfigurationIsMissingOrMalformed_FailsFastWithDetailedException(
        string? secretKey, string? webhookSecret, string? authority, string? frontendUrl, string expectedErrorMessage, string environmentName)
    {
        // Arrange
        var inMemory = new Dictionary<string, string?>();
        if (secretKey != null) inMemory["Clerk:SecretKey"] = secretKey;
        if (webhookSecret != null) inMemory["Clerk:WebhookSecret"] = webhookSecret;
        if (authority != null) inMemory["Clerk:Authority"] = authority;
        if (frontendUrl != null) inMemory["Clerk:FrontendUrl"] = frontendUrl;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var envMock = new Mock<IHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(environmentName);

        // Act
        var act = () => configuration.ValidateClerkConfiguration(envMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{expectedErrorMessage}*");
    }

    [Fact]
    public void StartupValidation_WhenAllConfigurationsAreValid_PassesWithoutThrowing()
    {
        // Arrange
        var inMemory = new Dictionary<string, string?>
        {
            { "Clerk:SecretKey", "sk_test_validSecretKey123" },
            { "Clerk:WebhookSecret", "whsec_test_valid_placeholder_secret" },
            { "Clerk:Authority", "https://clerk.accounts.dev" },
            { "Clerk:FrontendUrl", "http://localhost:3000" }
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();
        var envMock = new Mock<IHostEnvironment>();
        envMock.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        // Act & Assert
        var act = () => configuration.ValidateClerkConfiguration(envMock.Object);
        act.Should().NotThrow();
    }
}
