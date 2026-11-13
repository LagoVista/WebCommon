using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// TEMPORARY one-time operator endpoint. Remove this controller after the LIVE SOP/EJA cleanup.
    /// It deliberately calls the existing AI REST delete actions, so authorization,
    /// dependency checks and the first-delete/second-delete semantics remain authoritative.
    /// Do NOT register this endpoint as an MCP tool.
    /// </summary>
    [ApiController]
    [Authorize]
    [SystemAdmin]
    [Route("api/sysadmin/maintenance/temporary-ai-purge")]
    public sealed class TemporaryAiRecordPurgeController : ControllerBase
    {
        private const string Confirmation = "PURGE ALL EJAS AND SOPS";
        private const int PageSize = 100;
        private readonly IHttpClientFactory _clients;

        public TemporaryAiRecordPurgeController(IHttpClientFactory clients)
        {
            _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        }

        public sealed class PurgeFailure
        {
            public string Type { get; set; }
            public string Id { get; set; }
            public string Message { get; set; }
        }

        public sealed class PurgeReport
        {
            public string OrganizationId { get; set; }
            public int EjasFound { get; set; }
            public int SopsFound { get; set; }
            public int EjasPermanentlyDeleted { get; set; }
            public int SopsPermanentlyDeleted { get; set; }
            public int SkippedOtherOrganization { get; set; }
            public List<PurgeFailure> Failures { get; set; } = new List<PurgeFailure>();
        }

        private sealed class Item
        {
            public string Id { get; set; }
            public bool IsDeleted { get; set; }
            public bool IsPublic { get; set; }
        }

        [HttpGet("run")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<InvokeResult<PurgeReport>>> RunAsync(
            [FromQuery] string confirmation,
            [FromQuery] string organizationId,
            CancellationToken ct = default)
        {
            // SystemAdminAttribute currently performs its check in OnActionExecuted.
            // Reject BEFORE the first side effect, even if that filter is changed.
            // This temporary browser GET intentionally requires BOTH exact confirmation
            // and active-org ID; no request without these values can delete data.
            if (User?.Identity?.IsAuthenticated != true ||
                !User.HasClaim(ClaimsFactory.IsSystemAdmin, true.ToString()))
            {
                return StatusCode(403, InvokeResult<PurgeReport>.FromError("System administrator access required."));
            }

            var orgId = User.FindFirst(ClaimsFactory.CurrentOrgId)?.Value;
            if (String.IsNullOrWhiteSpace(orgId) ||
                !String.Equals(confirmation, Confirmation, StringComparison.Ordinal) ||
                !String.Equals(organizationId, orgId, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(InvokeResult<PurgeReport>.FromError(
                    "Supply the active organization ID and the exact confirmation: " + Confirmation));
            }

            // The AI controllers are installed in the same API host as WebCommon.
            // Only call this host through loopback; no caller-supplied URI, redirects or proxies.
            var port = HttpContext.Connection.LocalPort;
            if (port <= 0)
                return StatusCode(500, InvokeResult<PurgeReport>.FromError("Local API listener port unavailable."));

            using var client = _clients.CreateClient();
            client.BaseAddress = new Uri("http://127.0.0.1:" + port + "/");
            client.Timeout = TimeSpan.FromMinutes(3);

            var report = new PurgeReport { OrganizationId = orgId };
            try
            {
                // Snapshot every ID BEFORE changing the underlying query results.
                // Query both views: some list implementations expose only soft-deleted
                // records with x-show-deleted, others include them alongside active ones.
                var ejas = await ListAllAsync(client, "api/vteam/ejas", ct);
                var sops = await ListAllAsync(client, "api/vteam/sops", ct);

                // A list can include public records owned by a different organization.
                // Private records are already scoped by the existing AI list query.
                ejas = await FilterPublicRecordsAsync(client, "eja", orgId, ejas, report, ct);
                sops = await FilterPublicRecordsAsync(client, "sop", orgId, sops, report, ct);
                report.EjasFound = ejas.Count;
                report.SopsFound = sops.Count;

                // EJAs reference SOPs, so purge EJAs first.
                report.EjasPermanentlyDeleted = await PurgeAsync(client, "eja", ejas, report, ct);
                report.SopsPermanentlyDeleted = await PurgeAsync(client, "sop", sops, report, ct);
                return InvokeResult<PurgeReport>.Create(report);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return StatusCode(408, InvokeResult<PurgeReport>.FromError(
                    "Purge request cancelled. Repeat the request to resume; already-deleted IDs are safe to skip."));
            }
            catch (Exception ex)
            {
                // A transport/paging failure is not success; earlier deletions may have completed.
                return StatusCode(500, InvokeResult<PurgeReport>.FromError(
                    "Purge stopped: " + ex.Message + ". Re-run the endpoint to resume."));
            }
        }

        private async Task<List<Item>> ListAllAsync(HttpClient client, string path, CancellationToken ct)
        {
            var items = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
            foreach (var showDeleted in new[] { false, true })
            {
                string nextPartitionKey = null;
                string nextRowKey = null;
                var previousFingerprints = new HashSet<string>(StringComparer.Ordinal);
                var completed = false;
                for (var page = 1; page <= 1000; page++)
                {
                    using var request = NewRequest(HttpMethod.Get, path);
                    request.Headers.TryAddWithoutValidation("x-pageindex", page.ToString());
                    request.Headers.TryAddWithoutValidation("x-pagesize", PageSize.ToString());
                    request.Headers.TryAddWithoutValidation("x-show-deleted", showDeleted ? "true" : "false");
                    if (!String.IsNullOrEmpty(nextPartitionKey))
                        request.Headers.TryAddWithoutValidation("x-nextpartitionkey", nextPartitionKey);
                    if (!String.IsNullOrEmpty(nextRowKey))
                        request.Headers.TryAddWithoutValidation("x-nextrowkey", nextRowKey);

                    using var response = await client.SendAsync(request, ct);
                    using var body = await ReadJsonAsync(response, ct);
                    if (!body.RootElement.TryGetProperty("model", out var model) ||
                        model.ValueKind != JsonValueKind.Array)
                        throw new InvalidOperationException(path + " returned no model array.");

                    var pageIds = new List<string>();
                    foreach (var row in model.EnumerateArray())
                    {
                        if (!row.TryGetProperty("id", out var idElement)) continue;
                        var id = idElement.GetString();
                        if (String.IsNullOrWhiteSpace(id)) continue;
                        pageIds.Add(id);
                        var isDeleted = Bool(row, "isDeleted");
                        var isPublic = Bool(row, "isPublic");
                        if (items.TryGetValue(id, out var existing))
                        {
                            existing.IsDeleted |= isDeleted;
                            existing.IsPublic |= isPublic;
                        }
                        else
                        {
                            items.Add(id, new Item { Id = id, IsDeleted = isDeleted, IsPublic = isPublic });
                        }
                    }

                    var hasMore = Bool(body.RootElement, "hasMoreRecords");
                    if (!hasMore)
                    {
                        completed = true;
                        break;
                    }
                    if (pageIds.Count == 0)
                        throw new InvalidOperationException(path + " reported more records but returned an empty page.");

                    var fingerprint = String.Join(",", pageIds);
                    if (!previousFingerprints.Add(fingerprint))
                        throw new InvalidOperationException(path + " returned a repeated page; refusing an incomplete purge.");

                    nextPartitionKey = StringValue(body.RootElement, "nextPartitionKey");
                    nextRowKey = StringValue(body.RootElement, "nextRowKey");
                }
                if (!completed)
                    throw new InvalidOperationException(path + " exceeded the paging limit.");
            }

            return items.Values.ToList();
        }

        private async Task<List<Item>> FilterPublicRecordsAsync(
            HttpClient client, string kind, string orgId, List<Item> candidates,
            PurgeReport report, CancellationToken ct)
        {
            var scoped = new List<Item>();
            foreach (var item in candidates)
            {
                if (!item.IsPublic)
                {
                    scoped.Add(item);
                    continue;
                }

                using var request = NewRequest(HttpMethod.Get, "api/vteam/" + kind + "/" + item.Id);
                using var response = await client.SendAsync(request, ct);
                using var body = await ReadJsonAsync(response, ct);
                if (!body.RootElement.TryGetProperty("model", out var model) ||
                    !model.TryGetProperty("ownerOrganization", out var owner) ||
                    !String.Equals(StringValue(owner, "id"), orgId, StringComparison.OrdinalIgnoreCase))
                {
                    report.SkippedOtherOrganization++;
                    continue;
                }
                scoped.Add(item);
            }
            return scoped;
        }

        private async Task<int> PurgeAsync(
            HttpClient client, string kind, List<Item> items, PurgeReport report,
            CancellationToken ct)
        {
            var completed = 0;
            foreach (var item in items)
            {
                try
                {
                    // An existing soft-deleted item needs only the permanent-delete call.
                    // Normal items need two calls to the exact same existing AI endpoint.
                    var calls = item.IsDeleted ? 1 : 2;
                    for (var attempt = 0; attempt < calls; attempt++)
                    {
                        using var request = NewRequest(HttpMethod.Delete, "api/vteam/" + kind + "/" + item.Id);
                        using var response = await client.SendAsync(request, ct);
                        using var body = await ReadJsonAsync(response, ct);
                    }
                    completed++;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    report.Failures.Add(new PurgeFailure
                    {
                        Type = kind,
                        Id = item.Id,
                        Message = ex.Message
                    });
                }
            }
            return completed;
        }

        private HttpRequestMessage NewRequest(HttpMethod method, string path)
        {
            var result = new HttpRequestMessage(method, path);
            // Forward only incoming identity credentials to this very same loopback API.
            if (Request.Headers.TryGetValue("Authorization", out var auth))
                result.Headers.TryAddWithoutValidation("Authorization", auth.ToArray());
            if (Request.Headers.TryGetValue("Cookie", out var cookie))
                result.Headers.TryAddWithoutValidation("Cookie", cookie.ToArray());
            return result;
        }

        private static bool Bool(JsonElement value, string key) =>
            value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty(key, out var property) &&
            property.ValueKind == JsonValueKind.True;

        private static string StringValue(JsonElement value, string key) =>
            value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty(key, out var property) &&
            property.ValueKind == JsonValueKind.String ? property.GetString() : null;

        private static async Task<JsonDocument> ReadJsonAsync(
            HttpResponseMessage response, CancellationToken ct)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("AI API returned HTTP " + (int)response.StatusCode +
                    ": " + text.Substring(0, Math.Min(400, text.Length)));
            if (String.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("AI API returned an empty result.");

            var json = JsonDocument.Parse(text);
            if (json.RootElement.TryGetProperty("successful", out var successful) &&
                successful.ValueKind == JsonValueKind.False)
            {
                json.Dispose();
                throw new InvalidOperationException("AI API rejected the operation: " +
                    text.Substring(0, Math.Min(400, text.Length)));
            }
            return json;
        }
    }
}
