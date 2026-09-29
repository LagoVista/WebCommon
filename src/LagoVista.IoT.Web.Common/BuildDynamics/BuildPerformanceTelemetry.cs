using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.BuildDynamics
{
    public sealed class BuildExecutionTelemetry : IActivityRecord
    {
        public string Id { get; set; }
        public string OrganizationId { get; set; }
        public string Organization { get; set; }
        public DateTime CreationDate { get; set; }
        public string Operation { get; set; }
        public string Result { get; set; }
        public string Repository { get; set; }
        public string Project { get; set; }
        public string SourceIdentity { get; set; }
        public string WorkstreamId { get; set; }
        public string TaskId { get; set; }
        public string WorkspaceId { get; set; }
        public string BuildServerVersion { get; set; }
        public double QueueDurationMs { get; set; }
        public double DurationMs { get; set; }
        public string PhaseDurationsJson { get; set; }
    }

    public sealed class BuildExecutionSample
    {
        public string Id { get; set; }
        public string OrganizationId { get; set; }
        public string Organization { get; set; }
        public string Operation { get; set; }
        public string Result { get; set; }
        public string Repository { get; set; }
        public string Project { get; set; }
        public string SourceIdentity { get; set; }
        public string WorkstreamId { get; set; }
        public string TaskId { get; set; }
        public string WorkspaceId { get; set; }
        public string BuildServerVersion { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public IReadOnlyDictionary<string, TimeSpan> PhaseDurations { get; set; } = new Dictionary<string, TimeSpan>();
    }

    public interface IBuildPerformanceTelemetryService
    {
        Task RecordAsync(BuildExecutionSample sample, CancellationToken cancellationToken = default);
        Task<MetricQueryResult> QueryTasksPerHourAsync(string organizationId, DateTime start, DateTime end, IEnumerable<MetricDimensionFilter> dimensions = null, IEnumerable<string> groupByDimensions = null, CancellationToken cancellationToken = default);
        Task<MetricQueryResult> QueryBuildDurationPercentileAsync(string organizationId, DateTime start, DateTime end, bool p95, IEnumerable<MetricDimensionFilter> dimensions = null, IEnumerable<string> groupByDimensions = null, CancellationToken cancellationToken = default);
    }

    public sealed class BuildPerformanceTelemetryService : IBuildPerformanceTelemetryService
    {
        public const string ExecutionCountMetric = "build-dynamics.execution-count";
        public const string DurationMetric = "build-dynamics.duration-ms";
        public const string QueueDurationMetric = "build-dynamics.queue-duration-ms";
        public const string PhaseDurationMetric = "build-dynamics.phase-duration-ms";

        private static readonly MetricDimensionDefinition[] CommonDimensions =
        {
            new MetricDimensionDefinition("operation", "Operation", true),
            new MetricDimensionDefinition("result", "Result", true),
            new MetricDimensionDefinition("repository", "Repository", true),
            new MetricDimensionDefinition("project", "Project"),
            new MetricDimensionDefinition("source", "Source identity"),
            new MetricDimensionDefinition("workstream", "Workstream", true),
            new MetricDimensionDefinition("task", "Task", true),
            new MetricDimensionDefinition("workspace", "Workspace"),
            new MetricDimensionDefinition("buildServerVersion", "Build Server version")
        };

        private readonly IActivityRecordStore<BuildExecutionTelemetry> _activityStore;
        private readonly IMetricsStore _metricsStore;

        public BuildPerformanceTelemetryService(IActivityRecordStore<BuildExecutionTelemetry> activityStore, IMetricsStore metricsStore)
        {
            _activityStore = activityStore ?? throw new ArgumentNullException(nameof(activityStore));
            _metricsStore = metricsStore ?? throw new ArgumentNullException(nameof(metricsStore));
        }

        public async Task RecordAsync(BuildExecutionSample sample, CancellationToken cancellationToken = default)
        {
            Validate(sample);
            var queuedAt = NormalizeUtc(sample.QueuedAt);
            var startedAt = NormalizeUtc(sample.StartedAt);
            var completedAt = NormalizeUtc(sample.CompletedAt);
            if (startedAt < queuedAt) throw new ArgumentException("StartedAt must be at or after QueuedAt.", nameof(sample));
            if (completedAt < startedAt) throw new ArgumentException("CompletedAt must be at or after StartedAt.", nameof(sample));

            await EnsureDefinitionsAsync(cancellationToken).ConfigureAwait(false);

            var dimensions = Dimensions(sample);
            var activity = new BuildExecutionTelemetry
            {
                Id = sample.Id,
                OrganizationId = sample.OrganizationId,
                Organization = sample.Organization,
                CreationDate = completedAt,
                Operation = sample.Operation,
                Result = sample.Result,
                Repository = sample.Repository,
                Project = sample.Project,
                SourceIdentity = sample.SourceIdentity,
                WorkstreamId = sample.WorkstreamId,
                TaskId = sample.TaskId,
                WorkspaceId = sample.WorkspaceId,
                BuildServerVersion = sample.BuildServerVersion,
                QueueDurationMs = (startedAt - queuedAt).TotalMilliseconds,
                DurationMs = (completedAt - startedAt).TotalMilliseconds,
                PhaseDurationsJson = JsonSerializer.Serialize((sample.PhaseDurations ?? new Dictionary<string, TimeSpan>()).ToDictionary(item => item.Key, item => item.Value.TotalMilliseconds))
            };

            await _activityStore.InsertAsync(activity, cancellationToken).ConfigureAwait(false);

            var records = new List<MetricRecord>
            {
                Metric(sample, ExecutionCountMetric, completedAt, 1, dimensions, "count"),
                Metric(sample, DurationMetric, completedAt, activity.DurationMs, dimensions, "duration"),
                Metric(sample, QueueDurationMetric, completedAt, activity.QueueDurationMs, dimensions, "queue")
            };

            foreach (var phase in sample.PhaseDurations ?? new Dictionary<string, TimeSpan>())
            {
                var phaseDimensions = new Dictionary<string, string>(dimensions, StringComparer.OrdinalIgnoreCase)
                {
                    ["phase"] = phase.Key
                };
                records.Add(Metric(sample, PhaseDurationMetric, completedAt, phase.Value.TotalMilliseconds, phaseDimensions, $"phase-{phase.Key}"));
            }

            await _metricsStore.RecordBatchAsync(records, cancellationToken).ConfigureAwait(false);
        }

        public Task<MetricQueryResult> QueryTasksPerHourAsync(string organizationId, DateTime start, DateTime end, IEnumerable<MetricDimensionFilter> dimensions = null, IEnumerable<string> groupByDimensions = null, CancellationToken cancellationToken = default)
        {
            return _metricsStore.QueryAsync(new MetricQuery(organizationId, ExecutionCountMetric, start, end, MetricAggregate.Sum, TimeSpan.FromHours(1), dimensions, groupByDimensions), cancellationToken);
        }

        public Task<MetricQueryResult> QueryBuildDurationPercentileAsync(string organizationId, DateTime start, DateTime end, bool p95, IEnumerable<MetricDimensionFilter> dimensions = null, IEnumerable<string> groupByDimensions = null, CancellationToken cancellationToken = default)
        {
            var aggregate = (MetricAggregate)(p95 ? 6 : 5);
            return _metricsStore.QueryAsync(new MetricQuery(organizationId, DurationMetric, start, end, aggregate, null, dimensions, groupByDimensions), cancellationToken);
        }

        private async Task EnsureDefinitionsAsync(CancellationToken cancellationToken)
        {
            await _metricsStore.RegisterDefinitionAsync(new MetricDefinition("build-dynamics-execution-count", ExecutionCountMetric, "Build Dynamics execution count", CommonDimensions), cancellationToken).ConfigureAwait(false);
            await _metricsStore.RegisterDefinitionAsync(new MetricDefinition("build-dynamics-duration-ms", DurationMetric, "Build Dynamics execution duration", CommonDimensions), cancellationToken).ConfigureAwait(false);
            await _metricsStore.RegisterDefinitionAsync(new MetricDefinition("build-dynamics-queue-duration-ms", QueueDurationMetric, "Build Dynamics queue duration", CommonDimensions), cancellationToken).ConfigureAwait(false);
            await _metricsStore.RegisterDefinitionAsync(new MetricDefinition("build-dynamics-phase-duration-ms", PhaseDurationMetric, "Build Dynamics phase duration", CommonDimensions.Concat(new[] { new MetricDimensionDefinition("phase", "Phase", true) })), cancellationToken).ConfigureAwait(false);
        }

        private static MetricRecord Metric(BuildExecutionSample sample, string metric, DateTime timestamp, double value, IReadOnlyDictionary<string, string> dimensions, string suffix)
        {
            return new MetricRecord($"{sample.Id}-{suffix}", sample.OrganizationId, sample.Organization, metric, timestamp, value, dimensions);
        }

        private static Dictionary<string, string> Dimensions(BuildExecutionSample sample)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["operation"] = sample.Operation,
                ["result"] = sample.Result,
                ["repository"] = sample.Repository,
                ["project"] = sample.Project ?? String.Empty,
                ["source"] = sample.SourceIdentity,
                ["workstream"] = sample.WorkstreamId ?? String.Empty,
                ["task"] = sample.TaskId ?? String.Empty,
                ["workspace"] = sample.WorkspaceId ?? String.Empty,
                ["buildServerVersion"] = sample.BuildServerVersion
            };
        }

        private static void Validate(BuildExecutionSample sample)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));
            Require(sample.Id, nameof(sample.Id));
            Require(sample.OrganizationId, nameof(sample.OrganizationId));
            Require(sample.Organization, nameof(sample.Organization));
            Require(sample.Operation, nameof(sample.Operation));
            Require(sample.Result, nameof(sample.Result));
            Require(sample.Repository, nameof(sample.Repository));
            Require(sample.SourceIdentity, nameof(sample.SourceIdentity));
            Require(sample.BuildServerVersion, nameof(sample.BuildServerVersion));
        }

        private static void Require(string value, string name)
        {
            if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{name} is required.", name);
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
