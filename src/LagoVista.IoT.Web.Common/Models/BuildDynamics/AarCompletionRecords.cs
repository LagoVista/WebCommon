using System;
using System.Collections.Generic;

namespace LagoVista.IoT.Web.Common.Models.BuildDynamics
{
    public sealed class AarFollowUpRecord
    {
        public string FollowUpId { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
        public string ConvertedTaskId { get; set; } = String.Empty;
    }

    public sealed class AarRecord : BuildDynamicsApplicationRecord
    {
        public string AarId { get; set; } = String.Empty;
        public string WorkstreamId { get; set; } = String.Empty;
        public string TaskId { get; set; } = String.Empty;
        public string WorkspaceId { get; set; } = String.Empty;
        public string Category { get; set; } = String.Empty;
        public string Status { get; set; } = String.Empty;
        public string Disposition { get; set; } = String.Empty;
        public List<string> Findings { get; set; } = new List<string>();
        public List<AarFollowUpRecord> FollowUps { get; set; } = new List<AarFollowUpRecord>();
        public DateTime? FinalizedAtUtc { get; set; }
    }

    public sealed class CompletionTaskOutcome
    {
        public string TaskId { get; set; } = String.Empty;
        public string Outcome { get; set; } = String.Empty;
    }

    public sealed class WorkstreamCompletionSummaryRecord : BuildDynamicsApplicationRecord
    {
        public string WorkstreamId { get; set; } = String.Empty;
        public string Objective { get; set; } = String.Empty;
        public List<CompletionTaskOutcome> TaskOutcomes { get; set; } = new List<CompletionTaskOutcome>();
        public List<string> AcceptedSourceIdentities { get; set; } = new List<string>();
        public List<string> AcceptedArtifactIdentities { get; set; } = new List<string>();
        public List<string> SignificantFindings { get; set; } = new List<string>();
        public string BuildSummary { get; set; } = String.Empty;
        public string TestSummary { get; set; } = String.Empty;
        public List<string> AarConclusions { get; set; } = new List<string>();
        public List<string> UnresolvedIssues { get; set; } = new List<string>();
        public List<string> WaivedIssues { get; set; } = new List<string>();
        public DateTime GeneratedAtUtc { get; set; }
        public DateTime? DetailSummarizedThroughUtc { get; set; }
    }
}
