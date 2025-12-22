using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
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

public class GenerateReportHandlerTests
{
    private readonly Mock<IGenAIService> _genAIServiceMock;
    private readonly Mock<IGeneratedReportRepository> _reportRepositoryMock;
    private readonly Mock<IUserContextService> _userContextServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<GenerateReportHandler>> _loggerMock;
    private readonly GenerateReportHandler _handler;

    public GenerateReportHandlerTests()
    {
        _genAIServiceMock = new Mock<IGenAIService>();
        _reportRepositoryMock = new Mock<IGeneratedReportRepository>();
        _userContextServiceMock = new Mock<IUserContextService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<GenerateReportHandler>>();

        _handler = new GenerateReportHandler(
            _genAIServiceMock.Object,
            _reportRepositoryMock.Object,
            _userContextServiceMock.Object,
            _userRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_StreamsReportAndSavesWithEmbedding()
    {
        // Arrange
        var authUserId = "auth-user-123";
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, AuthUserId = authUserId };
        
        var prompt = "Generate a coverage report for Region 10";
        var reportRequestDto = new GenerateReportRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new GenerateReportRequest(reportRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var reportContent = "# Coverage Report\n\nThis is the report content.";
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(authUserId);
        _userRepositoryMock.Setup(x => x.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        _genAIServiceMock.Setup(x => x.GenerateReportStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(reportContent));
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(reportContent)).ReturnsAsync(embedding);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _reportRepositoryMock.Verify(x => x.AddReportWithEmbeddingAsync(
            It.Is<GeneratedReport>(r => r.UserId == userId && r.Prompt == prompt),
            embedding,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithRAGContext_IncludesContextInPrompt()
    {
        // Arrange
        var prompt = "Generate a report";
        var reportRequestDto = new GenerateReportRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new GenerateReportRequest(reportRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var similarReports = new List<string> { "Previous report content 1", "Previous report content 2" };
        string? capturedPrompt = null;
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(similarReports);
        _genAIServiceMock.Setup(x => x.GenerateReportStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((p, _, _) => capturedPrompt = p)
            .Returns(AsyncEnumerable("Report content"));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        capturedPrompt.Should().NotBeNull();
        capturedPrompt.Should().Contain("CONTEXT FROM PREVIOUS REPORTS:");
        capturedPrompt.Should().Contain("Previous report content 1");
        capturedPrompt.Should().Contain("Previous report content 2");
        capturedPrompt.Should().Contain("USER REQUEST:");
        capturedPrompt.Should().Contain(prompt);
    }

    [Fact]
    public async Task Handle_WithNoUser_DoesNotSaveReport()
    {
        // Arrange
        var prompt = "Generate a report";
        var reportRequestDto = new GenerateReportRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new GenerateReportRequest(reportRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        _genAIServiceMock.Setup(x => x.GenerateReportStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable("Report content"));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _reportRepositoryMock.Verify(x => x.AddReportWithEmbeddingAsync(
            It.IsAny<GeneratedReport>(),
            It.IsAny<float[]?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmbeddingFails_SavesReportWithoutEmbedding()
    {
        // Arrange
        var authUserId = "auth-user-123";
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, AuthUserId = authUserId };
        
        var prompt = "Generate a report";
        var reportRequestDto = new GenerateReportRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new GenerateReportRequest(reportRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var reportContent = "Report content";
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(authUserId);
        _userRepositoryMock.Setup(x => x.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        _genAIServiceMock.Setup(x => x.GenerateReportStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(reportContent));
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(reportContent))
            .ThrowsAsync(new Exception("Embedding service unavailable"));

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        _reportRepositoryMock.Verify(x => x.AddReportWithEmbeddingAsync(
            It.Is<GeneratedReport>(r => r.UserId == userId),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExtractsTitleFromMarkdownHeading()
    {
        // Arrange
        var authUserId = "auth-user-123";
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, AuthUserId = authUserId };
        
        var prompt = "Generate a report";
        var reportRequestDto = new GenerateReportRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new GenerateReportRequest(reportRequestDto, httpResponse);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var reportContent = "# My Report Title\n\nReport body content.";
        GeneratedReport? capturedReport = null;
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns(authUserId);
        _userRepositoryMock.Setup(x => x.Query(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(new List<User> { user }.AsQueryable());
        _userRepositoryMock.Setup(x => x.FirstOrDefaultAsync(It.IsAny<IQueryable<User>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.FindSimilarReportsAsync(embedding, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        _genAIServiceMock.Setup(x => x.GenerateReportStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable(reportContent));
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(reportContent)).ReturnsAsync(embedding);
        _reportRepositoryMock.Setup(x => x.AddReportWithEmbeddingAsync(It.IsAny<GeneratedReport>(), It.IsAny<float[]?>(), It.IsAny<CancellationToken>()))
            .Callback<GeneratedReport, float[]?, CancellationToken>((r, _, _) => capturedReport = r)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        capturedReport.Should().NotBeNull();
        capturedReport!.Title.Should().Be("My Report Title");
    }

    [Fact]
    public async Task Handle_WhenRAGContextFails_ContinuesWithoutContext()
    {
        // Arrange
        var prompt = "Generate a report";
        var reportRequestDto = new GenerateReportRequestDto { Prompt = prompt };
        var httpResponse = CreateMockHttpResponse();
        var request = new GenerateReportRequest(reportRequestDto, httpResponse);
        
        _userContextServiceMock.Setup(x => x.GetCurrentUserId()).Returns((string?)null);
        _genAIServiceMock.Setup(x => x.GetEmbeddingAsync(prompt))
            .ThrowsAsync(new Exception("Embedding service unavailable"));
        _genAIServiceMock.Setup(x => x.GenerateReportStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable("Report content"));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _genAIServiceMock.Verify(x => x.GenerateReportStreamAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
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
