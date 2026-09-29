using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Interfaces.BuildDynamics
{
    public interface IWorkstreamMessageRepository
    {
        Task PublishAsync(WorkstreamMessageRecord record, CancellationToken cancellationToken = default);

        Task<StoragePageResult<WorkstreamMessageRecord>> QueryAsync(
            EntityHeader scope,
            string workstreamId,
            string taskId = null,
            string recipientRole = null,
            string recipientId = null,
            bool attentionOnly = false,
            StoragePageRequest page = null,
            CancellationToken cancellationToken = default);
    }
}
