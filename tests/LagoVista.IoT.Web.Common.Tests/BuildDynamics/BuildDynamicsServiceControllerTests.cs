using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.BuildDynamics;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.IoT.Web.Common.Interfaces.BuildDynamics;
using LagoVista.IoT.Web.Common.Models.BuildDynamics;
using Microsoft.AspNetCore.Mvc;
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
    public class BuildDynamicsServiceControllerTests
    {
        [TestMethod]
        public void Controller_RequiresSignedServiceRequest()
        {
            Assert.IsTrue(typeof(BuildDynamicsServiceController)
                .GetCustomAttributes(typeof(RequireSignedRequestAttribute), inherit: true)
                .Any());
        }

        [TestMethod]
        public async Task UpdateWorkstream_UsesOpaqueClientVersionAndMapsConflict()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Strict);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose);
            ApplicationDataConcurrencyToken captured = null;

            authority.Setup(x => x.UpdateWorkstreamAsync(
                    It.IsAny<WorkstreamAuthorityRecord>(),
                    It.IsAny<ApplicationDataConcurrencyToken>(),
                    It.IsAny<CancellationToken>()))
                .Callback<WorkstreamAuthorityRecord, ApplicationDataConcurrencyToken, CancellationToken>((_, version, __) => captured = version)
                .ReturnsAsync(Mutation(ApplicationDataMutationStatus.Conflict));

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            var result = await controller.UpdateWorkstreamAsync(
                "ORG1",
                "ws-1",
                new BuildDynamicsMutationRequest<WorkstreamAuthorityRecord>
                {
                    ExpectedVersion = "opaque-etag-17",
                    Record = new WorkstreamAuthorityRecord { Name = "Updated" }
                });

            Assert.IsInstanceOfType<ConflictObjectResult>(result);
            Assert.AreEqual("opaque-etag-17", captured.Value);
            authority.VerifyAll();
        }

        [TestMethod]
        public async Task ActivityQuery_ClampsPageAndPreservesOpaqueCursor()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Strict);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose);
            StoragePageRequest captured = null;

            authority.Setup(x => x.QueryActivityAsync(
                    It.IsAny<EntityHeader>(),
                    "ws-1",
                    null,
                    null,
                    It.IsAny<StoragePageRequest>(),
                    It.IsAny<CancellationToken>()))
                .Callback<EntityHeader, string, DateTime?, DateTime?, StoragePageRequest, CancellationToken>((_, __, ___, ____, page, _____) => captured = page)
                .ReturnsAsync(new StoragePageResult<WorkstreamActivityRecord>(
                    new[] { new WorkstreamActivityRecord { Id = "A1", WorkstreamId = "ws-1" } },
                    "next-opaque"));

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            var action = await controller.QueryActivityAsync("ORG1", "ws-1", pageSize: 5000, continuationToken: "prior-opaque");
            var ok = action as OkObjectResult;
            var pageResult = ok?.Value as BuildDynamicsPage<WorkstreamActivityRecord>;

            Assert.IsNotNull(pageResult);
            Assert.AreEqual(1000, captured.PageSize);
            Assert.AreEqual("prior-opaque", captured.ContinuationToken);
            Assert.AreEqual("next-opaque", pageResult.ContinuationToken);
            Assert.AreEqual(1, pageResult.Items.Count);
            authority.VerifyAll();
        }

        [TestMethod]
        public async Task EffectiveRetention_ReturnsProviderNeutralDecision()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Loose);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Strict);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose);
            var scope = EntityHeader.Create("ORG1", "ORG1");
            var record = new StorageRetentionPolicyRecord
            {
                Organization = scope,
                Rules =
                {
                    new StorageRetentionRule
                    {
                        RecordClass = StorageRecordClass.ActivityRecord,
                        Scope = "workstream-history",
                        Ttl = TimeSpan.FromDays(30),
                        SummarizeBeforeExpiry = true
                    }
                }
            };

            retention.Setup(x => x.GetVersionedAsync(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Versioned(record, "retention-v2"));

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            var action = await controller.GetEffectiveRetentionAsync("ORG1", StorageRecordClass.ActivityRecord, "workstream-history");
            var ok = action as OkObjectResult;
            var decision = ok?.Value as BuildDynamicsRetentionDecisionResponse;

            Assert.IsNotNull(decision);
            Assert.AreEqual(TimeSpan.FromDays(30).TotalSeconds, decision.EffectiveTtlSeconds);
            Assert.IsTrue(decision.SummarizeBeforeExpiry);
            Assert.AreEqual("scoped-override", decision.Source);
            retention.VerifyAll();
        }


        [TestMethod]
        public async Task PerformanceQuery_UsesAcceptedTelemetryServiceWithProviderNeutralFilters()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Loose);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Strict);
            System.Collections.Generic.IEnumerable<MetricDimensionFilter> capturedDimensions = null;
            System.Collections.Generic.IEnumerable<string> capturedGroups = null;

            performance.Setup(x => x.QueryBuildDurationPercentileAsync(
                    "ORG1",
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    true,
                    It.IsAny<System.Collections.Generic.IEnumerable<MetricDimensionFilter>>(),
                    It.IsAny<System.Collections.Generic.IEnumerable<string>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, DateTime, DateTime, bool, System.Collections.Generic.IEnumerable<MetricDimensionFilter>, System.Collections.Generic.IEnumerable<string>, CancellationToken>(
                    (_, __, ___, ____, dimensions, groups, _____) =>
                    {
                        capturedDimensions = dimensions.ToArray();
                        capturedGroups = groups.ToArray();
                    })
                .ReturnsAsync(new MetricQueryResult(new[] { new MetricValue(DateTime.UtcNow, 1250) }));

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            var action = await controller.QueryBuildDurationAsync(
                "ORG1",
                DateTime.UtcNow.AddHours(-1),
                DateTime.UtcNow,
                p95: true,
                workstreamId: "build-dynamics-platform",
                repository: "LagoVista/WebCommon",
                groupBy: "repository,task");

            var ok = action as OkObjectResult;
            Assert.IsNotNull(ok);
            Assert.IsInstanceOfType<MetricQueryResult>(ok.Value);
            CollectionAssert.AreEquivalent(
                new[] { "workstream", "repository" },
                capturedDimensions.Select(item => item.Key).ToArray());
            CollectionAssert.AreEquivalent(
                new[] { "repository", "task" },
                capturedGroups.ToArray());
            performance.VerifyAll();
        }

        [TestMethod]
        public async Task InsertWorkspace_PreservesStableIdentityAndScopesOrganization()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Strict);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose);
            WorkspaceAuthorityRecord captured = null;

            authority.Setup(x => x.InsertWorkspaceAsync(It.IsAny<WorkspaceAuthorityRecord>(), It.IsAny<CancellationToken>()))
                .Callback<WorkspaceAuthorityRecord, CancellationToken>((record, _) => captured = record)
                .Returns(Task.CompletedTask);

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            var action = await controller.InsertWorkspaceAsync("ORG1", new WorkspaceAuthorityRecord
            {
                WorkstreamId = "build-dynamics-platform",
                TaskId = "migration-cutover",
                WorkspaceId = "ws-build-dynamics-platform-t007-migration-cutover",
                BranchIdentity = "workspace/build-dynamics-platform/t007-migration-cutover",
                State = "active"
            });

            Assert.IsInstanceOfType<OkResult>(action);
            Assert.IsNotNull(captured);
            Assert.AreEqual("ORG1", captured.Organization.Id);
            Assert.AreEqual("build-dynamics-platform", captured.WorkstreamId);
            Assert.AreEqual("migration-cutover", captured.TaskId);
            Assert.AreEqual("ws-build-dynamics-platform-t007-migration-cutover", captured.WorkspaceId);
            authority.VerifyAll();
        }

        [TestMethod]
        public async Task InsertCoordination_PreservesMigrationStableIdentityAndScope()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Strict);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose);
            WorkstreamCoordinationAuthorityRecord captured = null;

            authority.Setup(x => x.InsertCoordinationAsync(It.IsAny<WorkstreamCoordinationAuthorityRecord>(), It.IsAny<CancellationToken>()))
                .Callback<WorkstreamCoordinationAuthorityRecord, CancellationToken>((record, _) => captured = record)
                .Returns(Task.CompletedTask);

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            var action = await controller.InsertCoordinationAsync("ORG1", new WorkstreamCoordinationAuthorityRecord
            {
                WorkstreamId = "build-dynamics-platform",
                RecordType = "participant-session",
                StableId = "worker:migration-cutover",
                TaskId = "migration-cutover",
                Payload = "{\"conversationUrl\":\"https://chatgpt.com/c/example\"}"
            });

            Assert.IsInstanceOfType<OkResult>(action);
            Assert.IsNotNull(captured);
            Assert.AreEqual("ORG1", captured.Organization.Id);
            Assert.AreEqual("participant-session", captured.RecordType);
            Assert.AreEqual("worker:migration-cutover", captured.StableId);
            Assert.AreEqual("migration-cutover", captured.TaskId);
            authority.VerifyAll();
        }

        [TestMethod]
        public async Task OrchestrationScratch_UsesCallerStableIdForRetryableCutoverState()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Strict);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose);
            WorkstreamOrchestrationScratchRecord captured = null;

            authority.Setup(x => x.UpsertScratchAsync(It.IsAny<WorkstreamOrchestrationScratchRecord>(), It.IsAny<CancellationToken>()))
                .Callback<WorkstreamOrchestrationScratchRecord, CancellationToken>((record, _) => captured = record)
                .Returns(Task.CompletedTask);

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            var action = await controller.UpsertOrchestrationAsync("ORG1", "0123456789ABCDEF0123456789ABCDEF", new WorkstreamOrchestrationScratchRecord
            {
                WorkstreamId = "build-dynamics-platform",
                WorkspaceId = "ws-build-dynamics-platform-t007-migration-cutover",
                Kind = "authority-cutover",
                Value = "{\"phase\":\"reconciled\"}"
            });

            Assert.IsInstanceOfType<OkResult>(action);
            Assert.IsNotNull(captured);
            Assert.AreEqual("ORG1", captured.Organization.Id);
            Assert.AreEqual("0123456789ABCDEF0123456789ABCDEF", captured.Id.Value);
            Assert.AreEqual("authority-cutover", captured.Kind);
            authority.VerifyAll();
        }

        [TestMethod]
        public async Task CurrentAuthorityEndpoints_PreserveStableIdsAndOrganizationScope()
        {
            var authority = new Mock<IWorkstreamAuthorityRepository>(MockBehavior.Strict);
            var aar = new Mock<IAarCompletionRepository>(MockBehavior.Loose);
            var retention = new Mock<IStorageRetentionPolicyStore>(MockBehavior.Loose);
            var performance = new Mock<IBuildPerformanceTelemetryService>(MockBehavior.Loose);
            FixWorkspaceAuthorityRecord fix = null;
            DevOpsActivityAuthorityRecord devOps = null;
            StableFinalizationAuthorityRecord finalization = null;

            authority.Setup(x => x.InsertFixWorkspaceAsync(It.IsAny<FixWorkspaceAuthorityRecord>(), It.IsAny<CancellationToken>()))
                .Callback<FixWorkspaceAuthorityRecord, CancellationToken>((record, _) => fix = record)
                .Returns(Task.CompletedTask);
            authority.Setup(x => x.InsertDevOpsActivityAsync(It.IsAny<DevOpsActivityAuthorityRecord>(), It.IsAny<CancellationToken>()))
                .Callback<DevOpsActivityAuthorityRecord, CancellationToken>((record, _) => devOps = record)
                .Returns(Task.CompletedTask);
            authority.Setup(x => x.InsertFinalizationAsync(It.IsAny<StableFinalizationAuthorityRecord>(), It.IsAny<CancellationToken>()))
                .Callback<StableFinalizationAuthorityRecord, CancellationToken>((record, _) => finalization = record)
                .Returns(Task.CompletedTask);

            var controller = new BuildDynamicsServiceController(authority.Object, aar.Object, retention.Object, performance.Object);
            Assert.IsInstanceOfType<OkResult>(await controller.InsertFixWorkspaceAsync("ORG1", new FixWorkspaceAuthorityRecord
            {
                WorkspaceId = "fix-1",
                ReferenceId = "F001",
                Identifier = "FIX-ONE"
            }));
            Assert.IsInstanceOfType<OkResult>(await controller.InsertDevOpsActivityAsync("ORG1", new DevOpsActivityAuthorityRecord
            {
                ActivityId = "D001",
                OriginWorkstreamId = "build-dynamics-platform"
            }));
            Assert.IsInstanceOfType<OkResult>(await controller.InsertFinalizationAsync("ORG1", new StableFinalizationAuthorityRecord
            {
                FinalizationId = "ABCDEF0123456789ABCDEF0123456789",
                OwnerType = "FixWorkspace",
                OwnerId = "fix-1"
            }));

            Assert.AreEqual("ORG1", fix.Organization.Id);
            Assert.AreEqual("fix-1", fix.WorkspaceId);
            Assert.AreEqual("ORG1", devOps.Organization.Id);
            Assert.AreEqual("D001", devOps.ActivityId);
            Assert.AreEqual("ORG1", finalization.Organization.Id);
            Assert.AreEqual("ABCDEF0123456789ABCDEF0123456789", finalization.FinalizationId);
            authority.VerifyAll();
        }

        private static VersionedApplicationDataRecord<T> Versioned<T>(T record, string version)
            where T : class, IApplicationDataRecord
        {
            return (VersionedApplicationDataRecord<T>)Activator.CreateInstance(
                typeof(VersionedApplicationDataRecord<T>),
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                args: new object[] { record, ApplicationDataConcurrencyToken.FromValue(version) },
                culture: null);
        }

        private static ApplicationDataMutationResult Mutation(ApplicationDataMutationStatus status)
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
