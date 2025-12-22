using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NTC.QoS.Domain.Entities;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.ReportTemplate;
using NTC.QoS.Infrastructure.Persistence;

namespace NTC.QoS.API.Controllers;

[ApiController]
[Route("report-templates", Name = "report-templates")]
[Authorize]
public class ReportTemplatesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportTemplatesController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all active report templates (Id, Title, Description only).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ReportTemplateListDto>>>> GetAll()
    {
        var templates = await _context.ReportTemplates
            .Where(t => t.IsActive)
            .Select(t => new ReportTemplateListDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<ReportTemplateListDto>>(templates));
    }

    /// <summary>
    /// Get a report template by ID (full details including Instruction).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ReportTemplateDto>>> GetById(Guid id)
    {
        var template = await _context.ReportTemplates
            .Where(t => t.Id == id)
            .Select(t => new ReportTemplateDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Instruction = t.Instruction,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (template == null)
        {
            return NotFound(new ApiResponse<ReportTemplateDto>("Report template not found."));
        }

        return Ok(new ApiResponse<ReportTemplateDto>(template));
    }

    /// <summary>
    /// Create a new report template.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportTemplateDto>>> Create([FromBody] CreateTemplateDto dto)
    {
        var template = new ReportTemplate
        {
            Title = dto.Title,
            Description = dto.Description,
            Instruction = dto.Instruction,
            IsActive = true
        };

        _context.ReportTemplates.Add(template);
        await _context.SaveChangesAsync();

        var result = new ReportTemplateDto
        {
            Id = template.Id,
            Title = template.Title,
            Description = template.Description,
            Instruction = template.Instruction,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = template.Id }, new ApiResponse<ReportTemplateDto>(result));
    }

    /// <summary>
    /// Update a report template.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ReportTemplateDto>>> Update(Guid id, [FromBody] UpdateTemplateDto dto)
    {
        var template = await _context.ReportTemplates
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound(new ApiResponse<ReportTemplateDto>("Report template not found."));
        }

        if (dto.Title != null)
        {
            template.Title = dto.Title;
        }

        if (dto.Description != null)
        {
            template.Description = dto.Description;
        }

        if (dto.Instruction != null)
        {
            template.Instruction = dto.Instruction;
        }

        if (dto.IsActive.HasValue)
        {
            template.IsActive = dto.IsActive.Value;
        }

        await _context.SaveChangesAsync();

        var result = new ReportTemplateDto
        {
            Id = template.Id,
            Title = template.Title,
            Description = template.Description,
            Instruction = template.Instruction,
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };

        return Ok(new ApiResponse<ReportTemplateDto>(result));
    }

    /// <summary>
    /// Soft delete a report template.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        var template = await _context.ReportTemplates
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound(new ApiResponse<bool>("Report template not found."));
        }

        _context.ReportTemplates.Remove(template);
        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<bool>(true, true, "Report template deleted successfully."));
    }
}
