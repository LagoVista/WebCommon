using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Interfaces.BuildDynamics;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Repos.BuildDynamics
{
    public sealed class WorkstreamMessageRepository : IWorkstreamMessageRepository
    {
        private readonly IActivityRecordStore<WorkstreamMessageRecord> _messages;

        public WorkstreamMessageRepository(IActivityRecordStore<WorkstreamMessageRecord> messages)
        {
            _messages = messages ?? throw new ArgumentNullException(nameof(messages));
        }

        public Task PublishAsync(WorkstreamMessageRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            Required(record.Id, nameof(record.Id));
            Required(record.OrganizationId, nameof(record.OrganizationId));
            Required(record.WorkstreamId, nameof(record.WorkstreamId));
            Required(record.FromRole, nameof(record.FromRole));
            Required(record.ToRole, nameof(record.ToRole));
            Required(record.MessageType, nameof(record.MessageType));
            Required(record.Summary, nameof(record.Summary));

            if (record.AttentionRequired && String.IsNullOrWhiteSpace(record.AttentionAction))
                throw new ArgumentException("AttentionAction is required when AttentionRequired is true.", nameof(record));

            record.Organization = String.IsNullOrWhiteSpace(record.Organization) ? record.OrganizationId : record.Organization;
            record.CreationDate = record.CreationDate == default ? DateTime.UtcNow : record.CreationDate.ToUniversalTime();
            record.ThreadId = String.IsNullOrWhiteSpace(record.ThreadId) ? record.Id : record.ThreadId.Trim();
            record.ReplyToMessageId = record.ReplyToMessageId?.Trim() ?? String.Empty;
            return _messages.InsertAsync(record, cancellationToken);
        }

        public Task<StoragePageResult<WorkstreamMessageRecord>> QueryAsync(
            EntityHeader scope,
            string workstreamId,
            string taskId = null,
            string recipientRole = null,
            string recipientId = null,
            bool attentionOnly = false,
            StoragePageRequest page = null,
            CancellationToken cancellationToken = default)
        {
            var organizationId = Required(scope?.Id, nameof(scope));
            var query = new HistoryQuery<WorkstreamMessageRecord>()
                .Where(x => x.OrganizationId, StorageFilterOperator.Equal, organizationId)
                .Where(x => x.WorkstreamId, StorageFilterOperator.Equal, Required(workstreamId, nameof(workstreamId)))
                .WithPage(page ?? new StoragePageRequest());

            if (!String.IsNullOrWhiteSpace(taskId))
                query.Where(x => x.TaskId, StorageFilterOperator.Equal, taskId.Trim());
            if (!String.IsNullOrWhiteSpace(recipientRole))
                query.Where(x => x.ToRole, StorageFilterOperator.Equal, recipientRole.Trim());
            if (!String.IsNullOrWhiteSpace(recipientId))
                query.Where(x => x.ToId, StorageFilterOperator.Equal, recipientId.Trim());
            if (attentionOnly)
                query.Where(x => x.AttentionRequired, StorageFilterOperator.Equal, true);

            return _messages.QueryAsync(query, cancellationToken);
        }

        private static string Required(string value, string name)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException(name + " is required.", name);
            return value.Trim();
        }
    }
}
