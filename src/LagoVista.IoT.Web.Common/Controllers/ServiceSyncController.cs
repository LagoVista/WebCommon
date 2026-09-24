using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// Service-to-service sync surface used by trusted platform automation.
    /// Authentication is provided by the ServiceHttpV1 signed-request profile.
    ///
    /// The organization id is part of the signed request path so callers cannot
    /// alter sync scope without invalidating the request signature.
    /// </summary>
    [RequireSignedRequest]
    [Route("api/service/sync/{organizationId}")]
    public class ServiceSyncController : ControllerBase
    {
        private readonly ISyncRepository _syncRepository;

        public ServiceSyncController(ISyncRepository syncRepository)
        {
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
        }

        [HttpGet("summaries")]
        public async Task<ListResponse<SyncEntitySummary>> GetSummariesAsync(
            [FromRoute] string organizationId,
            [FromQuery] string entityType,
            [FromQuery] string search = null,
            [FromQuery] int take = 200,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
                return ListResponse<SyncEntitySummary>.FromError("organizationId is required.");

            if (string.IsNullOrWhiteSpace(entityType))
                return ListResponse<SyncEntitySummary>.FromError("entityType is required.");

            if (take <= 0) take = 200;
            if (take > 2000) take = 2000;

            try
            {
                var summaries = await _syncRepository.GetSummariesAsync(
                    entityType.Trim(),
                    organizationId.Trim(),
                    search,
                    take,
                    ct);

                return ListResponse<SyncEntitySummary>.Create(summaries);
            }
            catch (Exception ex)
            {
                return ListResponse<SyncEntitySummary>.FromError($"Failed to load summaries: {ex.Message}");
            }
        }

        [HttpGet("entity/{entityType}/{key}")]
        public async Task<ActionResult<InvokeResult<SyncJsonEnvelope>>> GetEntityAsync(
            [FromRoute] string organizationId,
            [FromRoute] string entityType,
            [FromRoute] string key,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
                return BadRequest(InvokeResult<SyncJsonEnvelope>.FromError("organizationId is required."));

            if (string.IsNullOrWhiteSpace(entityType))
                return BadRequest(InvokeResult<SyncJsonEnvelope>.FromError("entityType is required."));

            if (string.IsNullOrWhiteSpace(key))
                return BadRequest(InvokeResult<SyncJsonEnvelope>.FromError("key is required."));

            try
            {
                var json = await _syncRepository.GetJsonByEntityTypeAndKeyAsync(
                    key.Trim(),
                    entityType.Trim(),
                    organizationId.Trim(),
                    ct);

                if (string.IsNullOrWhiteSpace(json))
                    return NotFound(InvokeResult<SyncJsonEnvelope>.FromError("Item not found."));

                return Ok(InvokeResult<SyncJsonEnvelope>.Create(new SyncJsonEnvelope { Json = json }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, InvokeResult<SyncJsonEnvelope>.FromError($"Failed to load item: {ex.Message}"));
            }
        }
    }
}
