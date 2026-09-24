using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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
        private static readonly EntityHeader BuildServerActor = new EntityHeader
        {
            Id = "0000000000000000000000000000b001",
            Key = "build-server",
            Text = "Software Logistics Build Server",
            EntityType = "ServicePrincipal"
        };

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

        /// <summary>
        /// V1 write surface for Build Server configuration replication.
        /// Deliberately restricted to Module until broader entity/reference semantics are defined.
        /// </summary>
        [HttpPost("module/upsert")]
        public async Task<InvokeResult<SyncUpsertResult>> UpsertModuleAsync(
            [FromRoute] string organizationId,
            [FromBody] SyncUpsertRequest request,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(organizationId))
                return InvokeResult<SyncUpsertResult>.FromError("organizationId is required.");

            if (request == null || string.IsNullOrWhiteSpace(request.Json))
                return InvokeResult<SyncUpsertResult>.FromError("json is required.");

            JObject document;
            try
            {
                document = JObject.Parse(request.Json);
            }
            catch (Exception ex)
            {
                return InvokeResult<SyncUpsertResult>.FromError($"Invalid JSON: {ex.Message}");
            }

            var entityType = document["EntityType"]?.Value<string>();
            if (!string.Equals(entityType, "Module", StringComparison.Ordinal))
                return InvokeResult<SyncUpsertResult>.FromError("Service sync V1 only permits EntityType 'Module'.");

            try
            {
                var targetOrganization = await _syncRepository.GetEntityHeaderForRecordAsync(organizationId.Trim(), ct);
                if (targetOrganization == null || !string.Equals(targetOrganization.Id, organizationId.Trim(), StringComparison.OrdinalIgnoreCase))
                    return InvokeResult<SyncUpsertResult>.FromError($"Target organization '{organizationId}' was not found.");

                var result = await _syncRepository.UpsertJsonAsync(
                    request.Json,
                    targetOrganization,
                    BuildServerActor,
                    ct);

                return InvokeResult<SyncUpsertResult>.Create(result);
            }
            catch (Exception ex)
            {
                return InvokeResult<SyncUpsertResult>.FromError($"Module upsert failed: {ex.Message}");
            }
        }
    }
}
