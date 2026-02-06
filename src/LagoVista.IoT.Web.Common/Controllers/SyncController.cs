using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [Authorize]
    [Route("api/admin/sync")]
    public class SyncController : LagoVistaBaseController
    {
        ISyncRepository _syncRepository;

        public SyncController(ISyncRepository syncRepository, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
        }

        /// <summary>
        /// Returns a lightweight list of summaries for a given entity type.
        /// This is the primary endpoint for the WPF "compare grid".
        /// </summary>
        /// <example>
        /// GET /api/admin/sync/summaries?entityType=OrganizationDomain&amp;search=acme&amp;take=50
        /// </example>
        [HttpGet("summaries")]
        public async Task<ListResponse<SyncEntitySummary>> GetSummariesAsync(
            [FromQuery] string entityType,
            [FromQuery] string search = null,
            [FromQuery] int take = 200,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return ListResponse<SyncEntitySummary>.FromError("entityType is required.");

            if (take <= 0) take = 200;
            if (take > 2000) take = 2000; // safety rail

            try
            {
                var summaries = await _syncRepository.GetSummariesAsync(entityType.Trim(), OrgEntityHeader.Id, search, take, ct);
                return ListResponse<SyncEntitySummary>.Create(summaries);
            }
            catch (Exception ex)
            {
                // Keep payload safe but useful.
                return ListResponse<SyncEntitySummary>.FromError($"Failed to load summaries: {ex.Message}");
            }
        }

        /// <summary>
        /// Fetch raw JSON by id. WPF uses this for side-by-side display.
        /// </summary>
        [HttpGet("entity/{id}")]
        public async Task<ActionResult<InvokeResult<SyncJsonEnvelope>>> GetItemJsonByIdAsync(
            [FromRoute] string id,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(InvokeResult<SyncJsonEnvelope>.FromError("id is required."));

            try
            {
                var json = await _syncRepository.GetOwnedJsonByIdAsync(id.Trim(), OrgEntityHeader.Id, ct);
                if (string.IsNullOrWhiteSpace(json))
                    return NotFound(InvokeResult<SyncJsonEnvelope>.FromError("Item not found."));

                return Ok(InvokeResult<SyncJsonEnvelope>.Create(new SyncJsonEnvelope { Json = json }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, InvokeResult<SyncJsonEnvelope>.FromError($"Failed to load item: {ex.Message}"));
            }
        }

        [HttpGet("entity/{entitytype}/{key}")]
        public async Task<ActionResult<InvokeResult<SyncJsonEnvelope>>> GetItemJsonByEntityTypeAndKeyAsync(
           [FromRoute] string key,
           [FromRoute] string entitytype,
           CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest(InvokeResult<SyncJsonEnvelope>.FromError("id is required."));

            try
            {
                var json = await _syncRepository.GetJsonByEntityTypeAndKeyAsync(key.Trim(), entitytype, OrgEntityHeader.Id, ct);
                if (string.IsNullOrWhiteSpace(json))
                    return NotFound(InvokeResult<SyncJsonEnvelope>.FromError("Item not found."));

                return Ok(InvokeResult<SyncJsonEnvelope>.Create(new SyncJsonEnvelope { Json = json }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, InvokeResult<SyncJsonEnvelope>.FromError($"Failed to load item: {ex.Message}"));
            }
        }


        [HttpGet("entity/{id}/entityheader")]
        public async Task<InvokeResult<EntityHeader>> GetEntityHeaderForEntity(
           [FromRoute] string id,
           CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return InvokeResult<EntityHeader>.FromError("id is required.");

            try
            {
                var eh = await _syncRepository.GetEntityHeaderForRecordAsync(id.Trim(),ct);
                
                return InvokeResult<EntityHeader>.Create(eh);
            }
            catch (Exception ex)
            {
                return InvokeResult<EntityHeader>.FromError(ex.Message);
            }
        }


        [HttpGet("/api/entity/{id}/resolve/entityheaders")]
        public Task<InvokeResult<EhResolvedEntity>> ResolveEntityHeadersForIdsAsync([FromRoute] string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.FromResult(InvokeResult<EhResolvedEntity>.FromError("id is required."));
            try
            {
                return _syncRepository.ResolveEntityHeadersAsync(id.Trim(), ct);
            }
            catch (Exception ex)
            {
                return Task.FromResult(InvokeResult< EhResolvedEntity>.FromError(ex.Message));
            }
        }

        [HttpGet("/api/entitytype/{entitytype}/resolve/entityheaders")]
        public Task<InvokeResult<List<EhResolvedEntity>>> ResolveEntityHeadersForIdsAsync([FromRoute] string entitytype, string continuationToken = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(entitytype))
                return Task.FromResult(InvokeResult<List<EhResolvedEntity>>.FromError("id is required."));
            try
            {
                return _syncRepository.ResolveEntityHeadersAsync(entitytype, continuationToken, ct);
            }
            catch (Exception ex)
            {
                return Task.FromResult(InvokeResult<List<EhResolvedEntity>>.FromError(ex.Message));
            }
        }


        /// <summary>
        /// Upsert a raw JSON document. Optional expectedETag provides optimistic concurrency.
        /// WPF uses this when applying changes from env A to env B.
        /// </summary>
        [HttpPost("entity/upsert")]
        public async Task<InvokeResult<SyncUpsertResult>> UpsertAsync(
            [FromBody] SyncUpsertRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                return InvokeResult<SyncUpsertResult>.FromError("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Json))
                return InvokeResult<SyncUpsertResult>.FromError("json is required.");

            // Lightweight JSON sanity check (keeps server errors nicer).
            try { JsonConvert.DeserializeObject(request.Json); }
            catch (Exception ex)
            {
                return InvokeResult<SyncUpsertResult>.FromError($"Invalid JSON: {ex.Message}");
            }

            try
            {
                var result = await _syncRepository.UpsertJsonAsync(request.Json, OrgEntityHeader, UserEntityHeader, ct);
                return InvokeResult<SyncUpsertResult>.Create(result);
            }
            catch (InvalidOperationException ex)
            {
                // Your repo throws this on ETag mismatch (412).
                return InvokeResult<SyncUpsertResult>.FromError(ex.Message);
            }
            catch (Exception ex)
            {
                return InvokeResult<SyncUpsertResult>.FromError($"Upsert failed: {ex.Message}");
            }
        }


    }
}
