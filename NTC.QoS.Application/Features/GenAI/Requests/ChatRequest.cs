using MediatR;
using Microsoft.AspNetCore.Http;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Request to chat with AI assistant with streaming response
/// </summary>
public class ChatRequest(ChatRequestDto chatRequestDto, HttpResponse httpResponse) : IRequest<Unit>
{
    public ChatRequestDto Request { get; set; } = chatRequestDto;
    public HttpResponse HttpResponse { get; set; } = httpResponse;
}
