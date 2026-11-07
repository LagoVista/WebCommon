using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>Read-only discovery of operator-visible runtime controls from the CloudStorage catalog.</summary>
    [SystemAdmin]
    [Authorize]
    [ApiController]
    [Route("api/sysadmin/runtime-controls")]
    public sealed class RuntimeControlsCatalogController : ControllerBase
    {
        [HttpGet]
        public InvokeResult<List<RuntimeControlDefinition>> Get()
        {
            // Control metadata comes exclusively from CloudStorage; no Valkey query or UI-maintained list.
            var definitions = RuntimeControlCatalog.Definitions
                .Where(definition => definition.VisibleInAdmin)
                .OrderBy(definition => definition.Group)
                .ThenBy(definition => definition.Title)
                .ToList();

            return InvokeResult<List<RuntimeControlDefinition>>.Create(definitions);
        }
    }
}
