using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces.BuildDynamics
{
    public interface IWorkstreamAuthorityRepository
    {
        Task InsertWorkstreamAsync(WorkstreamAuthorityRecord record, CancellationToken cancellationToken = default);
        Task<VersionedApplicationDataRecord<WorkstreamAuthorityRecord>> GetWorkstreamAsync(EntityHeader scope, string workstreamId, CancellationToken cancellationToken = default);
        Task<ApplicationDataMutationResult> UpdateWorkstreamAsync(WorkstreamAuthorityRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default);

        Task InsertTaskAsync(TaskAuthorityRecord record, CancellationToken cancellationToken = default);
        Task<VersionedApplicationDataRecord<TaskAuthorityRecord>> GetTaskAsync(EntityHeader scope, string taskId, CancellationToken cancellationToken = default);
        Task<ApplicationDataMutationResult> UpdateTaskAsync(TaskAuthorityRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default);

        Task InsertWorkspaceAsync(WorkspaceAuthorityRecord record, CancellationToken cancellationToken = default);
        Task<VersionedApplicationDataRecord<WorkspaceAuthorityRecord>> GetWorkspaceAsync(EntityHeader scope, string workspaceId, CancellationToken cancellationToken = default);
        Task<ApplicationDataMutationResult> UpdateWorkspaceAsync(WorkspaceAuthorityRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default);

        Task<StoragePageResult<TaskAuthorityRecord>> QueryTasksAsync(EntityHeader scope, string workstreamId, StoragePageRequest page = null, CancellationToken cancellationToken = default);
        Task AppendActivityAsync(WorkstreamActivityRecord record, CancellationToken cancellationToken = default);
        Task<StoragePageResult<WorkstreamActivityRecord>> QueryActivityAsync(EntityHeader scope, string workstreamId, DateTime? startUtc, DateTime? endUtc, StoragePageRequest page = null, CancellationToken cancellationToken = default);

        Task UpsertScratchAsync(WorkstreamOrchestrationScratchRecord record, CancellationToken cancellationToken = default);
        Task<WorkstreamOrchestrationScratchRecord> GetScratchAsync(EntityHeader scope, string scratchId, CancellationToken cancellationToken = default);
        Task DeleteScratchAsync(EntityHeader scope, string scratchId, CancellationToken cancellationToken = default);
    }
}
