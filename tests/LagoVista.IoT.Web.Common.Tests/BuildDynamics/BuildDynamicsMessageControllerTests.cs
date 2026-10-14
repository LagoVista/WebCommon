using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Web.Common.BuildDynamics;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.IoT.Web.Common.Interfaces.BuildDynamics;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Tests.BuildDynamics
{
    [TestClass]
    public class BuildDynamicsMessageControllerTests
    {
        [TestMethod]
        public async Task PublishMessage_PreservesReplyMetadata()
        {
            var messages = new Mock<IWorkstreamMessageRepository>(MockBehavior.Strict);
            WorkstreamMessageRecord captured = null;
            messages.Setup(x => x.PublishAsync(It.IsAny<WorkstreamMessageRecord>(), It.IsAny<CancellationToken>()))
                .Callback<WorkstreamMessageRecord, CancellationToken>((record, _) => captured = record)
                .Returns(Task.CompletedTask);

            var controller = CreateController(messages.Object);
            var result = await controller.PublishMessageAsync("ORG1", "build-dynamics-platform", new WorkstreamMessageRecord
            {
                Id = "MSG-003",
                TaskId = "signed-workstream-message-api",
                WorkspaceId = "ws-13",
                FromRole = "worker",
                ToRole = "orchestrator",
                MessageType = "answer",
                Summary = "reply",
                ThreadId = "THREAD-1",
                ReplyToMessageId = "MSG-001"
            });

            Assert.IsInstanceOfType<OkResult>(result);
            Assert.AreEqual("ORG1", captured.OrganizationId);
            Assert.AreEqual("build-dynamics-platform", captured.WorkstreamId);
            Assert.AreEqual("THREAD-1", captured.ThreadId);
            Assert.AreEqual("MSG-001", captured.ReplyToMessageId);
            messages.VerifyAll();
        }

        [TestMethod]
        public async Task Inbox_IsRecipientScopedAttentionReadAndClampsPage()
        {
            var messages = new Mock<IWorkstreamMessageRepository>(MockBehavior.Strict);
            StoragePageRequest capturedPage = null;
            messages.Setup(x => x.QueryAsync(
                    It.IsAny<LagoVista.Core.Models.EntityHeader>(),
                    "build-dynamics-platform",
                    "signed-workstream-message-api",
                    "worker",
                    "worker-7",
                    true,
                    It.IsAny<StoragePageRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<LagoVista.Core.Models.EntityHeader, string, string, string, string, bool, StoragePageRequest, CancellationToken>(
                    (_, __, ___, ____, _____, ______, page, _______) => capturedPage = page)
                .ReturnsAsync(new StoragePageResult<WorkstreamMessageRecord>(
                    new[] { new WorkstreamMessageRecord { Id = "MSG-004", AttentionRequired = true } },
                    "next"));

            var controller = CreateController(messages.Object);
            var result = await controller.QueryInboxAsync(
                "ORG1",
                "build-dynamics-platform",
                "worker",
                recipientId: "worker-7",
                taskId: "signed-workstream-message-api",
                pageSize: 5000,
                continuationToken: "prior");

            var ok = result as OkObjectResult;
            var page = ok?.Value as BuildDynamicsPage<WorkstreamMessageRecord>;
            Assert.IsNotNull(page);
            Assert.AreEqual(1000, capturedPage.PageSize);
            Assert.AreEqual("prior", capturedPage.ContinuationToken);
            Assert.AreEqual("next", page.ContinuationToken);
            Assert.AreEqual("MSG-004", page.Items.Single().Id);
            messages.VerifyAll();
        }

        [TestMethod]
        public async Task PublishMessage_RejectsMissingAttentionAction()
        {
            var controller = CreateController(new Mock<IWorkstreamMessageRepository>(MockBehavior.Loose).Object);
            var result = await controller.PublishMessageAsync("ORG1", "build-dynamics-platform", new WorkstreamMessageRecord
            {
                Id = "MSG-005",
                FromRole = "worker",
                ToRole = "orchestrator",
                MessageType = "blocker",
                Summary = "blocked",
                AttentionRequired = true
            });

            Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        }

        private static BuildDynamicsServiceController CreateController(IWorkstreamMessageRepository messages)
        {
            return new BuildDynamicsServiceController(
                new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Loose).Object,
                new Mock<IAarCompletionRepository>(MockBehavior.Loose).Object,
                new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose).Object,
                new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose).Object,
                messages);
        }
    }
}
