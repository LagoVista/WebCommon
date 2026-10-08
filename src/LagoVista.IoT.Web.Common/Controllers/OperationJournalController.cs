using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Validation;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using LagoVista.AspNetCore.Identity;
using LagoVista.AspNetCore.Identity.Managers;
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
    [Route("api/devops/operations")]
    public sealed class OperationJournalController : ControllerBase
    {
        private readonly OperationJournalAccessService _journal;
        private readonly ILogger<OperationJournalController> _logger;
        public OperationJournalController(OperationJournalAccessService journal, ILogger<OperationJournalController> logger)
        {
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        // Preserve the original exception message for actionable diagnostics, while
        // returning a typed InvokeResult rather than an empty ASP.NET failure.
        private async Task<ActionResult<InvokeResult<T>>> Execute<T>(Func<Task<T>> action)
        {
            try
            {
                return Ok(InvokeResult<T>.Create(await action()));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, InvokeResult<T>.FromError(ex.Message, "OPERATION_JOURNAL_FORBIDDEN"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(InvokeResult<T>.FromError(ex.Message, "OPERATION_JOURNAL_NOT_FOUND"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(InvokeResult<T>.FromError(ex.Message, "OPERATION_JOURNAL_ARGUMENT"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation Journal request failed.");
                return StatusCode(502, InvokeResult<T>.FromError(
                    ex.Message, "OPERATION_JOURNAL_UPSTREAM_FAILURE"));
            }
        }

        [HttpGet("{operationId}")]
        public Task<ActionResult<InvokeResult<OperationJournalRecord>>> Get(string operationId, CancellationToken ct)
            => Execute(() => _journal.GetAsync(Principal(), operationId, ct));

        [HttpGet("{operationId}/details")]
        public Task<ActionResult<InvokeResult<OperationJournalPage<OperationJournalDetail>>>> Details(string operationId,
            [FromQuery] int pageSize = 50, [FromQuery] string cursor = null, CancellationToken ct = default)
            => Execute(() => _journal.GetDetailsAsync(Principal(), operationId, pageSize, cursor, ct));

        [HttpGet]
        public Task<ActionResult<InvokeResult<OperationJournalPage<OperationJournalRecord>>>> List(
            [FromQuery] string scopeType, [FromQuery] string ownerId,
            [FromQuery] DateTimeOffset startUtc, [FromQuery] DateTimeOffset endUtc,
            [FromQuery] int pageSize = 50, [FromQuery] string cursor = null, CancellationToken ct = default)
            => Execute(() =>
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
            });

        [HttpPost("start")]
        public Task<ActionResult<InvokeResult<OperationJournalRecord>>> Start([FromBody] OperationJournalRecord operation, CancellationToken ct)
            => Execute(() => _journal.StartAsync(Principal(), operation, ct));

        [HttpPost("{operationId}/transition")]
        public Task<ActionResult<InvokeResult<OperationJournalRecord>>> Transition(string operationId,
            [FromBody] OperationJournalTransitionRequest request, CancellationToken ct)
            => Execute(() => _journal.TransitionAsync(Principal(), operationId,
                request.ExpectedStatus, request.NextStatus, request.Summary, request.ChangedAtUtc, ct));

        [HttpPost("{operationId}/recover")]
        public Task<ActionResult<InvokeResult<OperationJournalRecord>>> Recover(string operationId,
            [FromBody] OperationJournalRecoveryRequest request, CancellationToken ct)
            => Execute(() => _journal.RecoverAsync(Principal(), operationId,
                request.ExpectedStatus, request.Summary, request.ChangedAtUtc, ct));

        [HttpPost("{operationId}/details")]
        public Task<ActionResult<InvokeResult<OperationJournalDetail>>> Append(string operationId,
            [FromBody] OperationJournalDetail detail, CancellationToken ct)
            => Execute(() =>
            {
                if (detail == null || !String.Equals(detail.OperationId, operationId, StringComparison.Ordinal))
                    throw new ArgumentException("Detail must match route operation.");
                return _journal.AppendAsync(Principal(), detail, ct);
            });
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
