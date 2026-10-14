using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
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
    public class WorkstreamAuthorityRepositoryTests
    {
        [TestMethod]
        public async Task InsertAndVersionedUpdate_PreserveStableIdentityAndUseConditionalMutation()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scratch = new Mock<IScratchStore>(MockBehavior.Loose);
            var activity = new Mock<IActivityRecordStore<WorkstreamActivityRecord>>(MockBehavior.Loose);
            var scope = EntityHeader.Create("ORG123", "Org");
            WorkstreamAuthorityRecord inserted = null;
            WorkstreamAuthorityRecord updated = null;

            app.Setup(x => x.InsertAsync(It.IsAny<WorkstreamAuthorityRecord>(), It.IsAny<CancellationToken>()))
               .Callback<WorkstreamAuthorityRecord, CancellationToken>((r, _) => inserted = r)
               .Returns(Task.CompletedTask);
            app.Setup(x => x.UpdateIfVersionAsync(It.IsAny<WorkstreamAuthorityRecord>(), null, It.IsAny<CancellationToken>()))
               .Callback<WorkstreamAuthorityRecord, ApplicationDataConcurrencyToken, CancellationToken>((r, _, __) => updated = r)
               .ReturnsAsync((ApplicationDataMutationResult)null);

            var repo = new WorkstreamAuthorityRepository(app.Object, scratch.Object, activity.Object);
            var record = new WorkstreamAuthorityRecord { Organization = scope, WorkstreamId = "build-dynamics-platform", Name = "Build Dynamics" };

            await repo.InsertWorkstreamAsync(record);
            await repo.UpdateWorkstreamAsync(record, null);

            Assert.AreEqual("build-dynamics-platform", inserted.WorkstreamId);
            Assert.AreEqual(inserted.Id.Value, updated.Id.Value);
            Assert.AreEqual(32, inserted.Id.Value.Length);
            app.VerifyAll();
        }

        [TestMethod]
        public async Task GetWorkstream_UsesVersionedReadWithStableScopedIdentity()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scratch = new Mock<IScratchStore>(MockBehavior.Loose);
            var activity = new Mock<IActivityRecordStore<WorkstreamActivityRecord>>(MockBehavior.Loose);
            var scope = EntityHeader.Create("ORG123", "Org");
            StorageKey captured = null;

            app.Setup(x => x.GetVersionedAsync<WorkstreamAuthorityRecord>(It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
               .Callback<StorageKey, CancellationToken>((key, _) => captured = key)
               .ReturnsAsync((VersionedApplicationDataRecord<WorkstreamAuthorityRecord>)null);

            var repo = new WorkstreamAuthorityRepository(app.Object, scratch.Object, activity.Object);
            await repo.GetWorkstreamAsync(scope, "build-dynamics-platform");

            Assert.IsNotNull(captured);
            Assert.AreEqual("ORG123", captured.Scope);
            Assert.AreEqual(32, captured.Id.Length);
            app.VerifyAll();
        }

        [TestMethod]
        public async Task UpdateWorkstream_WhenStorageRejectsStaleVersion_ReturnsConflict()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scratch = new Mock<IScratchStore>(MockBehavior.Loose);
            var activity = new Mock<IActivityRecordStore<WorkstreamActivityRecord>>(MockBehavior.Loose);
            var scope = EntityHeader.Create("ORG123", "Org");
            var staleVersion = CreateConcurrencyToken("stale-version");
            var conflict = CreateMutationResult(ApplicationDataMutationStatus.Conflict);

            app.Setup(x => x.UpdateIfVersionAsync(
                    It.IsAny<WorkstreamAuthorityRecord>(),
                    staleVersion,
                    It.IsAny<CancellationToken>()))
               .ReturnsAsync(conflict);

            var repo = new WorkstreamAuthorityRepository(app.Object, scratch.Object, activity.Object);
            var record = new WorkstreamAuthorityRecord
            {
                Organization = scope,
                WorkstreamId = "build-dynamics-platform",
                Name = "Build Dynamics"
            };

            var result = await repo.UpdateWorkstreamAsync(record, staleVersion);

            Assert.IsNotNull(result);
            Assert.AreEqual(ApplicationDataMutationStatus.Conflict, result.Status);
            app.VerifyAll();
        }

        [TestMethod]
        public async Task QueryTasks_EnforcesOrganizationAndWorkstreamScope()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scratch = new Mock<IScratchStore>(MockBehavior.Loose);
            var activity = new Mock<IActivityRecordStore<WorkstreamActivityRecord>>(MockBehavior.Loose);
            StorageQuery<TaskAuthorityRecord> captured = null;

            app.Setup(x => x.QueryAsync(It.IsAny<StorageQuery<TaskAuthorityRecord>>(), It.IsAny<CancellationToken>()))
               .Callback<StorageQuery<TaskAuthorityRecord>, CancellationToken>((q, _) => captured = q)
               .ReturnsAsync(new StoragePageResult<TaskAuthorityRecord>(Array.Empty<TaskAuthorityRecord>()));

            var repo = new WorkstreamAuthorityRepository(app.Object, scratch.Object, activity.Object);
            await repo.QueryTasksAsync(EntityHeader.Create("ORG123", "Org"), "ws-1");

            Assert.AreEqual(2, captured.Filters.Count);
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "Organization.Id" && (string)f.Value == "ORG123"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "WorkstreamId" && (string)f.Value == "ws-1"));
        }

        [TestMethod]
        public async Task ActivityHistory_UsesImmutableActivityStoreAndLifecycleQuery()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Loose);
            var scratch = new Mock<IScratchStore>(MockBehavior.Loose);
            var activity = new Mock<IActivityRecordStore<WorkstreamActivityRecord>>(MockBehavior.Strict);
            HistoryQuery<WorkstreamActivityRecord> captured = null;

            activity.Setup(x => x.QueryAsync(It.IsAny<HistoryQuery<WorkstreamActivityRecord>>(), It.IsAny<CancellationToken>()))
                    .Callback<HistoryQuery<WorkstreamActivityRecord>, CancellationToken>((q, _) => captured = q)
                    .ReturnsAsync(new StoragePageResult<WorkstreamActivityRecord>(Array.Empty<WorkstreamActivityRecord>()));

            var repo = new WorkstreamAuthorityRepository(app.Object, scratch.Object, activity.Object);
            var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddDays(1);
            await repo.QueryActivityAsync(BuildDynamicsAuthorityScope.System, "ws-1", start, end);

            Assert.AreEqual(start, captured.StartUtc);
            Assert.AreEqual(end, captured.EndUtc);
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "OrganizationId" && (string)f.Value == "SYSTEM"));
            Assert.IsTrue(captured.Filters.Any(f => f.Field == "WorkstreamId" && (string)f.Value == "ws-1"));
        }

        [TestMethod]
        public async Task CurrentAuthorityRecords_UseStableApplicationDataIdentityAndConditionalMutation()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scratch = new Mock<IScratchStore>(MockBehavior.Loose);
            var activity = new Mock<IActivityRecordStore<WorkstreamActivityRecord>>(MockBehavior.Loose);
            var scope = EntityHeader.Create("ORG123", "Org");
            FixWorkspaceAuthorityRecord insertedFix = null;
            DevOpsActivityAuthorityRecord insertedDevOps = null;
            StableFinalizationAuthorityRecord insertedFinalization = null;
            var version = CreateConcurrencyToken("current-version");

            app.Setup(x => x.InsertAsync(It.IsAny<FixWorkspaceAuthorityRecord>(), It.IsAny<CancellationToken>()))
               .Callback<FixWorkspaceAuthorityRecord, CancellationToken>((r, _) => insertedFix = r)
               .Returns(Task.CompletedTask);
            app.Setup(x => x.InsertAsync(It.IsAny<DevOpsActivityAuthorityRecord>(), It.IsAny<CancellationToken>()))
               .Callback<DevOpsActivityAuthorityRecord, CancellationToken>((r, _) => insertedDevOps = r)
               .Returns(Task.CompletedTask);
            app.Setup(x => x.InsertAsync(It.IsAny<StableFinalizationAuthorityRecord>(), It.IsAny<CancellationToken>()))
               .Callback<StableFinalizationAuthorityRecord, CancellationToken>((r, _) => insertedFinalization = r)
               .Returns(Task.CompletedTask);
            app.Setup(x => x.UpdateIfVersionAsync(It.IsAny<FixWorkspaceAuthorityRecord>(), version, It.IsAny<CancellationToken>()))
               .ReturnsAsync(CreateMutationResult(ApplicationDataMutationStatus.Updated));

            var repo = new WorkstreamAuthorityRepository(app.Object, scratch.Object, activity.Object);
            var fix = new FixWorkspaceAuthorityRecord
            {
                Organization = scope,
                WorkspaceId = "fix-current",
                ReferenceId = "F053",
                Identifier = "CURRENT",
                State = "completed",
                Payload = "{\"tasks\":[{\"id\":1,\"closed\":true}]}"
            };
            await repo.InsertFixWorkspaceAsync(fix);
            await repo.UpdateFixWorkspaceAsync(fix, version);
            await repo.InsertDevOpsActivityAsync(new DevOpsActivityAuthorityRecord
            {
                Organization = scope,
                ActivityId = "D035",
                OriginWorkstreamId = "build-dynamics-platform",
                State = "resolved",
                Payload = "{\"fixWorkspaceIds\":[\"fix-current\"]}"
            });
            await repo.InsertFinalizationAsync(new StableFinalizationAuthorityRecord
            {
                Organization = scope,
                FinalizationId = "ABCDEF0123456789ABCDEF0123456789",
                OwnerType = "FixWorkspace",
                OwnerId = "fix-current",
                State = "Succeeded",
                Payload = "{\"ownerId\":\"fix-current\"}"
            });

            Assert.AreEqual(32, insertedFix.Id.Value.Length);
            Assert.AreEqual(32, insertedDevOps.Id.Value.Length);
            Assert.AreEqual(32, insertedFinalization.Id.Value.Length);
            Assert.AreNotEqual(insertedFix.Id.Value, insertedDevOps.Id.Value);
            Assert.AreNotEqual(insertedFix.Id.Value, insertedFinalization.Id.Value);
            app.VerifyAll();
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

        [TestMethod]
        public async Task ScratchState_IsRoutedOnlyThroughScratchStore()
        {
            var app = new Mock<IApplicationDataStore>(MockBehavior.Strict);
            var scratch = new Mock<IScratchStore>(MockBehavior.Strict);
            var activity = new Mock<IActivityRecordStore<WorkstreamActivityRecord>>(MockBehavior.Loose);
            var scope = EntityHeader.Create("ORG123", "Org");
            var record = new WorkstreamOrchestrationScratchRecord
            {
                Id = NormalizedId32.Factory(),
                Organization = scope,
                WorkstreamId = "ws-1",
                WorkspaceId = "workspace-1",
                Kind = "lease-probe",
                Value = "ok"
            };

            scratch.Setup(x => x.UpsertAsync(record, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var repo = new WorkstreamAuthorityRepository(app.Object, scratch.Object, activity.Object);
            await repo.UpsertScratchAsync(record);

            scratch.VerifyAll();
            app.VerifyNoOtherCalls();
        }
    }
}
