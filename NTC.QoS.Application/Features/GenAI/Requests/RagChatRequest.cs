using MediatR;
using Microsoft.AspNetCore.Http;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Request for RAG-based chat with semantic context retrieval from generated reports
/// </summary>
public class RagChatRequest(RagChatRequestDto ragChatRequestDto, HttpResponse httpResponse) : IRequest<Unit>
{
    public RagChatRequestDto Request { get; set; } = ragChatRequestDto;
    public HttpResponse HttpResponse { get; set; } = httpResponse;
}
