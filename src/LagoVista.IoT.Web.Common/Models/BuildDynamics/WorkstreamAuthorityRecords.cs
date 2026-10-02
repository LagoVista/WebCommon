using LagoVista.CloudStorage.Storage;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using System;

namespace LagoVista.IoT.Web.Common.Models.BuildDynamics
{
    public static class BuildDynamicsAuthorityScope
    {
        public static EntityHeader System { get; } = EntityHeader.Create("SYSTEM", "System");
    }

    public abstract class BuildDynamicsApplicationRecord : IApplicationDataRecord
    {
        public NormalizedId32 Id { get; set; }
        public EntityHeader Organization { get; set; }
        public UtcTimestamp CreationDate { get; set; }
        public UtcTimestamp LastUpdatedDate { get; set; }
    }

    public sealed class WorkstreamAuthorityRecord : BuildDynamicsApplicationRecord
    {
        public string WorkstreamId { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Objective { get; set; } = String.Empty;
        public string State { get; set; } = String.Empty;
    }

    public sealed class TaskAuthorityRecord : BuildDynamicsApplicationRecord
    {
        public string WorkstreamId { get; set; } = String.Empty;
        public string TaskId { get; set; } = String.Empty;
        public string WorkspaceId { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string Objective { get; set; } = String.Empty;
        public string State { get; set; } = String.Empty;
    }

    public sealed class WorkspaceAuthorityRecord : BuildDynamicsApplicationRecord
    {
        public string WorkstreamId { get; set; } = String.Empty;
        public string TaskId { get; set; } = String.Empty;
        public string WorkspaceId { get; set; } = String.Empty;
        public string BranchIdentity { get; set; } = String.Empty;
        public string State { get; set; } = String.Empty;
        public DateTime? LeaseExpiresAtUtc { get; set; }
    }

    public sealed class FixWorkspaceAuthorityRecord : BuildDynamicsApplicationRecord
    {
        public string WorkspaceId { get; set; } = String.Empty;
        public string ReferenceId { get; set; } = String.Empty;
        public string Identifier { get; set; } = String.Empty;
        public string State { get; set; } = String.Empty;
        public string FinalizationStatus { get; set; } = String.Empty;
        public string DevOpsActivityId { get; set; } = String.Empty;
        public string ConversationUrl { get; set; } = String.Empty;
        public string Payload { get; set; } = String.Empty;
    }

    public sealed class DevOpsActivityAuthorityRecord : BuildDynamicsApplicationRecord
    {
        public string ActivityId { get; set; } = String.Empty;
        public string OriginWorkstreamId { get; set; } = String.Empty;
        public string OriginTaskId { get; set; } = String.Empty;
        public string OriginWorkspaceId { get; set; } = String.Empty;
        public string State { get; set; } = String.Empty;
        public string Classification { get; set; } = String.Empty;
        public string ConversationUrl { get; set; } = String.Empty;
        public string Payload { get; set; } = String.Empty;
    }

    public sealed class StableFinalizationAuthorityRecord : BuildDynamicsApplicationRecord
    {
        public string FinalizationId { get; set; } = String.Empty;
        public string OwnerType { get; set; } = String.Empty;
        public string OwnerId { get; set; } = String.Empty;
        public string State { get; set; } = String.Empty;
        public string Mode { get; set; } = String.Empty;
        public string Payload { get; set; } = String.Empty;
    }

    public sealed class WorkstreamCoordinationAuthorityRecord : BuildDynamicsApplicationRecord
    {
        public string WorkstreamId { get; set; } = String.Empty;
        public string RecordType { get; set; } = String.Empty;
        public string StableId { get; set; } = String.Empty;
        public string TaskId { get; set; } = String.Empty;
        public string Payload { get; set; } = String.Empty;
    }

    public sealed class WorkstreamOrchestrationScratchRecord : IScratchDataRecord
    {
        public NormalizedId32 Id { get; set; }
        public EntityHeader Organization { get; set; }
        public string WorkstreamId { get; set; } = String.Empty;
        public string WorkspaceId { get; set; } = String.Empty;
        public string Kind { get; set; } = String.Empty;
        public string Value { get; set; } = String.Empty;
    }

    public sealed class WorkstreamActivityRecord : IActivityRecord
    {
        public string Id { get; set; } = String.Empty;
        public string OrganizationId { get; set; } = String.Empty;
        public string Organization { get; set; } = String.Empty;
        public DateTime CreationDate { get; set; }
        public string WorkstreamId { get; set; } = String.Empty;
        public string TaskId { get; set; } = String.Empty;
        public string WorkspaceId { get; set; } = String.Empty;
        public string ActivityType { get; set; } = String.Empty;
        public string Detail { get; set; } = String.Empty;
    }
}
