using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Interfaces.BuildDynamics;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Repos.BuildDynamics
{
    public sealed class AarCompletionRepository : IAarCompletionRepository
    {
        private readonly IApplicationDataStore _applicationData;

        public AarCompletionRepository(IApplicationDataStore applicationData)
        {
            _applicationData = applicationData ?? throw new ArgumentNullException(nameof(applicationData));
        }

        public Task InsertAarAsync(AarRecord record, CancellationToken cancellationToken = default) =>
            _applicationData.InsertAsync(PrepareAar(record), cancellationToken);

        public Task<VersionedApplicationDataRecord<AarRecord>> GetAarAsync(EntityHeader scope, string aarId, CancellationToken cancellationToken = default) =>
            _applicationData.GetVersionedAsync<AarRecord>(Key(scope, "aar", aarId), cancellationToken);

        public Task<ApplicationDataMutationResult> UpdateAarAsync(AarRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default) =>
            _applicationData.UpdateIfVersionAsync(PrepareAar(record), expectedVersion, cancellationToken);

        public Task<StoragePageResult<AarRecord>> QueryAarsAsync(EntityHeader scope, string workstreamId, StoragePageRequest page = null, CancellationToken cancellationToken = default)
        {
            RequireScope(scope);
            var query = new StorageQuery<AarRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, scope.Id)
                .Where(x => x.WorkstreamId, StorageFilterOperator.Equal, Required(workstreamId, nameof(workstreamId)))
                .WithPage(page ?? new StoragePageRequest());

            return _applicationData.QueryAsync(query, cancellationToken);
        }

        public Task<VersionedApplicationDataRecord<WorkstreamCompletionSummaryRecord>> GetCompletionSummaryAsync(EntityHeader scope, string workstreamId, CancellationToken cancellationToken = default) =>
            _applicationData.GetVersionedAsync<WorkstreamCompletionSummaryRecord>(Key(scope, "completion-summary", workstreamId), cancellationToken);

        public async Task<WorkstreamCompletionSummaryRecord> EnsureCompletionSummaryAsync(WorkstreamCompletionSummaryRecord summary, CancellationToken cancellationToken = default)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            RequireScope(summary.Organization);
            Required(summary.WorkstreamId, nameof(summary.WorkstreamId));

            var existing = await GetCompletionSummaryAsync(summary.Organization, summary.WorkstreamId, cancellationToken).ConfigureAwait(false);
            if (existing?.Record != null)
                return existing.Record;

            summary.Id = new NormalizedId32(StorageId("completion-summary", summary.WorkstreamId));
            summary.GeneratedAtUtc = summary.GeneratedAtUtc == default ? DateTime.UtcNow : summary.GeneratedAtUtc.ToUniversalTime();
            await _applicationData.InsertAsync(summary, cancellationToken).ConfigureAwait(false);
            return summary;
        }

        public async Task<bool> IsDetailedHistorySafeToExpireAsync(EntityHeader scope, string workstreamId, StorageRetentionDecision detailRetention, CancellationToken cancellationToken = default)
        {
            RequireScope(scope);
            Required(workstreamId, nameof(workstreamId));
            if (detailRetention == null) throw new ArgumentNullException(nameof(detailRetention));

            if (detailRetention.IsProtected || !detailRetention.EffectiveTtl.HasValue)
                return false;

            var summary = await GetCompletionSummaryAsync(scope, workstreamId, cancellationToken).ConfigureAwait(false);
            return summary?.Record != null;
        }

        private static AarRecord PrepareAar(AarRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireScope(record.Organization);
            Required(record.AarId, nameof(record.AarId));
            Required(record.WorkstreamId, nameof(record.WorkstreamId));

            record.Id = new NormalizedId32(StorageId("aar", record.AarId));
            if (String.Equals(record.Status, "finalized", StringComparison.OrdinalIgnoreCase) && !record.FinalizedAtUtc.HasValue)
                record.FinalizedAtUtc = DateTime.UtcNow;
            else if (record.FinalizedAtUtc.HasValue)
                record.FinalizedAtUtc = record.FinalizedAtUtc.Value.ToUniversalTime();

            record.Findings = record.Findings?.Where(item => !String.IsNullOrWhiteSpace(item)).ToList() ?? new System.Collections.Generic.List<string>();
            record.FollowUps = record.FollowUps ?? new System.Collections.Generic.List<AarFollowUpRecord>();
            return record;
        }

        private static StorageKey Key(EntityHeader scope, string kind, string stableId) =>
            new StorageKey(StorageId(kind, stableId), RequireScope(scope).Id);

        private static EntityHeader RequireScope(EntityHeader scope)
        {
            if (scope == null || String.IsNullOrWhiteSpace(scope.Id))
                throw new ArgumentException("Organization/system scope is required.", nameof(scope));
            return scope;
        }

        private static string Required(string value, string name)
        {
            if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required.", name);
            return value;
        }

        private static string StorageId(string kind, string stableId)
        {
            var value = kind + ":" + Required(stableId, nameof(stableId));
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            var hex = BitConverter.ToString(bytes).Replace("-", String.Empty);
            return hex.Substring(0, 32);
        }
    }
}
