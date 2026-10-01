using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.BuildDynamics;
using LagoVista.IoT.Web.Common.Interfaces.BuildDynamics;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [RequireSignedRequest]
    [Route("api/service/build-dynamics/{organizationId}")]
    public sealed class BuildDynamicsServiceController : ControllerBase
    {
        private readonly IWorkstreamAuthorityRepository _authority;
        private readonly IAarCompletionRepository _aar;
        private readonly IStorageRetentionPolicyStore _retention;
        private readonly IBuildPerformanceTelemetryService _performance;
        private readonly IWorkstreamMessageRepository _messages;

        public BuildDynamicsServiceController(
            IWorkstreamAuthorityRepository authority,
            IAarCompletionRepository aar,
            IStorageRetentionPolicyStore retention,
            IBuildPerformanceTelemetryService performance,
            IWorkstreamMessageRepository messages = null)
        {
            _authority = authority ?? throw new ArgumentNullException(nameof(authority));
            _aar = aar ?? throw new ArgumentNullException(nameof(aar));
            _retention = retention ?? throw new ArgumentNullException(nameof(retention));
            _performance = performance ?? throw new ArgumentNullException(nameof(performance));
            _messages = messages;
        }

        [HttpGet("workstreams/{workstreamId}")]
        public async Task<IActionResult> GetWorkstreamAsync(string organizationId, string workstreamId, CancellationToken ct = default)
        {
            var versioned = await _authority.GetWorkstreamAsync(Scope(organizationId), Required(workstreamId, nameof(workstreamId)), ct);
            return versioned == null ? NotFound() : Ok(Versioned(versioned));
        }

        [HttpPost("workstreams")]
        public async Task<IActionResult> InsertWorkstreamAsync(string organizationId, [FromBody] WorkstreamAuthorityRecord record, CancellationToken ct = default)
        {
            if (record == null || String.IsNullOrWhiteSpace(record.WorkstreamId))
                return BadRequest("record.workstreamId is required.");

            record.Organization = Scope(organizationId);
            await _authority.InsertWorkstreamAsync(record, ct);
            return Ok();
        }

        [HttpPut("workstreams/{workstreamId}")]
        public async Task<IActionResult> UpdateWorkstreamAsync(string organizationId, string workstreamId, [FromBody] BuildDynamicsMutationRequest<WorkstreamAuthorityRecord> request, CancellationToken ct = default)
        {
            if (request?.Record == null || String.IsNullOrWhiteSpace(request.ExpectedVersion))
                return BadRequest("record and expectedVersion are required.");

            request.Record.Organization = Scope(organizationId);
            request.Record.WorkstreamId = Required(workstreamId, nameof(workstreamId));
            return MapMutation(await _authority.UpdateWorkstreamAsync(request.Record, ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct));
        }

        [HttpGet("workstreams/{workstreamId}/tasks")]
        public async Task<IActionResult> QueryTasksAsync(string organizationId, string workstreamId, int pageSize = 100, string continuationToken = null, CancellationToken ct = default)
        {
            var page = await _authority.QueryTasksAsync(
                Scope(organizationId),
                Required(workstreamId, nameof(workstreamId)),
                Page(pageSize, continuationToken),
                ct);
            return Ok(ToPage(page));
        }

        [HttpGet("tasks/{taskId}")]
        public async Task<IActionResult> GetTaskAsync(string organizationId, string taskId, CancellationToken ct = default)
        {
            var versioned = await _authority.GetTaskAsync(Scope(organizationId), Required(taskId, nameof(taskId)), ct);
            return versioned == null ? NotFound() : Ok(Versioned(versioned));
        }

        [HttpPost("tasks")]
        public async Task<IActionResult> InsertTaskAsync(string organizationId, [FromBody] TaskAuthorityRecord record, CancellationToken ct = default)
        {
            if (record == null || String.IsNullOrWhiteSpace(record.TaskId) || String.IsNullOrWhiteSpace(record.WorkstreamId))
                return BadRequest("record.workstreamId and record.taskId are required.");

            record.Organization = Scope(organizationId);
            await _authority.InsertTaskAsync(record, ct);
            return Ok();
        }

        [HttpPut("tasks/{taskId}")]
        public async Task<IActionResult> UpdateTaskAsync(string organizationId, string taskId, [FromBody] BuildDynamicsMutationRequest<TaskAuthorityRecord> request, CancellationToken ct = default)
        {
            if (request?.Record == null || String.IsNullOrWhiteSpace(request.ExpectedVersion))
                return BadRequest("record and expectedVersion are required.");

            request.Record.Organization = Scope(organizationId);
            request.Record.TaskId = Required(taskId, nameof(taskId));
            return MapMutation(await _authority.UpdateTaskAsync(request.Record, ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct));
        }

        [HttpGet("workspaces/{workspaceId}")]
        public async Task<IActionResult> GetWorkspaceAsync(string organizationId, string workspaceId, CancellationToken ct = default)
        {
            var versioned = await _authority.GetWorkspaceAsync(Scope(organizationId), Required(workspaceId, nameof(workspaceId)), ct);
            return versioned == null ? NotFound() : Ok(Versioned(versioned));
        }

        [HttpPost("workspaces")]
        public async Task<IActionResult> InsertWorkspaceAsync(string organizationId, [FromBody] WorkspaceAuthorityRecord record, CancellationToken ct = default)
        {
            if (record == null || String.IsNullOrWhiteSpace(record.WorkstreamId) || String.IsNullOrWhiteSpace(record.TaskId) || String.IsNullOrWhiteSpace(record.WorkspaceId))
                return BadRequest("record.workstreamId, record.taskId, and record.workspaceId are required.");

            record.Organization = Scope(organizationId);
            await _authority.InsertWorkspaceAsync(record, ct);
            return Ok();
        }

        [HttpPut("workspaces/{workspaceId}")]
        public async Task<IActionResult> UpdateWorkspaceAsync(string organizationId, string workspaceId, [FromBody] BuildDynamicsMutationRequest<WorkspaceAuthorityRecord> request, CancellationToken ct = default)
        {
            if (request?.Record == null || String.IsNullOrWhiteSpace(request.ExpectedVersion))
                return BadRequest("record and expectedVersion are required.");

            request.Record.Organization = Scope(organizationId);
            request.Record.WorkspaceId = Required(workspaceId, nameof(workspaceId));
            return MapMutation(await _authority.UpdateWorkspaceAsync(request.Record, ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct));
        }

        [HttpGet("workstreams/{workstreamId}/activity")]
        public async Task<IActionResult> QueryActivityAsync(string organizationId, string workstreamId, DateTime? startUtc = null, DateTime? endUtc = null, int pageSize = 100, string continuationToken = null, CancellationToken ct = default)
        {
            var page = await _authority.QueryActivityAsync(
                Scope(organizationId),
                Required(workstreamId, nameof(workstreamId)),
                startUtc,
                endUtc,
                Page(pageSize, continuationToken),
                ct);
            return Ok(ToPage(page));
        }

        [HttpPost("activity")]
        public async Task<IActionResult> AppendActivityAsync(string organizationId, [FromBody] WorkstreamActivityRecord record, CancellationToken ct = default)
        {
            if (record == null)
                return BadRequest("record is required.");

            record.OrganizationId = Scope(organizationId).Id;
            await _authority.AppendActivityAsync(record, ct);
            return Ok();
        }

        [HttpGet("coordination/{recordType}/{stableId}")]
        public async Task<IActionResult> GetCoordinationAsync(string organizationId, string recordType, string stableId, CancellationToken ct = default)
        {
            var versioned = await _authority.GetCoordinationAsync(Scope(organizationId), Required(recordType, nameof(recordType)), Required(stableId, nameof(stableId)), ct);
            return versioned == null ? NotFound() : Ok(Versioned(versioned));
        }

        [HttpGet("workstreams/{workstreamId}/coordination/{recordType}")]
        public async Task<IActionResult> QueryCoordinationAsync(string organizationId, string workstreamId, string recordType, int pageSize = 100, string continuationToken = null, CancellationToken ct = default)
        {
            var page = await _authority.QueryCoordinationAsync(Scope(organizationId), Required(workstreamId, nameof(workstreamId)), Required(recordType, nameof(recordType)), Page(pageSize, continuationToken), ct);
            return Ok(ToPage(page));
        }

        [HttpPost("coordination")]
        public async Task<IActionResult> InsertCoordinationAsync(string organizationId, [FromBody] WorkstreamCoordinationAuthorityRecord record, CancellationToken ct = default)
        {
            if (record == null || String.IsNullOrWhiteSpace(record.WorkstreamId) || String.IsNullOrWhiteSpace(record.RecordType) || String.IsNullOrWhiteSpace(record.StableId))
                return BadRequest("record.workstreamId, record.recordType, and record.stableId are required.");

            record.Organization = Scope(organizationId);
            await _authority.InsertCoordinationAsync(record, ct);
            return Ok();
        }

        [HttpPut("coordination/{recordType}/{stableId}")]
        public async Task<IActionResult> UpdateCoordinationAsync(string organizationId, string recordType, string stableId, [FromBody] BuildDynamicsMutationRequest<WorkstreamCoordinationAuthorityRecord> request, CancellationToken ct = default)
        {
            if (request?.Record == null || String.IsNullOrWhiteSpace(request.ExpectedVersion))
                return BadRequest("record and expectedVersion are required.");

            request.Record.Organization = Scope(organizationId);
            request.Record.RecordType = Required(recordType, nameof(recordType));
            request.Record.StableId = Required(stableId, nameof(stableId));
            return MapMutation(await _authority.UpdateCoordinationAsync(request.Record, ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct));
        }

        [HttpPut("orchestration/{scratchId}")]
        public async Task<IActionResult> UpsertOrchestrationAsync(string organizationId, string scratchId, [FromBody] WorkstreamOrchestrationScratchRecord record, CancellationToken ct = default)
        {
            if (record == null)
                return BadRequest("record is required.");

            record.Id = new NormalizedId32(Required(scratchId, nameof(scratchId)));
            record.Organization = Scope(organizationId);
            await _authority.UpsertScratchAsync(record, ct);
            return Ok();
        }

        [HttpGet("orchestration/{scratchId}")]
        public async Task<IActionResult> GetOrchestrationAsync(string organizationId, string scratchId, CancellationToken ct = default)
        {
            var record = await _authority.GetScratchAsync(Scope(organizationId), Required(scratchId, nameof(scratchId)), ct);
            return record == null ? NotFound() : Ok(record);
        }

        [HttpDelete("orchestration/{scratchId}")]
        public async Task<IActionResult> DeleteOrchestrationAsync(string organizationId, string scratchId, CancellationToken ct = default)
        {
            await _authority.DeleteScratchAsync(Scope(organizationId), Required(scratchId, nameof(scratchId)), ct);
            return Ok();
        }

        [HttpPost("workstreams/{workstreamId}/messages")]
        public async Task<IActionResult> PublishMessageAsync(string organizationId, string workstreamId, [FromBody] WorkstreamMessageRecord record, CancellationToken ct = default)
        {
            if (record == null) return BadRequest("record is required.");
            if (_messages == null) return StatusCode(500, "workstream message repository is unavailable.");
            if (String.IsNullOrWhiteSpace(record.Id)) return BadRequest("id is required.");
            if (String.IsNullOrWhiteSpace(record.FromRole) || String.IsNullOrWhiteSpace(record.ToRole)) return BadRequest("fromRole and toRole are required.");
            if (String.IsNullOrWhiteSpace(record.MessageType) || String.IsNullOrWhiteSpace(record.Summary)) return BadRequest("messageType and summary are required.");
            if (record.AttentionRequired && String.IsNullOrWhiteSpace(record.AttentionAction)) return BadRequest("attentionAction is required when attentionRequired is true.");

            record.OrganizationId = Scope(organizationId).Id;
            record.Organization = record.OrganizationId;
            record.WorkstreamId = Required(workstreamId, nameof(workstreamId));
            await _messages.PublishAsync(record, ct);
            return Ok();
        }

        [HttpGet("workstreams/{workstreamId}/messages")]
        public async Task<IActionResult> QueryMessagesAsync(string organizationId, string workstreamId, string taskId = null, string recipientRole = null, string recipientId = null, bool attentionOnly = false, int pageSize = 100, string continuationToken = null, CancellationToken ct = default)
        {
            if (_messages == null) return StatusCode(500, "workstream message repository is unavailable.");
            var page = await _messages.QueryAsync(Scope(organizationId), Required(workstreamId, nameof(workstreamId)), taskId, recipientRole, recipientId, attentionOnly, Page(pageSize, continuationToken), ct);
            return Ok(ToPage(page));
        }

        [HttpGet("workstreams/{workstreamId}/inbox")]
        public async Task<IActionResult> QueryInboxAsync(string organizationId, string workstreamId, string recipientRole, string recipientId = null, string taskId = null, int pageSize = 100, string continuationToken = null, CancellationToken ct = default)
        {
            if (_messages == null) return StatusCode(500, "workstream message repository is unavailable.");
            if (String.IsNullOrWhiteSpace(recipientRole)) return BadRequest("recipientRole is required.");

            var page = await _messages.QueryAsync(Scope(organizationId), Required(workstreamId, nameof(workstreamId)), taskId, recipientRole, recipientId, true, Page(pageSize, continuationToken), ct);
            return Ok(ToPage(page));
        }


        [HttpGet("performance/tasks-per-hour")]
        public async Task<IActionResult> QueryTasksPerHourAsync(
            string organizationId,
            DateTime startUtc,
            DateTime endUtc,
            string workstreamId = null,
            string taskId = null,
            string repository = null,
            string project = null,
            string groupBy = null,
            CancellationToken ct = default)
        {
            var result = await _performance.QueryTasksPerHourAsync(
                Required(organizationId, nameof(organizationId)),
                startUtc,
                endUtc,
                PerformanceDimensions(workstreamId, taskId, repository, project),
                PerformanceGroups(groupBy),
                ct);
            return Ok(result);
        }

        [HttpGet("performance/build-duration")]
        public async Task<IActionResult> QueryBuildDurationAsync(
            string organizationId,
            DateTime startUtc,
            DateTime endUtc,
            bool p95 = false,
            string workstreamId = null,
            string taskId = null,
            string repository = null,
            string project = null,
            string groupBy = null,
            CancellationToken ct = default)
        {
            var result = await _performance.QueryBuildDurationPercentileAsync(
                Required(organizationId, nameof(organizationId)),
                startUtc,
                endUtc,
                p95,
                PerformanceDimensions(workstreamId, taskId, repository, project),
                PerformanceGroups(groupBy),
                ct);
            return Ok(result);
        }
        [HttpGet("workstreams/{workstreamId}/aars")]
        public async Task<IActionResult> QueryAarsAsync(string organizationId, string workstreamId, int pageSize = 100, string continuationToken = null, CancellationToken ct = default)
        {
            var page = await _aar.QueryAarsAsync(
                Scope(organizationId),
                Required(workstreamId, nameof(workstreamId)),
                Page(pageSize, continuationToken),
                ct);
            return Ok(ToPage(page));
        }

        [HttpGet("aars/{aarId}")]
        public async Task<IActionResult> GetAarAsync(string organizationId, string aarId, CancellationToken ct = default)
        {
            var versioned = await _aar.GetAarAsync(Scope(organizationId), Required(aarId, nameof(aarId)), ct);
            return versioned == null ? NotFound() : Ok(Versioned(versioned));
        }

        [HttpPost("aars")]
        public async Task<IActionResult> InsertAarAsync(string organizationId, [FromBody] AarRecord record, CancellationToken ct = default)
        {
            if (record == null)
                return BadRequest("record is required.");

            record.Organization = Scope(organizationId);
            await _aar.InsertAarAsync(record, ct);
            return Ok();
        }

        [HttpPut("aars/{aarId}")]
        public async Task<IActionResult> UpdateAarAsync(string organizationId, string aarId, [FromBody] BuildDynamicsMutationRequest<AarRecord> request, CancellationToken ct = default)
        {
            if (request?.Record == null || String.IsNullOrWhiteSpace(request.ExpectedVersion))
                return BadRequest("record and expectedVersion are required.");

            request.Record.Organization = Scope(organizationId);
            request.Record.AarId = Required(aarId, nameof(aarId));
            return MapMutation(await _aar.UpdateAarAsync(request.Record, ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct));
        }

        [HttpGet("workstreams/{workstreamId}/completion-summary")]
        public async Task<IActionResult> GetCompletionSummaryAsync(string organizationId, string workstreamId, CancellationToken ct = default)
        {
            var versioned = await _aar.GetCompletionSummaryAsync(Scope(organizationId), Required(workstreamId, nameof(workstreamId)), ct);
            return versioned == null ? NotFound() : Ok(Versioned(versioned));
        }

        [HttpPost("workstreams/{workstreamId}/completion-summary")]
        public async Task<IActionResult> EnsureCompletionSummaryAsync(string organizationId, string workstreamId, [FromBody] WorkstreamCompletionSummaryRecord record, CancellationToken ct = default)
        {
            if (record == null)
                return BadRequest("record is required.");

            record.Organization = Scope(organizationId);
            record.WorkstreamId = Required(workstreamId, nameof(workstreamId));
            return Ok(await _aar.EnsureCompletionSummaryAsync(record, ct));
        }

        [HttpGet("retention")]
        public async Task<IActionResult> GetRetentionPolicyAsync(string organizationId, CancellationToken ct = default)
        {
            var versioned = await _retention.GetVersionedAsync(RetentionKey(Scope(organizationId)), ct);
            if (versioned == null)
                return Ok(new BuildDynamicsVersionedRecord<StorageRetentionPolicyRecord>
                {
                    Record = NewRetentionRecord(Scope(organizationId)),
                    Version = String.Empty
                });

            return Ok(Versioned(versioned));
        }

        [HttpPut("retention")]
        public async Task<IActionResult> UpdateRetentionPolicyAsync(string organizationId, [FromBody] BuildDynamicsRetentionPolicyUpdateRequest request, CancellationToken ct = default)
        {
            if (request == null)
                return BadRequest("request is required.");

            var scope = Scope(organizationId);
            var record = NewRetentionRecord(scope);
            record.Rules = request.Rules ?? new System.Collections.Generic.List<StorageRetentionRule>();
            foreach (var rule in record.Rules)
                rule.Validate();

            if (String.IsNullOrWhiteSpace(request.ExpectedVersion))
            {
                var existing = await _retention.GetVersionedAsync(RetentionKey(scope), ct);
                if (existing != null)
                    return Conflict("expectedVersion is required when replacing an existing retention policy.");

                await _retention.InsertAsync(record, ct);
                return Ok(new BuildDynamicsMutationResponse { Status = "created" });
            }

            return MapMutation(await _retention.UpdateIfVersionAsync(record, ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct));
        }

        [HttpGet("retention/effective")]
        public async Task<IActionResult> GetEffectiveRetentionAsync(string organizationId, StorageRecordClass recordClass, string scope = null, CancellationToken ct = default)
        {
            var organization = Scope(organizationId);
            var versioned = await _retention.GetVersionedAsync(RetentionKey(organization), ct);
            var decision = versioned?.Record?.ToPolicy().Resolve(recordClass, scope) ?? StorageRetentionDecision.DurableDefault();

            return Ok(new BuildDynamicsRetentionDecisionResponse
            {
                EffectiveTtlSeconds = decision.EffectiveTtl?.TotalSeconds,
                IsProtected = decision.IsProtected,
                SummarizeBeforeExpiry = decision.SummarizeBeforeExpiry,
                ExplicitlyGoverned = decision.ExplicitlyGoverned,
                Source = decision.Source
            });
        }


        private static System.Collections.Generic.IEnumerable<MetricDimensionFilter> PerformanceDimensions(
            string workstreamId,
            string taskId,
            string repository,
            string project)
        {
            if (!String.IsNullOrWhiteSpace(workstreamId))
                yield return new MetricDimensionFilter("workstream", workstreamId.Trim());
            if (!String.IsNullOrWhiteSpace(taskId))
                yield return new MetricDimensionFilter("task", taskId.Trim());
            if (!String.IsNullOrWhiteSpace(repository))
                yield return new MetricDimensionFilter("repository", repository.Trim());
            if (!String.IsNullOrWhiteSpace(project))
                yield return new MetricDimensionFilter("project", project.Trim());
        }

        private static System.Collections.Generic.IEnumerable<string> PerformanceGroups(string groupBy)
            => String.IsNullOrWhiteSpace(groupBy)
                ? Array.Empty<string>()
                : groupBy.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Trim())
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase);
        private static BuildDynamicsVersionedRecord<T> Versioned<T>(VersionedApplicationDataRecord<T> value)
            where T : class, IApplicationDataRecord
            => new BuildDynamicsVersionedRecord<T>
            {
                Record = value.Record,
                Version = value.ConcurrencyToken.Value
            };

        private static BuildDynamicsPage<T> ToPage<T>(StoragePageResult<T> value)
            => new BuildDynamicsPage<T>
            {
                Items = value?.Items ?? Array.Empty<T>(),
                ContinuationToken = value?.ContinuationToken ?? String.Empty
            };

        private IActionResult MapMutation(ApplicationDataMutationResult result)
        {
            if (result == null)
                return StatusCode(500, "conditional mutation returned no result.");

            var response = new BuildDynamicsMutationResponse
            {
                Status = result.Status.ToString().ToLowerInvariant(),
                Version = result.ConcurrencyToken?.Value ?? String.Empty
            };

            return result.Status switch
            {
                ApplicationDataMutationStatus.Updated => Ok(response),
                ApplicationDataMutationStatus.Conflict => Conflict(response),
                ApplicationDataMutationStatus.NotFound => NotFound(response),
                _ => StatusCode(500, response)
            };
        }

        private static StoragePageRequest Page(int pageSize, string continuationToken)
            => new StoragePageRequest(Math.Clamp(pageSize, 1, 1000), continuationToken);

        private static EntityHeader Scope(string organizationId)
        {
            var id = Required(organizationId, nameof(organizationId));
            return EntityHeader.Create(id, id);
        }

        private static StorageKey RetentionKey(EntityHeader scope)
            => new StorageKey(RetentionId(), scope.Id);

        private static StorageRetentionPolicyRecord NewRetentionRecord(EntityHeader scope)
            => new StorageRetentionPolicyRecord
            {
                Id = new NormalizedId32(RetentionId()),
                Organization = scope
            };

        private static string RetentionId()
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("build-dynamics:retention-policy"));
            return BitConverter.ToString(bytes).Replace("-", String.Empty).Substring(0, 32);
        }

        private static string Required(string value, string name)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException(name + " is required.", name);
            return value.Trim();
        }
    }
}
