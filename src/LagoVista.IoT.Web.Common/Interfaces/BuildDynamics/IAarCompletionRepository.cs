using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces.BuildDynamics
{
    public interface IAarCompletionRepository
    {
        Task InsertAarAsync(AarRecord record, CancellationToken cancellationToken = default);
        Task<VersionedApplicationDataRecord<AarRecord>> GetAarAsync(EntityHeader scope, string aarId, CancellationToken cancellationToken = default);
        Task<ApplicationDataMutationResult> UpdateAarAsync(AarRecord record, ApplicationDataConcurrencyToken expectedVersion, CancellationToken cancellationToken = default);
        Task<StoragePageResult<AarRecord>> QueryAarsAsync(EntityHeader scope, string workstreamId, StoragePageRequest page = null, CancellationToken cancellationToken = default);

        Task<VersionedApplicationDataRecord<WorkstreamCompletionSummaryRecord>> GetCompletionSummaryAsync(EntityHeader scope, string workstreamId, CancellationToken cancellationToken = default);
        Task<WorkstreamCompletionSummaryRecord> EnsureCompletionSummaryAsync(WorkstreamCompletionSummaryRecord summary, CancellationToken cancellationToken = default);
        Task<bool> IsDetailedHistorySafeToExpireAsync(EntityHeader scope, string workstreamId, StorageRetentionDecision detailRetention, CancellationToken cancellationToken = default);
    }
}
