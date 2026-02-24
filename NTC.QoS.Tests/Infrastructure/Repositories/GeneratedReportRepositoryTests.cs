using FluentAssertions;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Interfaces;
using Xunit;

namespace NTC.QoS.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for GeneratedReportRepository RAG-related functionality
/// Note: These are interface contract tests that verify the expected behavior
/// Full integration tests would require a PostgreSQL database with pgvector extension
/// </summary>
public class GeneratedReportRepositoryTests
{
    [Fact]
    public void FindSimilarReportsAsync_ShouldHaveCorrectSignature()
    {
        // This test verifies the interface contract
        // The actual implementation requires a PostgreSQL database with pgvector
        var interfaceType = typeof(IGeneratedReportRepository);
        var method = interfaceType.GetMethod("FindSimilarReportsAsync");
        
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<List<string>>));
        
        var parameters = method.GetParameters();
        parameters.Should().HaveCount(3);
        parameters[0].ParameterType.Should().Be(typeof(float[]));
        parameters[0].Name.Should().Be("queryEmbedding");
        parameters[1].ParameterType.Should().Be(typeof(int));
        parameters[1].Name.Should().Be("maxResults");
        parameters[2].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void AddReportWithEmbeddingAsync_ShouldHaveCorrectSignature()
    {
        // This test verifies the interface contract
        var interfaceType = typeof(IGeneratedReportRepository);
        var method = interfaceType.GetMethod("AddReportWithEmbeddingAsync");
        
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
        
        var parameters = method.GetParameters();
        parameters.Should().HaveCount(3);
        parameters[0].ParameterType.Should().Be(typeof(GeneratedReport));
        parameters[0].Name.Should().Be("report");
        parameters[1].ParameterType.Should().Be(typeof(float[]));
        parameters[1].Name.Should().Be("embedding");
        parameters[2].ParameterType.Should().Be(typeof(CancellationToken));
    }

    [Fact]
    public void IGeneratedReportRepository_ShouldInheritFromIRepository()
    {
        // Verify the repository inherits from the base IRepository<GeneratedReport>
        var interfaceType = typeof(IGeneratedReportRepository);
        var baseInterface = typeof(IRepository<GeneratedReport>);
        
        interfaceType.GetInterfaces().Should().Contain(baseInterface);
    }

    [Fact]
    public void GeneratedReport_ShouldHaveEmbeddingProperty()
    {
        // Verify the GeneratedReport entity has the Embedding property for vector storage
        var entityType = typeof(GeneratedReport);
        var embeddingProperty = entityType.GetProperty("Embedding");
        
        embeddingProperty.Should().NotBeNull();
    }
}
