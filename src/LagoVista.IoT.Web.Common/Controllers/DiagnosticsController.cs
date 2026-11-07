using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// Read-only system-admin diagnostics over Cassandra's 48-hour per-application
    /// partitions and Mongo's 30-day error scratch store.
    /// </summary>
    [Authorize]
    [SystemAdmin]
    [ApiController]
    [Route("api/diagnostics")]
    public sealed class DiagnosticsController : LagoVistaBaseController
    {
        private const int MaxCombinedResults = 500;
        private const int MaxTags = 5;
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
        /// Up to 500 most recent combined records, in pages; real app/environment
        /// scope remains mandatory for Cassandra. Multiple tags use OR semantics.
        /// startUtc/endUtc override the relative hours window when both are set.
        /// </summary>
        [HttpGet]
        public async Task<ListResponse<DiagnosticRecordSummary>> GetDiagnosticsAsync(
            [FromQuery] string application,
            [FromQuery] string environment,
            [FromQuery] int hours = 2,
            [FromQuery] string level = null,
            [FromQuery] string tag = null,
            [FromQuery] DateTime? startUtc = null,
            [FromQuery] DateTime? endUtc = null,
            [FromQuery] string tags = null,
            CancellationToken ct = default)
        {
            var request = GetListRequestFromHeader();
            if (String.IsNullOrWhiteSpace(application) || String.IsNullOrWhiteSpace(environment))
                return ListResponse<DiagnosticRecordSummary>.FromError("Application and environment are required for a combined diagnostics query.");
            if (!ValidBoundedPage(request, 100, MaxCombinedResults))
                return ListResponse<DiagnosticRecordSummary>.FromError("Combined diagnostics support pages of 1-100, within the latest 500 results.");
            if (!TryWindow(hours, startUtc, endUtc, 48, out var from, out var to, out var error))
                return ListResponse<DiagnosticRecordSummary>.FromError(error);
            var selectedTags = ParseTags(tag, tags);
            if (selectedTags.Count > MaxTags)
                return ListResponse<DiagnosticRecordSummary>.FromError("Select no more than five tags.");

            try
            {
                var app = application.Trim();
                var env = NormalizeEnvironment(environment);
                var pages = await QueryLogPagesAsync(app, env, from, to, level, selectedTags, MaxCombinedResults, ct).ConfigureAwait(false);
                var errors = await _errors.GetRecentErrorsAsync(1000, app, ct).ConfigureAwait(false);
                var merged = pages.SelectMany(page => page.Records)
                    .Select(DiagnosticRecordSummary.FromLog)
                    .Concat(errors.Where(item => item != null &&
                               InWindow(item.TimeStamp, from, to) &&
                               String.Equals(item.Environment, env, StringComparison.OrdinalIgnoreCase))
                        .Select(DiagnosticRecordSummary.FromError))
                    .Where(item =>
                        Matches(item.LogLevel, level) &&
                        (selectedTags.Count == 0 ||
                         selectedTags.Any(value => Matches(item.Tag, value))))
                    .OrderByDescending(item => item.TimeStamp)
                    .ThenBy(item => item.Source, StringComparer.Ordinal)
                    .ThenBy(item => item.Id, StringComparer.Ordinal)
                    .Take(MaxCombinedResults)
                    .ToList();

                var skip = (request.PageIndex - 1) * request.PageSize;
                var items = merged.Skip(skip).Take(request.PageSize).ToList();
                return ListResponse<DiagnosticRecordSummary>.Create(
                    items, request, merged.Count > skip + items.Count, String.Empty, String.Empty);
            }
            catch (Exception ex)
            {
                return ListResponse<DiagnosticRecordSummary>.FromError("Failed to query diagnostics: " + ex.Message);
            }
        }

        /// <summary>
        /// Native Cassandra paging for zero or one tag. Up to five selected tags
        /// use bounded union paging over the latest 500 matching records.
        /// </summary>
        [HttpGet("logs")]
        public async Task<ListResponse<LogRecord>> GetLogsAsync(
            [FromQuery] string application,
            [FromQuery] string environment,
            [FromQuery] int hours = 2,
            [FromQuery] string level = null,
            [FromQuery] string tag = null,
            [FromQuery] DateTime? startUtc = null,
            [FromQuery] DateTime? endUtc = null,
            [FromQuery] string tags = null,
            CancellationToken ct = default)
        {
            var request = GetListRequestFromHeader();
            if (String.IsNullOrWhiteSpace(application) || String.IsNullOrWhiteSpace(environment))
                return ListResponse<LogRecord>.FromError("Application and environment are required for Cassandra diagnostics.");
            if (!TryWindow(hours, startUtc, endUtc, 48, out var from, out var to, out var error))
                return ListResponse<LogRecord>.FromError(error);
            var selectedTags = ParseTags(tag, tags);
            if (selectedTags.Count > MaxTags)
                return ListResponse<LogRecord>.FromError("Select no more than five tags.");

            var multiple = selectedTags.Count > 1;
            if (request.PageSize < 1 || request.PageSize > (multiple ? 100 : 500) ||
                request.PageIndex < 1 || (multiple && (long)request.PageIndex * request.PageSize > MaxCombinedResults))
                return ListResponse<LogRecord>.FromError("Cassandra diagnostics require a bounded page: up to 500 records, or 100 per page for multiple tags.");
            if (multiple && request.HasCursor)
                return ListResponse<LogRecord>.FromError("Multiple-tag queries use page indexes, not Cassandra continuation tokens.");
            if (!multiple && request.PageIndex > 1 && !request.HasCursor)
                return ListResponse<LogRecord>.FromError("A Cassandra continuation cursor is required after the first page.");
            if (!multiple && request.HasCursor && request.NextPartitionKey != "diagnostic-logs")
                return ListResponse<LogRecord>.FromError("Invalid diagnostic continuation cursor.");

            try
            {
                var app = application.Trim();
                var env = NormalizeEnvironment(environment);
                if (multiple)
                {
                    var pages = await QueryLogPagesAsync(app, env, from, to, level, selectedTags, MaxCombinedResults, ct).ConfigureAwait(false);
                    var ordered = pages.SelectMany(page => page.Records)
                        .OrderByDescending(item => item.TimeStamp)
                        .ThenBy(item => item.Id.ToString(), StringComparer.Ordinal)
                        .Take(MaxCombinedResults).ToList();
                    var skip = (request.PageIndex - 1) * request.PageSize;
                    var items = ordered.Skip(skip).Take(request.PageSize).ToList();
                    return ListResponse<LogRecord>.Create(
                        items, request, ordered.Count > skip + items.Count, String.Empty, String.Empty);
                }

                var result = await _logs.QueryAsync(new DiagnosticLogQuery
                {
                    Application = app,
                    Environment = env,
                    StartUtc = from,
                    EndUtc = to,
                    Level = level,
                    Tag = selectedTags.FirstOrDefault(),
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
        /// Pages over the existing repository's latest 1,000 Mongo error summaries.
        /// </summary>
        [HttpGet("errors")]
        public async Task<ListResponse<ApplicationErrorSummary>> GetErrorsAsync(
            [FromQuery] string application = null,
            [FromQuery] string environment = null,
            [FromQuery] int hours = 48,
            [FromQuery] DateTime? startUtc = null,
            [FromQuery] DateTime? endUtc = null,
            [FromQuery] string tags = null,
            CancellationToken ct = default)
        {
            var request = GetListRequestFromHeader();
            if (!ValidBoundedPage(request, 100, 1000))
                return ListResponse<ApplicationErrorSummary>.FromError("Mongo error queries support up to 1,000 recent records in pages of 1-100.");
            if (!TryWindow(hours, startUtc, endUtc, 720, out var from, out var to, out var error))
                return ListResponse<ApplicationErrorSummary>.FromError(error);
            var selectedTags = ParseTags(null, tags);
            if (selectedTags.Count > MaxTags)
                return ListResponse<ApplicationErrorSummary>.FromError("Select no more than five tags.");

            try
            {
                var latest = await _errors.GetRecentErrorsAsync(1000, application, ct).ConfigureAwait(false);
                var env = String.IsNullOrWhiteSpace(environment) ? null : NormalizeEnvironment(environment);
                var eligible = latest.Where(item =>
                        item != null && InWindow(item.TimeStamp, from, to) &&
                        (env == null || String.Equals(item.Environment, env, StringComparison.OrdinalIgnoreCase)) &&
                        (selectedTags.Count == 0 || selectedTags.Any(value => Matches(item.Tag, value))))
                    .OrderByDescending(item => item.TimeStamp).ToList();
                var skip = (request.PageIndex - 1) * request.PageSize;
                var items = eligible.Skip(skip).Take(request.PageSize).ToList();
                return ListResponse<ApplicationErrorSummary>.Create(
                    items, request, eligible.Count > skip + items.Count, String.Empty, String.Empty);
            }
            catch (Exception ex)
            {
                return ListResponse<ApplicationErrorSummary>.FromError("Failed to query Mongo diagnostic errors: " + ex.Message);
            }
        }

        /// <summary>
        /// Searchable nuviotTag facet for the chosen application, source and time range.
        /// Counts are from a bounded sample: up to 500 Cassandra events and 1,000
        /// Mongo error summaries. They are NOT global aggregate counts.
        /// </summary>
        [HttpGet("facets/tags")]
        public async Task<ListResponse<DiagnosticTagFacet>> GetTagFacetsAsync(
            [FromQuery] string application,
            [FromQuery] string environment,
            [FromQuery] int hours = 2,
            [FromQuery] string source = "all",
            [FromQuery] string level = null,
            [FromQuery] DateTime? startUtc = null,
            [FromQuery] DateTime? endUtc = null,
            CancellationToken ct = default)
        {
            var request = GetListRequestFromHeader();
            var errorsOnly = String.Equals(source, "errors", StringComparison.OrdinalIgnoreCase);
            var logsOnly = String.Equals(source, "logs", StringComparison.OrdinalIgnoreCase);
            if (!errorsOnly && !logsOnly && !Matches(source, "all"))
                return ListResponse<DiagnosticTagFacet>.FromError("Unknown diagnostic source.");
            if ((!errorsOnly && String.IsNullOrWhiteSpace(application)) ||
                String.IsNullOrWhiteSpace(environment))
                return ListResponse<DiagnosticTagFacet>.FromError("Application and environment are required for diagnostic tag facets.");
            if (!ValidBoundedPage(request, 500, 1000))
                return ListResponse<DiagnosticTagFacet>.FromError("Tag facets support a page size of 1-500.");
            if (!TryWindow(hours, startUtc, endUtc, errorsOnly ? 720 : 48, out var from, out var to, out var error))
                return ListResponse<DiagnosticTagFacet>.FromError(error);

            try
            {
                var sample = new List<string>();
                var env = NormalizeEnvironment(environment);
                if (!errorsOnly)
                {
                    var page = await _logs.QueryAsync(new DiagnosticLogQuery
                    {
                        Application = application.Trim(),
                        Environment = env,
                        StartUtc = from,
                        EndUtc = to,
                        Level = level,
                        PageSize = 500
                    }, ct).ConfigureAwait(false);
                    sample.AddRange(page.Records.Select(item => item.Tag));
                }
                if (!logsOnly)
                {
                    var errors = await _errors.GetRecentErrorsAsync(1000, application, ct).ConfigureAwait(false);
                    sample.AddRange(errors.Where(item => item != null && InWindow(item.TimeStamp, from, to) &&
                                         String.Equals(item.Environment, env, StringComparison.OrdinalIgnoreCase) &&
                                         Matches(item.LogLevel, level))
                        .Select(item => item.Tag));
                }

                var total = sample.Count;
                var facets = sample.Where(value => !String.IsNullOrWhiteSpace(value))
                    .GroupBy(value => value.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new DiagnosticTagFacet
                    {
                        Value = group.Key,
                        Count = group.Count(),
                        SampleSize = total,
                        Percentage = total == 0 ? 0 : Math.Round(100d * group.Count() / total, 1)
                    })
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var skip = (request.PageIndex - 1) * request.PageSize;
                var items = facets.Skip(skip).Take(request.PageSize).ToList();
                var response = ListResponse<DiagnosticTagFacet>.Create(
                    items, request, facets.Count > skip + items.Count, String.Empty, String.Empty);
                response.Help = "Facet percentages are based on a bounded " + total +
                                "-record sample, not the total volume of stored logs.";
                return response;
            }
            catch (Exception ex)
            {
                return ListResponse<DiagnosticTagFacet>.FromError("Failed to compute tag facets: " + ex.Message);
            }
        }

        [HttpGet("errors/{id}")]
        public async Task<ActionResult<InvokeResult<LogRecord>>> GetErrorAsync(
            [FromRoute] string id, CancellationToken ct = default)
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

        private async Task<DiagnosticLogPage[]> QueryLogPagesAsync(
            string application, string environment, DateTime from, DateTime to,
            string level, IReadOnlyList<string> tags, int perTagLimit, CancellationToken ct)
        {
            var selected = tags.Count == 0 ? new string[] { null } : tags.ToArray();
            var tasks = selected.Select(tag => _logs.QueryAsync(new DiagnosticLogQuery
            {
                Application = application,
                Environment = environment,
                StartUtc = from,
                EndUtc = to,
                Level = level,
                Tag = tag,
                PageSize = perTagLimit
            }, ct)).ToArray();
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static IReadOnlyList<string> ParseTags(string tag, string tags)
        {
            if (!String.IsNullOrWhiteSpace(tags))
                return tags.Split('|')
                    .Select(value => value.Trim())
                    .Where(value => value.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            return String.IsNullOrWhiteSpace(tag) ? Array.Empty<string>() : new[] { tag.Trim() };
        }

        private static bool ValidBoundedPage(ListRequest request, int maxPageSize, int maxTotal)
        {
            return request.PageSize >= 1 && request.PageSize <= maxPageSize &&
                   request.PageIndex >= 1 && (long)request.PageIndex * request.PageSize <= maxTotal;
        }

        private static bool InWindow(DateTime time, DateTime from, DateTime to)
        {
            var utc = NormalizeUtc(time);
            return utc >= from && utc <= to;
        }

        private static bool Matches(string actual, string selected)
        {
            return String.IsNullOrWhiteSpace(selected) ||
                   String.Equals(actual, selected.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryWindow(int hours, DateTime? startUtc, DateTime? endUtc, int maxHours,
            out DateTime from, out DateTime to, out string error)
        {
            from = default;
            to = default;
            error = null;
            if (startUtc.HasValue != endUtc.HasValue)
            {
                error = "Absolute queries require both startUtc and endUtc.";
                return false;
            }
            if (!startUtc.HasValue && (hours < 1 || hours > maxHours))
            {
                error = "Relative diagnostic window must be between 1 and " + maxHours + " hours.";
                return false;
            }
            to = endUtc.HasValue ? NormalizeUtc(endUtc.Value) : DateTime.UtcNow;
            from = startUtc.HasValue ? NormalizeUtc(startUtc.Value) : to.AddHours(-hours);
            if (to <= from || to > DateTime.UtcNow.AddMinutes(5) ||
                to - from > TimeSpan.FromHours(maxHours))
            {
                error = "Diagnostic window must be positive, end no later than now, and span at most " +
                        maxHours + " hours.";
                return false;
            }
            return true;
        }

        private static DateTime NormalizeUtc(DateTime date)
        {
            if (date.Kind == DateTimeKind.Utc) return date;
            if (date.Kind == DateTimeKind.Unspecified) return DateTime.SpecifyKind(date, DateTimeKind.Utc);
            return date.ToUniversalTime();
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

    public sealed class DiagnosticTagFacet
    {
        public string Value { get; set; }
        public int Count { get; set; }
        public int SampleSize { get; set; }
        public double Percentage { get; set; }
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
