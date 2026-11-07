using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// Read-only diagnostic viewer for the System Admin application.
    /// Cassandra is an append-only 48-hour stream; Mongo scratch contains
    /// longer-lived errors. Both are accessed through their existing readers.
    /// </summary>
    [Authorize]
    [SystemAdmin]
    [ApiController]
    [Route("api/diagnostics")]
    public sealed class DiagnosticsController : LagoVistaBaseController
    {
        private readonly IDiagnosticLogReader _logs;
        private readonly IApplicationErrorRepository _errors;

        public DiagnosticsController(
            UserManager<AppUser> userManager,
            IAdminLogger logger,
            IDiagnosticLogReader logs,
            IApplicationErrorRepository errors)
            : base(userManager, logger)
        {
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
            _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        /// <summary>
        /// Combined, most-recent-first view of both diagnostic stores.
        /// V1 pages the latest 500 records per application/environment/window;
        /// source-specific queries are available for deeper investigation.
        /// </summary>
        [HttpGet]
        public async Task<ListResponse<DiagnosticRecordSummary>> GetDiagnosticsAsync(
            [FromQuery] string application,
            [FromQuery] string environment,
            [FromQuery] int hours = 2,
            [FromQuery] string level = null,
            [FromQuery] string tag = null,
            CancellationToken ct = default)
        {
            var request = GetListRequestFromHeader();
            if (String.IsNullOrWhiteSpace(application) || String.IsNullOrWhiteSpace(environment))
                return ListResponse<DiagnosticRecordSummary>.FromError("Application and environment are required for a unified diagnostic query.");
            if (hours < 1 || hours > 48)
                return ListResponse<DiagnosticRecordSummary>.FromError("Unified diagnostic queries must be within the last 1-48 hours.");
            if (request.PageSize < 1 || request.PageSize > 100 || request.PageIndex < 1 ||
                (long)request.PageIndex * request.PageSize > 500)
                return ListResponse<DiagnosticRecordSummary>.FromError("Unified diagnostics support up to 500 bounded records using page sizes 1-100.");

            try
            {
                var now = DateTime.UtcNow;
                var from = now.AddHours(-hours);
                var env = NormalizeEnvironment(environment);
                var top = request.PageIndex * request.PageSize + 1;
                var logsTask = _logs.QueryAsync(new DiagnosticLogQuery
                {
                    Application = application.Trim(),
                    Environment = env,
                    StartUtc = from,
                    EndUtc = now,
                    PageSize = Math.Min(top, 500)
                }, ct);
                var errorsTask = _errors.GetRecentErrorsAsync(1000, application.Trim(), ct);
                await Task.WhenAll(logsTask, errorsTask).ConfigureAwait(false);

                var logPage = await logsTask.ConfigureAwait(false);
                var errors = await errorsTask.ConfigureAwait(false);
                var rows = logPage.Records
                    .Select(record => DiagnosticRecordSummary.FromLog(record))
                    .Concat(errors
                        .Where(record => record != null &&
                                         String.Equals(record.Environment, env, StringComparison.OrdinalIgnoreCase) &&
                                         record.TimeStamp >= from && record.TimeStamp <= now)
                        .Select(record => DiagnosticRecordSummary.FromError(record)))
                    .Where(record => (String.IsNullOrWhiteSpace(level) ||
                                      String.Equals(record.LogLevel, level.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                                     (String.IsNullOrWhiteSpace(tag) ||
                                      String.Equals(record.Tag, tag.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(record => record.TimeStamp)
                    .ThenBy(record => record.Source, StringComparer.Ordinal)
                    .ThenBy(record => record.Id, StringComparer.Ordinal)
                    .ToList();

                var skip = (request.PageIndex - 1) * request.PageSize;
                var selected = rows.Skip(skip).Take(request.PageSize).ToList();
                var more = rows.Count > skip + selected.Count ||
                           !String.IsNullOrWhiteSpace(logPage.ContinuationToken);
                return ListResponse<DiagnosticRecordSummary>.Create(selected, request, more, String.Empty, String.Empty);
            }
            catch (Exception ex)
            {
                return ListResponse<DiagnosticRecordSummary>.FromError("Failed to query diagnostics: " + ex.Message);
            }
        }

        /// <summary>Query Cassandra logs with native opaque continuation tokens.</summary>
        [HttpGet("logs")]
        public async Task<ListResponse<LogRecord>> GetLogsAsync(
            [FromQuery] string application,
            [FromQuery] string environment,
            [FromQuery] int hours = 2,
            [FromQuery] string level = null,
            [FromQuery] string tag = null,
            CancellationToken ct = default)
        {
            var request = GetListRequestFromHeader();
            if (String.IsNullOrWhiteSpace(application) || String.IsNullOrWhiteSpace(environment))
                return ListResponse<LogRecord>.FromError("Application and environment are required.");
            if (hours < 1 || hours > 48 || request.PageSize < 1 || request.PageSize > 500)
                return ListResponse<LogRecord>.FromError("Cassandra diagnostics require 1-48 hours and a page size of 1-500.");
            if (request.PageIndex > 1 && !request.HasCursor)
                return ListResponse<LogRecord>.FromError("A continuation cursor is required after the first Cassandra page.");
            if (request.HasCursor && request.NextPartitionKey != "diagnostic-logs")
                return ListResponse<LogRecord>.FromError("Invalid diagnostic continuation cursor.");

            try
            {
                var now = DateTime.UtcNow;
                var result = await _logs.QueryAsync(new DiagnosticLogQuery
                {
                    Application = application.Trim(),
                    Environment = NormalizeEnvironment(environment),
                    StartUtc = now.AddHours(-hours),
                    EndUtc = now,
                    Level = level,
                    Tag = tag,
                    PageSize = request.PageSize,
                    ContinuationToken = request.HasCursor ? request.NextRowKey : null
                }, ct).ConfigureAwait(false);
                var hasMore = !String.IsNullOrWhiteSpace(result.ContinuationToken);
                return ListResponse<LogRecord>.Create(
                    result.Records, request, hasMore,
                    hasMore ? "diagnostic-logs" : String.Empty,
                    result.ContinuationToken ?? String.Empty);
            }
            catch (Exception ex)
            {
                return ListResponse<LogRecord>.FromError("Failed to query Cassandra diagnostics: " + ex.Message);
            }
        }

        /// <summary>
        /// Read the 30-day Mongo scratch error store. The existing repository
        /// exposes the latest 1,000 entries; pages are bounded by that limit.
        /// </summary>
        [HttpGet("errors")]
        public async Task<ListResponse<ApplicationErrorSummary>> GetErrorsAsync(
            [FromQuery] string application = null,
            [FromQuery] string environment = null,
            [FromQuery] int hours = 48,
            CancellationToken ct = default)
        {
            var request = GetListRequestFromHeader();
            if (hours < 1 || hours > 720 || request.PageSize < 1 || request.PageSize > 100 ||
                request.PageIndex < 1 || (long)request.PageIndex * request.PageSize > 1000)
                return ListResponse<ApplicationErrorSummary>.FromError("Mongo error queries allow 1-720 hours and up to 1,000 recent records, in pages of 1-100.");
            try
            {
                var latest = await _errors.GetRecentErrorsAsync(1000, application, ct).ConfigureAwait(false);
                var from = DateTime.UtcNow.AddHours(-hours);
                var env = String.IsNullOrWhiteSpace(environment) ? null : NormalizeEnvironment(environment);
                var eligible = latest
                    .Where(error => error != null && error.TimeStamp >= from &&
                                    (env == null || String.Equals(error.Environment, env, StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(error => error.TimeStamp).ToList();
                var skip = (request.PageIndex - 1) * request.PageSize;
                var model = eligible.Skip(skip).Take(request.PageSize).ToList();
                return ListResponse<ApplicationErrorSummary>.Create(model, request,
                    eligible.Count > skip + model.Count, String.Empty, String.Empty);
            }
            catch (Exception ex)
            {
                return ListResponse<ApplicationErrorSummary>.FromError("Failed to query Mongo diagnostic errors: " + ex.Message);
            }
        }

        [HttpGet("errors/{id}")]
        public async Task<ActionResult<InvokeResult<LogRecord>>> GetErrorAsync(
            [FromRoute] string id,
            CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(id))
                return BadRequest(InvokeResult<LogRecord>.FromError("Error id is required."));
            try
            {
                var record = await _errors.GetErrorAsync(id.Trim(), ct).ConfigureAwait(false);
                return record == null
                    ? NotFound(InvokeResult<LogRecord>.FromError("Error record not found."))
                    : Ok(InvokeResult<LogRecord>.Create(record));
            }
            catch (Exception ex)
            {
                return StatusCode(500, InvokeResult<LogRecord>.FromError("Failed to load diagnostic error: " + ex.Message));
            }
        }

        private static string NormalizeEnvironment(string environment)
        {
            var normalized = environment?.Trim();
            if (String.Equals(normalized, "DEV", StringComparison.OrdinalIgnoreCase))
                return "Development";
            if (String.Equals(normalized, "LIVE", StringComparison.OrdinalIgnoreCase))
                return "Production";
            return normalized;
        }
    }

    public sealed class DiagnosticRecordSummary
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Application { get; set; }
        public string Environment { get; set; }
        public string LogLevel { get; set; }
        public string Tag { get; set; }
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public string Version { get; set; }
        public string HostId { get; set; }
        public string Details { get; set; }
        public string StackTrace { get; set; }

        public static DiagnosticRecordSummary FromLog(LogRecord record) => new DiagnosticRecordSummary
        {
            Id = record.Id.ToString(),
            Source = "Cassandra",
            TimeStamp = record.TimeStamp,
            Application = record.Application,
            Environment = record.Environment,
            LogLevel = record.LogLevel,
            Tag = record.Tag,
            Message = record.Message,
            ExceptionType = record.ExceptionType,
            Version = record.Version,
            HostId = record.HostId,
            Details = record.Details,
            StackTrace = record.StackTrace
        };

        public static DiagnosticRecordSummary FromError(ApplicationErrorSummary record) => new DiagnosticRecordSummary
        {
            Id = record.Id.ToString(),
            Source = "Mongo",
            TimeStamp = record.TimeStamp,
            Application = record.Application,
            Environment = record.Environment,
            LogLevel = record.LogLevel,
            Tag = record.Tag,
            Message = record.Message,
            ExceptionType = record.ExceptionType,
            Version = record.Version,
            HostId = record.HostId
        };
    }
}
