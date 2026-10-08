using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// Machine-to-machine, read-only journal diagnostics. The service-http signing
    /// filter authenticates the caller; tenant scope is part of the signed route.
    /// Public user routes remain under /api/devops/operations.
    /// </summary>
    [ApiController]
    [RequireSignedRequest]
    [Route("api/service/devops/operations/{organizationId}")]
    public sealed class ServiceOperationJournalController : ControllerBase
    {
        private readonly OperationJournalAccessService _journal;
        private readonly ILogger<ServiceOperationJournalController> _logger;

        public ServiceOperationJournalController(
            OperationJournalAccessService journal,
            ILogger<ServiceOperationJournalController> logger)
        {
            _journal = journal;
            _logger = logger;
        }

        private static OperationJournalPrincipal SignedPrincipal(string organizationId)
        {
            if (String.IsNullOrWhiteSpace(organizationId))
                throw new ArgumentException("Organization id is required.", nameof(organizationId));
            return new OperationJournalPrincipal
            {
                OrganizationId = organizationId,
                CanReadAllOperations = true,
                InternalDeterministicExecutor = false
            };
        }

        private async Task<ActionResult<InvokeResult<T>>> Execute<T>(Func<Task<T>> action)
        {
            try
            {
                return Ok(InvokeResult<T>.Create(await action()));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, InvokeResult<T>.FromError(ex.Message, "JOURNAL_FORBIDDEN"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(InvokeResult<T>.FromError(ex.Message, "JOURNAL_NOT_FOUND"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(InvokeResult<T>.FromError(ex.Message, "JOURNAL_INVALID_REQUEST"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Signed Operation Journal request failed.");
                return StatusCode(502, InvokeResult<T>.FromError(ex.Message, "JOURNAL_BACKEND_FAILURE"));
            }
        }

        [HttpGet]
        public Task<ActionResult<InvokeResult<OperationJournalPage<OperationJournalRecord>>>> List(
            string organizationId,
            [FromQuery] string scopeType, [FromQuery] string ownerId,
            [FromQuery] DateTimeOffset startUtc, [FromQuery] DateTimeOffset endUtc,
            [FromQuery] int pageSize = 50, [FromQuery] string cursor = null,
            CancellationToken ct = default)
            => Execute(() =>
            {
                if (scopeType != "workstream" && scopeType != "workspace" && scopeType != "fix-workspace")
                    throw new ArgumentException("Unknown operation scope.");
                if (String.IsNullOrWhiteSpace(ownerId) || startUtc >= endUtc)
                    throw new ArgumentException("Valid owner and time range are required.");
                var principal = SignedPrincipal(organizationId);
                return _journal.ListAsync(principal, new OperationJournalScope
                {
                    OrganizationId = principal.OrganizationId,
                    ScopeType = scopeType,
                    WorkstreamId = scopeType == "workstream" ? ownerId : null,
                    WorkspaceId = scopeType == "workspace" ? ownerId : null,
                    FixWorkspaceId = scopeType == "fix-workspace" ? ownerId : null,
                    StartUtc = startUtc,
                    EndUtc = endUtc
                }, pageSize, cursor, ct);
            });

        [HttpGet("{operationId}")]
        public Task<ActionResult<InvokeResult<OperationJournalRecord>>> Get(
            string organizationId, string operationId, CancellationToken ct)
            => Execute(() => _journal.GetAsync(SignedPrincipal(organizationId), operationId, ct));

        [HttpGet("{operationId}/details")]
        public Task<ActionResult<InvokeResult<OperationJournalPage<OperationJournalDetail>>>> Details(
            string organizationId, string operationId, [FromQuery] int pageSize = 50,
            [FromQuery] string cursor = null, CancellationToken ct = default)
            => Execute(() => _journal.GetDetailsAsync(
                SignedPrincipal(organizationId), operationId, pageSize, cursor, ct));
    }
}
