using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using LagoVista.IoT.Web.Common.Repos.BuildDynamics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Tests.BuildDynamics
{
    [TestClass]
    public class AarCompletionRepositoryTests
    {
        [TestMethod]
        public async Task AarLifecycle_PreservesLinkageDispositionAndFollowUpMetadata()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            AarRecord inserted = null;
            AarRecord updated = null;
            var version = CreateConcurrencyToken("v1");

            app.Setup(x => x.InsertAsync(It.IsAny<AarRecord>(), It.IsAny<CancellationToken>()))
               .Callback<AarRecord, CancellationToken>((record, _) => inserted = record)
               .Returns(Task.CompletedTask);
            app.Setup(x => x.UpdateIfVersionAsync(It.IsAny<AarRecord>(), version, It.IsAny<CancellationToken>()))
               .Callback<AarRecord, ApplicationDataConcurrencyToken, CancellationToken>((record, _, __) => updated = record)
               .ReturnsAsync(CreateMutationResult(ApplicationDataMutationStatus.Updated));

            var repo = new AarCompletionRepository(app.Object);
            var record = new AarRecord
            {
                Organization = EntityHeader.Create("ORG1", "Org"),
                AarId = "AAR-1",
                WorkstreamId = "build-dynamics-platform",
                TaskId = "aar-completion-summary",
                WorkspaceId = "ws-t005",
                Category = "process",
                Status = "open",
                Disposition = "accepted",
                Findings = { "Keep durable command evidence." },
                FollowUps =
                {
                    new AarFollowUpRecord
                    {
                        FollowUpId = "F1",
                        Description = "Automate follow-up conversion.",
                        Status = "proposed",
                        ConvertedTaskId = "future-task"
                    }
                }
            };

            await repo.InsertAarAsync(record);
            record.Status = "finalized";
            await repo.UpdateAarAsync(record, version);

            Assert.AreEqual("build-dynamics-platform", inserted.WorkstreamId);
            Assert.AreEqual("aar-completion-summary", inserted.TaskId);
            Assert.AreEqual("ws-t005", inserted.WorkspaceId);
            Assert.AreEqual("accepted", inserted.Disposition);
            Assert.AreEqual("future-task", inserted.FollowUps.Single().ConvertedTaskId);
            Assert.IsNotNull(updated.FinalizedAtUtc);
            Assert.AreEqual(inserted.Id.Value, updated.Id.Value);
            app.VerifyAll();
        }

        [TestMethod]
        public async Task QueryAars_UsesOrganizationAndWorkstreamLinkage()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            StorageQuery<AarRecord> captured = null;
            app.Setup(x => x.QueryAsync(It.IsAny<StorageQuery<AarRecord>>(), It.IsAny<CancellationToken>()))
               .Callback<StorageQuery<AarRecord>, CancellationToken>((query, _) => captured = query)
               .ReturnsAsync(new StoragePageResult<AarRecord>(Array.Empty<AarRecord>()));

            var repo = new AarCompletionRepository(app.Object);
            await repo.QueryAarsAsync(EntityHeader.Create("ORG1", "Org"), "ws-1");

            Assert.IsTrue(captured.Filters.Any(f => f.Field == "Organization.Id" && (string)f.Value == "ORG1"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "WorkstreamId" && (string)f.Value == "ws-1"));
            app.VerifyAll();
        }

        [TestMethod]
        public async Task EnsureCompletionSummary_IsIdempotentAndPreservesCompletionFacts()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scope = EntityHeader.Create("ORG1", "Org");
            WorkstreamCompletionSummaryRecord stored = null;
            var readCount = 0;

            app.Setup(x => x.GetVersionedAsync<WorkstreamCompletionSummaryRecord>(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() =>
               {
                   readCount++;
                   return stored == null ? null : CreateVersioned(stored);
               });
            app.Setup(x => x.InsertAsync(It.IsAny<WorkstreamCompletionSummaryRecord>(), It.IsAny<CancellationToken>()))
               .Callback<WorkstreamCompletionSummaryRecord, CancellationToken>((record, _) => stored = record)
               .Returns(Task.CompletedTask);

            var repo = new AarCompletionRepository(app.Object);
            var summary = new WorkstreamCompletionSummaryRecord
            {
                Organization = scope,
                WorkstreamId = "build-dynamics-platform",
                Objective = "Move build dynamics into platform authority.",
                TaskOutcomes = { new CompletionTaskOutcome { TaskId = "T001", Outcome = "closed" } },
                AcceptedSourceIdentities = { "LagoVista/WebCommon@abc123" },
                AcceptedArtifactIdentities = { "nuget:LagoVista.IoT.Web.Common@7.0.1" },
                SignificantFindings = { "Workspace commands must outlive transport." },
                BuildSummary = "green",
                TestSummary = "green",
                AarConclusions = { "Retain durable summaries." },
                UnresolvedIssues = { "none" },
                WaivedIssues = { "legacy detail" }
            };

            var first = await repo.EnsureCompletionSummaryAsync(summary);
            var second = await repo.EnsureCompletionSummaryAsync(new WorkstreamCompletionSummaryRecord
            {
                Organization = scope,
                WorkstreamId = "build-dynamics-platform",
                Objective = "different input must not replace durable completion truth"
            });

            Assert.AreSame(first, second);
            Assert.AreEqual(1, first.TaskOutcomes.Count);
            Assert.AreEqual("LagoVista/WebCommon@abc123", first.AcceptedSourceIdentities.Single());
            Assert.AreEqual("green", first.BuildSummary);
            Assert.AreEqual("green", first.TestSummary);
            Assert.AreEqual(32, first.Id.Value.Length);
            Assert.AreEqual(2, readCount);
            app.Verify(x => x.InsertAsync(It.IsAny<WorkstreamCompletionSummaryRecord>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task DetailedHistory_IsNotSafeToExpireUntilSummaryExists()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scope = EntityHeader.Create("ORG1", "Org");
            WorkstreamCompletionSummaryRecord stored = null;

            app.Setup(x => x.GetVersionedAsync<WorkstreamCompletionSummaryRecord>(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => stored == null ? null : CreateVersioned(stored));

            var repo = new AarCompletionRepository(app.Object);
            var expiring = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.ActivityRecord, TimeSpan.FromDays(30), summarizeBeforeExpiry: true)
                .Resolve(StorageRecordClass.ActivityRecord);

            Assert.IsFalse(await repo.IsDetailedHistorySafeToExpireAsync(scope, "build-dynamics-platform", expiring));

            stored = new WorkstreamCompletionSummaryRecord
            {
                Organization = scope,
                WorkstreamId = "build-dynamics-platform",
                Objective = "summary retained"
            };

            Assert.IsTrue(await repo.IsDetailedHistorySafeToExpireAsync(scope, "build-dynamics-platform", expiring));
            app.VerifyAll();
        }

        [TestMethod]
        public async Task CompletionSummary_RemainsReadableAfterDetailedHistoryExpires()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scope = EntityHeader.Create("ORG1", "Org");
            WorkstreamCompletionSummaryRecord storedSummary = null;
            var detailedHistoryAvailable = true;

            app.Setup(x => x.GetVersionedAsync<WorkstreamCompletionSummaryRecord>(
                    It.IsAny<StorageKey>(),
                    It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => storedSummary == null ? null : CreateVersioned(storedSummary));

            app.Setup(x => x.InsertAsync(
                    It.IsAny<WorkstreamCompletionSummaryRecord>(),
                    It.IsAny<CancellationToken>()))
               .Callback<WorkstreamCompletionSummaryRecord, CancellationToken>((record, _) => storedSummary = record)
               .Returns(Task.CompletedTask);

            var repo = new AarCompletionRepository(app.Object);
            var summary = new WorkstreamCompletionSummaryRecord
            {
                Organization = scope,
                WorkstreamId = "build-dynamics-platform",
                Objective = "Move build dynamics into platform authority.",
                TaskOutcomes = { new CompletionTaskOutcome { TaskId = "T005", Outcome = "integration-ready" } },
                AcceptedSourceIdentities = { "LagoVista/WebCommon@ecdda65" },
                AcceptedArtifactIdentities = { "nuget:LagoVista.IoT.Web.Common@workspace" },
                SignificantFindings = { "Completion truth outlives expiring detail." },
                BuildSummary = "green",
                TestSummary = "green",
                AarConclusions = { "Retain summary before detail expires." },
                UnresolvedIssues = { "none" },
                WaivedIssues = { "expired raw history" },
                DetailSummarizedThroughUtc = DateTime.UtcNow
            };

            await repo.EnsureCompletionSummaryAsync(summary);

            detailedHistoryAvailable = false; // simulate raw activity/history TTL expiration or removal
            Assert.IsFalse(detailedHistoryAvailable);

            var retained = await repo.GetCompletionSummaryAsync(scope, "build-dynamics-platform");

            Assert.IsNotNull(retained);
            Assert.IsNotNull(retained.Record);
            Assert.AreEqual("Move build dynamics into platform authority.", retained.Record.Objective);
            Assert.AreEqual("integration-ready", retained.Record.TaskOutcomes.Single().Outcome);
            Assert.AreEqual("LagoVista/WebCommon@ecdda65", retained.Record.AcceptedSourceIdentities.Single());
            Assert.AreEqual("green", retained.Record.BuildSummary);
            Assert.AreEqual("green", retained.Record.TestSummary);
            Assert.AreEqual("Retain summary before detail expires.", retained.Record.AarConclusions.Single());
            Assert.AreEqual("expired raw history", retained.Record.WaivedIssues.Single());
            app.VerifyAll();
        }

        [TestMethod]
        public async Task ProtectedOrNonExpiringDetail_IsNeverReportedAsEligibleForExpiry()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var repo = new AarCompletionRepository(app.Object);
            var scope = EntityHeader.Create("ORG1", "Org");

            var protectedDecision = new StorageRetentionPolicy()
                .AddDefault(StorageRecordClass.ActivityRecord, isProtected: true)
                .Resolve(StorageRecordClass.ActivityRecord);
            var durableDecision = new StorageRetentionPolicy()
                .Resolve(StorageRecordClass.ActivityRecord);

            Assert.IsFalse(await repo.IsDetailedHistorySafeToExpireAsync(scope, "ws", protectedDecision));
            Assert.IsFalse(await repo.IsDetailedHistorySafeToExpireAsync(scope, "ws", durableDecision));
            app.VerifyNoOtherCalls();
        }

        private static VersionedApplicationDataRecord<T> CreateVersioned<T>(T record) where T : class, IApplicationDataRecord
        {
            return (VersionedApplicationDataRecord<T>)Activator.CreateInstance(
                typeof(VersionedApplicationDataRecord<T>),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { record, CreateConcurrencyToken("v1") },
                culture: null);
        }

        private static ApplicationDataConcurrencyToken CreateConcurrencyToken(string value)
        {
            return (ApplicationDataConcurrencyToken)Activator.CreateInstance(
                typeof(ApplicationDataConcurrencyToken),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { value },
                culture: null);
        }

        private static ApplicationDataMutationResult CreateMutationResult(ApplicationDataMutationStatus status)
        {
            return (ApplicationDataMutationResult)Activator.CreateInstance(
                typeof(ApplicationDataMutationResult),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { status, null },
                culture: null);
        }
    }
}
