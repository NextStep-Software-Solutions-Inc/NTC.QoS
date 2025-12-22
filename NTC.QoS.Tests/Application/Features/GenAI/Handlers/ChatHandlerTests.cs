using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NTC.QoS.Application.Features.GenAI.Handlers.Commands;
using NTC.QoS.Application.Features.GenAI.Requests;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs.GenAI;
using System.Linq.Expressions;
using System.Text;
using Xunit;

namespace NTC.QoS.Tests.Application.Features.GenAI.Handlers;

public class ChatHandlerTests
{
    private readonly Mock<IGenAIService> _genAIServiceMock;
    private readonly Mock<IGeneratedReportRepository> _reportRepositoryMock;
    private readonly Mock<IUserContextService> _userContextServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAIInstructionService> _aiInstructionServiceMock;
    private readonly Mock<ILogger<ChatHandler>> _loggerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly ChatHandler _handler;

    public ChatHandlerTests()
    {
        _genAIServiceMock = new Mock<IGenAIService>();
        _reportRepositoryMock = new Mock<IGeneratedReportRepository>();
        _userContextServiceMock = new Mock<IUserContextService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _aiInstructionServiceMock = new Mock<IAIInstructionService>();
        _loggerMock = new Mock<ILogger<ChatHandler>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Setup default global instruction
        _aiInstructionServiceMock.Setup(x => x.GetGlobalInstruction())
            .Returns("You are an AI assistant for CMTS and Broadband Mapping.");

        _handler = new ChatHandler(
            _genAIServiceMock.Object,
            _reportRepositoryMock.Object,
            _userContextServiceMock.Object,
            _userRepositoryMock.Object,
            _aiInstructionServiceMock.Object,
            _memoryCache,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_StreamsResponseSuccessfully()
    {
        // Arrange
        var prompt = "What is the coverage in Region 10?";
        var chatRequestDto = new ChatRequestDto { Prompt = prompt };
        
        var httpResponse = CreateMockHttpResponse();
        var request = new ChatRequest(chatRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var similarReports = new List<string> { "Report 1 content", "Report 2 content" };
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(similarReports);
        _genAIServiceMock.Setup(x => x.ChatStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable("Hello", " World"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _genAIServiceMock.Verify(x => x.GetEmbeddingAsync(prompt), Times.Once);
        _reportRepositoryMock.Verify(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAuthenticatedUser_MaintainsSessionHistory()
    {
        // Arrange
        var authUserId = "auth-user-123";
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, AuthUserId = authUserId };
        
        var prompt = "Test prompt";
        var chatRequestDto = new ChatRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new ChatRequest(chatRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(authUserId);
        _userRepositoryMock.Setup(x => x.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        _genAIServiceMock.Setup(x => x.ChatStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable("Response"));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        var cachedHistory = _memoryCache.Get<List<ChatMessage>>(userId.ToString());
        cachedHistory.Should().NotBeNull();
        cachedHistory.Should().HaveCount(2);
        cachedHistory![0].Role.Should().Be("user");
        cachedHistory[0].Content.Should().Be(prompt);
        cachedHistory[1].Role.Should().Be("assistant");
    }

    [Fact]
    public async Task Handle_WhenRAGContextFails_ContinuesWithoutContext()
    {
        // Arrange
        var prompt = "Test prompt";
        var chatRequestDto = new ChatRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new ChatRequest(chatRequestDto, httpResponse);
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt))
            .ThrowsAsync(new Exception("Embedding service unavailable"));
        _genAIServiceMock.Setup(x => x.ChatStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable("Response without context"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _genAIServiceMock.Verify(x => x.ChatStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRelevantReports_IncludesContextInPrompt()
    {
        // Arrange
        var prompt = "Test prompt";
        var chatRequestDto = new ChatRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new ChatRequest(chatRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var similarReports = new List<string> { "Report content 1", "Report content 2" };
        string? capturedPrompt = null;
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(similarReports);
        _genAIServiceMock.Setup(x => x.ChatStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((p, _, _) => capturedPrompt = p)
            .Returns(AsyncEnumerable("Response"));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        capturedPrompt.Should().NotBeNull();
        capturedPrompt.Should().Contain("CONTEXT FROM PREVIOUS REPORTS:");
        capturedPrompt.Should().Contain("Report content 1");
        capturedPrompt.Should().Contain("Report content 2");
        capturedPrompt.Should().Contain("USER:");
        capturedPrompt.Should().Contain(prompt);
    }

    [Fact]
    public async Task Handle_WithNoRelevantReports_DoesNotIncludeContext()
    {
        // Arrange
        var prompt = "Test prompt";
        var chatRequestDto = new ChatRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new ChatRequest(chatRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        string? capturedPrompt = null;
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        _genAIServiceMock.Setup(x => x.ChatStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((p, _, _) => capturedPrompt = p)
            .Returns(AsyncEnumerable("Response"));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        capturedPrompt.Should().NotBeNull();
        capturedPrompt.Should().NotContain("CONTEXT FROM PREVIOUS REPORTS:");
        capturedPrompt.Should().Contain("USER:");
        capturedPrompt.Should().Contain(prompt);
    }

    private static HttpResponse CreateMockHttpResponse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext.Response;
    }

    private static async IAsyncEnumerable<string> AsyncEnumerable(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
