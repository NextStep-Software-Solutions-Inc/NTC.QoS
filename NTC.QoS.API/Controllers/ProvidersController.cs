using Microsoft.AspNetCore.Mvc;
using MediatR;
using NTC.QoS.Application.Features.Provider.Queries;
using NTC.QoS.Domain.Entities;

namespace NTC.QoS.API.Controllers
{

    [ApiController]
    [Route("providers", Name = "providers")]
    public class ProvidersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProvidersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<Provider>>> GetAllProviders()
        {
            var query = new GetAllProvidersQuery();
            var products = await _mediator.Send(query);
            return Ok(products);
        }
    }
}
