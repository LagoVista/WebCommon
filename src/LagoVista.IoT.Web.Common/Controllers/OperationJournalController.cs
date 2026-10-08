using LagoVista.CloudStorage.Storage;
using LagoVista.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>Authorized API for deterministic execution boundary history.
    /// No arbitrary model activity may be written through this controller.</summary>
    [Authorize]
    [ApiController]
    [Route("api/operations")]
    public sealed class OperationJournalController : ControllerBase
    {
        private readonly OperationJournalAccessService _journal;
        public OperationJournalController(OperationJournalAccessService journal)
        {
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        }

        private OperationJournalPrincipal Principal()
        {
            var org = User?.Claims?.FirstOrDefault(c => c.Type == ClaimsFactory.CurrentOrgId)?.Value;
            if (String.IsNullOrWhiteSpace(org))
                throw new UnauthorizedAccessException("Authenticated current organization required.");
            // Authorization-bearing claims must be issued by the trusted authentication pipeline.
            var claims = User.Claims;
            return new OperationJournalPrincipal
            {
                OrganizationId = org,
                InternalDeterministicExecutor = claims.Any(c =>
                    c.Type == "operation_journal_executor" && c.Value == "true"),
                CanReadAllOperations = claims.Any(c =>
                    c.Type == "operation_journal_read_all" && c.Value == "true"),
                WorkstreamIds = claims.Where(c => c.Type == "operation_journal_workstream").Select(c => c.Value).ToArray(),
                WorkspaceIds = claims.Where(c => c.Type == "operation_journal_workspace").Select(c => c.Value).ToArray(),
                FixWorkspaceIds = claims.Where(c => c.Type == "operation_journal_fix_workspace").Select(c => c.Value).ToArray()
            };
        }

        [HttpGet("{operationId}")]
        public Task<OperationJournalRecord> Get(string operationId, CancellationToken ct)
            => _journal.GetAsync(Principal(), operationId, ct);

        [HttpGet("{operationId}/details")]
        public Task<OperationJournalPage<OperationJournalDetail>> Details(string operationId,
            [FromQuery] int pageSize = 50, [FromQuery] string cursor = null, CancellationToken ct = default)
            => _journal.GetDetailsAsync(Principal(), operationId, pageSize, cursor, ct);

        [HttpGet]
        public Task<OperationJournalPage<OperationJournalRecord>> List(
            [FromQuery] string scopeType, [FromQuery] string ownerId,
            [FromQuery] DateTimeOffset startUtc, [FromQuery] DateTimeOffset endUtc,
            [FromQuery] int pageSize = 50, [FromQuery] string cursor = null, CancellationToken ct = default)
        {
            var principal = Principal();
            var scope = new OperationJournalScope
            {
                OrganizationId = principal.OrganizationId,
                ScopeType = scopeType,
                WorkstreamId = scopeType == "workstream" ? ownerId : null,
                WorkspaceId = scopeType == "workspace" ? ownerId : null,
                FixWorkspaceId = scopeType == "fix-workspace" ? ownerId : null,
                StartUtc = startUtc,
                EndUtc = endUtc
            };
            return _journal.ListAsync(principal, scope, pageSize, cursor, ct);
        }

        [HttpPost("start")]
        public Task<OperationJournalRecord> Start([FromBody] OperationJournalRecord operation, CancellationToken ct)
            => _journal.StartAsync(Principal(), operation, ct);

        [HttpPost("{operationId}/transition")]
        public Task<OperationJournalRecord> Transition(string operationId,
            [FromBody] OperationJournalTransitionRequest request, CancellationToken ct)
            => _journal.TransitionAsync(Principal(), operationId,
                request.ExpectedStatus, request.NextStatus, request.Summary, request.ChangedAtUtc, ct);

        [HttpPost("{operationId}/recover")]
        public Task<OperationJournalRecord> Recover(string operationId,
            [FromBody] OperationJournalRecoveryRequest request, CancellationToken ct)
            => _journal.RecoverAsync(Principal(), operationId,
                request.ExpectedStatus, request.Summary, request.ChangedAtUtc, ct);

        [HttpPost("{operationId}/details")]
        public Task<OperationJournalDetail> Append(string operationId,
            [FromBody] OperationJournalDetail detail, CancellationToken ct)
        {
            if (detail == null || !String.Equals(detail.OperationId, operationId, StringComparison.Ordinal))
                throw new ArgumentException("Detail must match route operation.");
            return _journal.AppendAsync(Principal(), detail, ct);
        }
    }

    public sealed class OperationJournalTransitionRequest
    {
        public string ExpectedStatus { get; set; }
        public string NextStatus { get; set; }
        public string Summary { get; set; }
        public DateTimeOffset ChangedAtUtc { get; set; }
    }

    public sealed class OperationJournalRecoveryRequest
    {
        public string ExpectedStatus { get; set; }
        public string Summary { get; set; }
        public DateTimeOffset ChangedAtUtc { get; set; }
    }
}
