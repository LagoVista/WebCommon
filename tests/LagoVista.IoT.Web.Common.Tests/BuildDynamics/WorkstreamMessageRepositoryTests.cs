using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using LagoVista.IoT.Web.Common.Repos.BuildDynamics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Tests.BuildDynamics
{
    [TestClass]
    public class WorkstreamMessageRepositoryTests
    {
        [TestMethod]
        public async Task Publish_PreservesStableIdentityAndThreadSemantics()
        {
            var store = new Mock<IActivityRecordStore<WorkstreamMessageRecord>>(MockBehavior.Strict);
            WorkstreamMessageRecord inserted = null;
            store.Setup(x => x.InsertAsync(It.IsAny<WorkstreamMessageRecord>(), It.IsAny<CancellationToken>()))
                .Callback<WorkstreamMessageRecord, CancellationToken>((record, _) => inserted = record)
                .Returns(Task.CompletedTask);

            var repo = new WorkstreamMessageRepository(store.Object);
            await repo.PublishAsync(new WorkstreamMessageRecord
            {
                Id = "MSG-001",
                OrganizationId = "ORG1",
                WorkstreamId = "build-dynamics-platform",
                TaskId = "signed-workstream-message-api",
                WorkspaceId = "ws-13",
                FromRole = "worker",
                ToRole = "orchestrator",
                MessageType = "status",
                Summary = "ready"
            });

            Assert.AreEqual("MSG-001", inserted.Id);
            Assert.AreEqual("MSG-001", inserted.ThreadId);
            Assert.AreEqual("ORG1", inserted.Organization);
            Assert.AreNotEqual(default, inserted.CreationDate);
            store.VerifyAll();
        }

        [TestMethod]
        public async Task QueryInbox_FiltersRecipientTaskAndAttention()
        {
            var store = new Mock<IActivityRecordStore<WorkstreamMessageRecord>>(MockBehavior.Strict);
            HistoryQuery<WorkstreamMessageRecord> captured = null;
            store.Setup(x => x.QueryAsync(It.IsAny<HistoryQuery<WorkstreamMessageRecord>>(), It.IsAny<CancellationToken>()))
                .Callback<HistoryQuery<WorkstreamMessageRecord>, CancellationToken>((query, _) => captured = query)
                .ReturnsAsync(new StoragePageResult<WorkstreamMessageRecord>(Array.Empty<WorkstreamMessageRecord>(), "next"));

            var repo = new WorkstreamMessageRepository(store.Object);
            var result = await repo.QueryAsync(
                EntityHeader.Create("ORG1", "ORG1"),
                "build-dynamics-platform",
                taskId: "signed-workstream-message-api",
                recipientRole: "worker",
                recipientId: "worker-7",
                attentionOnly: true,
                page: new StoragePageRequest(25, "prior"));

            Assert.AreEqual("next", result.ContinuationToken);
            Assert.AreEqual(25, captured.Page.PageSize);
            Assert.AreEqual("prior", captured.Page.ContinuationToken);
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "OrganizationId" && (string)f.Value == "ORG1"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "WorkstreamId" && (string)f.Value == "build-dynamics-platform"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "TaskId" && (string)f.Value == "signed-workstream-message-api"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "ToRole" && (string)f.Value == "worker"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "ToId" && (string)f.Value == "worker-7"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "AttentionRequired" && (bool)f.Value));
            store.VerifyAll();
        }

        [TestMethod]
        public async Task Publish_RequiresAttentionActionForActionableMessage()
        {
            var repo = new WorkstreamMessageRepository(new Mock<IActivityRecordStore<WorkstreamMessageRecord>>(MockBehavior.Loose).Object);
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => repo.PublishAsync(new WorkstreamMessageRecord
            {
                Id = "MSG-002",
                OrganizationId = "ORG1",
                WorkstreamId = "build-dynamics-platform",
                FromRole = "worker",
                ToRole = "orchestrator",
                MessageType = "decision-request",
                Summary = "Need decision",
                AttentionRequired = true
            }));
        }
    }
}
