using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTC.QoS.Application.Features.Region.Requests;

public class DeleteRegionRequest
{
    public DeleteRegionRequest(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; set; }
}
