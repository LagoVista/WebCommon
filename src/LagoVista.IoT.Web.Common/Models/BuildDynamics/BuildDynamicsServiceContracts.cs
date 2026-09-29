using LagoVista.CloudStorage.Storage;
using System;
using System.Collections.Generic;

namespace LagoVista.IoT.Web.Common.Models.BuildDynamics
{
    public sealed class BuildDynamicsVersionedRecord<T>
    {
        public T Record { get; set; }
        public string Version { get; set; } = String.Empty;
    }

    public sealed class BuildDynamicsMutationRequest<T>
    {
        public T Record { get; set; }
        public string ExpectedVersion { get; set; } = String.Empty;
    }

    public sealed class BuildDynamicsMutationResponse
    {
        public string Status { get; set; } = String.Empty;
        public string Version { get; set; } = String.Empty;
    }

    public sealed class BuildDynamicsPage<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
        public string ContinuationToken { get; set; } = String.Empty;
    }

    public sealed class BuildDynamicsRetentionPolicyUpdateRequest
    {
        public List<StorageRetentionRule> Rules { get; set; } = new List<StorageRetentionRule>();
        public string ExpectedVersion { get; set; } = String.Empty;
    }

    public sealed class BuildDynamicsRetentionDecisionResponse
    {
        public double? EffectiveTtlSeconds { get; set; }
        public bool IsProtected { get; set; }
        public bool SummarizeBeforeExpiry { get; set; }
        public bool ExplicitlyGoverned { get; set; }
        public string Source { get; set; } = String.Empty;
    }
}
