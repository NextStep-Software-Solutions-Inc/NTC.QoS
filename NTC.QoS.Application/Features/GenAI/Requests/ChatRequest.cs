using MediatR;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.GenAI;

namespace NTC.QoS.Application.Features.GenAI.Requests;

/// <summary>
/// Request to chat with AI assistant
/// </summary>
public class ChatRequest(ChatRequestDto chatRequestDto) : IRequest<ApiResponse<ChatResponseDto>>
{
    public ChatRequestDto Request { get; set; } = chatRequestDto;
}
