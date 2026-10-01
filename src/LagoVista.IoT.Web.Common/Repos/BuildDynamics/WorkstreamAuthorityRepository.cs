using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Interfaces.BuildDynamics;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Repos.BuildDynamics
{
    public sealed class WorkstreamAuthorityRepository : IWorkstreamAuthorityRepository
    {
        private readonly IApplicationDataStore _applicationData;
        private readonly IScratchStore _scratch;
        private readonly IActivityRecordStore<WorkstreamActivityRecord> _activity;

        public WorkstreamAuthorityRepository(IApplicationDataStore applicationData, IScratchStore scratch, IActivityRecordStore<WorkstreamActivityRecord> activity)
        {
            _applicationData = applicationData ?? throw new ArgumentNullException(nameof(applicationData));
            _scratch = scratch ?? throw new ArgumentNullException(nameof(scratch));
            _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        }

        public Task InsertWorkstreamAsync(WorkstreamAuthorityRecord record, CancellationToken cancellationToken = default) =>
            _applicationData.InsertAsync(Prepare(record, "workstream", record?.WorkstreamId), cancellationToken);

        public Task<VersionedApplicationDataRecord<WorkstreamAuthorityRecord>> GetWorkstreamAsync(EntityHeader scope, string workstreamId, CancellationToken cancellationToken = default) =>
            _applicationData.GetVersionedAsync<WorkstreamAuthorityRecord>(Key(scope, "workstream", workstreamId), cancellationToken);

        public Task<ApplicationDataMutationResult> UpdateWorkstreamAsync(WorkstreamAuthorityRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default) =>
            _applicationData.UpdateIfVersionAsync(Prepare(record, "workstream", record?.WorkstreamId), expectedVersion, cancellationToken);

        public Task InsertTaskAsync(TaskAuthorityRecord record, CancellationToken cancellationToken = default) =>
            _applicationData.InsertAsync(Prepare(record, "task", record?.TaskId), cancellationToken);

        public Task<VersionedApplicationDataRecord<TaskAuthorityRecord>> GetTaskAsync(EntityHeader scope, string taskId, CancellationToken cancellationToken = default) =>
            _applicationData.GetVersionedAsync<TaskAuthorityRecord>(Key(scope, "task", taskId), cancellationToken);

        public Task<ApplicationDataMutationResult> UpdateTaskAsync(TaskAuthorityRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default) =>
            _applicationData.UpdateIfVersionAsync(Prepare(record, "task", record?.TaskId), expectedVersion, cancellationToken);

        public Task InsertWorkspaceAsync(WorkspaceAuthorityRecord record, CancellationToken cancellationToken = default) =>
            _applicationData.InsertAsync(Prepare(record, "workspace", record?.WorkspaceId), cancellationToken);

        public Task<VersionedApplicationDataRecord<WorkspaceAuthorityRecord>> GetWorkspaceAsync(EntityHeader scope, string workspaceId, CancellationToken cancellationToken = default) =>
            _applicationData.GetVersionedAsync<WorkspaceAuthorityRecord>(Key(scope, "workspace", workspaceId), cancellationToken);

        public Task<ApplicationDataMutationResult> UpdateWorkspaceAsync(WorkspaceAuthorityRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default) =>
            _applicationData.UpdateIfVersionAsync(Prepare(record, "workspace", record?.WorkspaceId), expectedVersion, cancellationToken);

        public Task<StoragePageResult<TaskAuthorityRecord>> QueryTasksAsync(EntityHeader scope, string workstreamId, StoragePageRequest page = null, CancellationToken cancellationToken = default)
        {
            RequireScope(scope);
            var query = new StorageQuery<TaskAuthorityRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, scope.Id)
                .Where(x => x.WorkstreamId, StorageFilterOperator.Equal, Required(workstreamId, nameof(workstreamId)))
                .WithPage(page ?? new StoragePageRequest());
            return _applicationData.QueryAsync(query, cancellationToken);
        }

        public Task AppendActivityAsync(WorkstreamActivityRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            record.Id = String.IsNullOrWhiteSpace(record.Id) ? Guid.NewGuid().ToString("N").ToUpperInvariant() : record.Id;
            record.CreationDate = record.CreationDate == default ? DateTime.UtcNow : record.CreationDate.ToUniversalTime();
            return _activity.InsertAsync(record, cancellationToken);
        }

        public Task<StoragePageResult<WorkstreamActivityRecord>> QueryActivityAsync(EntityHeader scope, string workstreamId, DateTime? startUtc, DateTime? endUtc, StoragePageRequest page = null, CancellationToken cancellationToken = default)
        {
            RequireScope(scope);
            var query = new HistoryQuery<WorkstreamActivityRecord>()
                .Between(startUtc, endUtc)
                .Where(x => x.OrganizationId, StorageFilterOperator.Equal, scope.Id)
                .Where(x => x.WorkstreamId, StorageFilterOperator.Equal, Required(workstreamId, nameof(workstreamId)))
                .WithPage(page ?? new StoragePageRequest());
            return _activity.QueryAsync(query, cancellationToken);
        }

        public Task InsertCoordinationAsync(WorkstreamCoordinationAuthorityRecord record, CancellationToken cancellationToken = default) =>
            _applicationData.InsertAsync(Prepare(record, CoordinationKind(record?.RecordType), record?.StableId), cancellationToken);

        public Task<VersionedApplicationDataRecord<WorkstreamCoordinationAuthorityRecord>> GetCoordinationAsync(EntityHeader scope, string recordType, string stableId, CancellationToken cancellationToken = default) =>
            _applicationData.GetVersionedAsync<WorkstreamCoordinationAuthorityRecord>(Key(scope, CoordinationKind(recordType), stableId), cancellationToken);

        public Task<ApplicationDataMutationResult> UpdateCoordinationAsync(WorkstreamCoordinationAuthorityRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default) =>
            _applicationData.UpdateIfVersionAsync(Prepare(record, CoordinationKind(record?.RecordType), record?.StableId), expectedVersion, cancellationToken);

        public Task<StoragePageResult<WorkstreamCoordinationAuthorityRecord>> QueryCoordinationAsync(EntityHeader scope, string workstreamId, string recordType, StoragePageRequest page = null, CancellationToken cancellationToken = default)
        {
            RequireScope(scope);
            var query = new StorageQuery<WorkstreamCoordinationAuthorityRecord>()
                .Where(x => x.Organization.Id, StorageFilterOperator.Equal, scope.Id)
                .Where(x => x.WorkstreamId, StorageFilterOperator.Equal, Required(workstreamId, nameof(workstreamId)))
                .Where(x => x.RecordType, StorageFilterOperator.Equal, Required(recordType, nameof(recordType)))
                .WithPage(page ?? new StoragePageRequest());
            return _applicationData.QueryAsync(query, cancellationToken);
        }

        public Task UpsertScratchAsync(WorkstreamOrchestrationScratchRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireScope(record.Organization);
            if (String.IsNullOrWhiteSpace(record.Id.Value))
                throw new ArgumentException("Scratch Id is required.", nameof(record));
            return _scratch.UpsertAsync(record, cancellationToken);
        }

        public Task<WorkstreamOrchestrationScratchRecord> GetScratchAsync(EntityHeader scope, string scratchId, CancellationToken cancellationToken = default) =>
            _scratch.GetAsync<WorkstreamOrchestrationScratchRecord>(new StorageKey(Required(scratchId, nameof(scratchId)), RequireScope(scope).Id), cancellationToken);

        public Task DeleteScratchAsync(EntityHeader scope, string scratchId, CancellationToken cancellationToken = default) =>
            _scratch.DeleteAsync<WorkstreamOrchestrationScratchRecord>(new StorageKey(Required(scratchId, nameof(scratchId)), RequireScope(scope).Id), cancellationToken);

        private static string CoordinationKind(string recordType) =>
            "coordination:" + Required(recordType, nameof(recordType)).Trim().ToLowerInvariant();

        private static T Prepare<T>(T record, string kind, string stableId) where T : BuildDynamicsApplicationRecord
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            RequireScope(record.Organization);
            record.Id = new NormalizedId32(StorageId(kind, stableId));
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
