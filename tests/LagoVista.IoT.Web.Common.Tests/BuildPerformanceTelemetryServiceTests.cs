using LagoVista.CloudStorage.Storage;
using LagoVista.IoT.Web.Common.BuildDynamics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Tests.BuildDynamics
{
    [TestClass]
    public class BuildPerformanceTelemetryServiceTests
    {
        [TestMethod]
        public async Task RecordAsync_WritesDetailedActivityAndDimensionalMetrics()
        {
            var activity = new ActivityStore();
            var metrics = new MetricsStore();
            var service = new BuildPerformanceTelemetryService(activity, metrics);
            var sample = CreateSample("e1", 100, 900, new Dictionary<string, TimeSpan> { ["restore"] = TimeSpan.FromMilliseconds(200), ["compile"] = TimeSpan.FromMilliseconds(500) });

            await service.RecordAsync(sample);

            Assert.AreEqual(1, activity.Items.Count);
            Assert.AreEqual("repo/a", activity.Items[0].Repository);
            Assert.AreEqual("source-1", activity.Items[0].SourceIdentity);
            Assert.AreEqual("ws-1", activity.Items[0].WorkstreamId);
            Assert.AreEqual("task-1", activity.Items[0].TaskId);
            Assert.AreEqual("workspace-1", activity.Items[0].WorkspaceId);
            Assert.AreEqual("2.0.21", activity.Items[0].BuildServerVersion);
            Assert.AreEqual(100d, activity.Items[0].QueueDurationMs, 0.001);
            Assert.AreEqual(900d, activity.Items[0].DurationMs, 0.001);
            Assert.AreEqual(5, metrics.Records.Count);
            Assert.IsTrue(metrics.Definitions.ContainsKey(BuildPerformanceTelemetryService.DurationMetric));
            Assert.AreEqual("repo/a", metrics.Records.Single(r => r.Metric == BuildPerformanceTelemetryService.DurationMetric).Dimensions["repository"]);
            Assert.AreEqual("compile", metrics.Records.Single(r => r.Metric == BuildPerformanceTelemetryService.PhaseDurationMetric && r.Dimensions["phase"] == "compile").Dimensions["phase"]);
        }

        [TestMethod]
        public async Task Queries_ProvideHourlyThroughputAndP50P95AfterRawActivityIsGone()
        {
            var activity = new ActivityStore();
            var metrics = new MetricsStore();
            var service = new BuildPerformanceTelemetryService(activity, metrics);
            await service.RecordAsync(CreateSample("e1", 50, 100, null));
            await service.RecordAsync(CreateSample("e2", 75, 200, null, minutes: 10));
            await service.RecordAsync(CreateSample("e3", 25, 1000, null, minutes: 20));

            activity.Items.Clear();

            var start = new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc);
            var hourly = await service.QueryTasksPerHourAsync("ORG", start, start.AddHours(1));
            var p50 = await service.QueryBuildDurationPercentileAsync("ORG", start, start.AddHours(1), false);
            var p95 = await service.QueryBuildDurationPercentileAsync("ORG", start, start.AddHours(1), true);

            Assert.AreEqual(3d, hourly.Values.Single().Value, 0.001);
            Assert.AreEqual(200d, p50.Values.Single().Value, 0.001);
            Assert.AreEqual(920d, p95.Values.Single().Value, 0.001);
            Assert.AreEqual(0, activity.Items.Count);
        }

        private static BuildExecutionSample CreateSample(string id, double queueMs, double durationMs, IReadOnlyDictionary<string, TimeSpan> phases, int minutes = 0)
        {
            var queued = new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc).AddMinutes(minutes);
            return new BuildExecutionSample
            {
                Id = id,
                OrganizationId = "ORG",
                Organization = "Software Logistics",
                Operation = "build",
                Result = "succeeded",
                Repository = "repo/a",
                Project = "app",
                SourceIdentity = "source-1",
                WorkstreamId = "ws-1",
                TaskId = "task-1",
                WorkspaceId = "workspace-1",
                BuildServerVersion = "2.0.21",
                QueuedAt = queued,
                StartedAt = queued.AddMilliseconds(queueMs),
                CompletedAt = queued.AddMilliseconds(queueMs + durationMs),
                PhaseDurations = phases ?? new Dictionary<string, TimeSpan>()
            };
        }

        private sealed class ActivityStore : IActivityRecordStore<BuildExecutionTelemetry>
        {
            public List<BuildExecutionTelemetry> Items { get; } = new();
            public Task InsertAsync(BuildExecutionTelemetry record, CancellationToken cancellationToken = default) { Items.Add(record); return Task.CompletedTask; }
            public Task InsertBatchAsync(IEnumerable<BuildExecutionTelemetry> records, CancellationToken cancellationToken = default) { Items.AddRange(records); return Task.CompletedTask; }
            public Task<StoragePageResult<BuildExecutionTelemetry>> QueryAsync(HistoryQuery<BuildExecutionTelemetry> query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }

        private sealed class MetricsStore : IMetricsStore
        {
            public Dictionary<string, MetricDefinition> Definitions { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<MetricRecord> Records { get; } = new();

            public Task RegisterDefinitionAsync(MetricDefinition definition, CancellationToken cancellationToken = default) { Definitions[definition.Key] = definition; Definitions[definition.Id] = definition; return Task.CompletedTask; }
            public Task<MetricDefinition> GetDefinitionAsync(string metric, CancellationToken cancellationToken = default) { Definitions.TryGetValue(metric, out var value); return Task.FromResult(value); }
            public Task RecordAsync(MetricRecord record, CancellationToken cancellationToken = default) { Records.Add(record); return Task.CompletedTask; }
            public Task RecordBatchAsync(IEnumerable<MetricRecord> records, CancellationToken cancellationToken = default) { Records.AddRange(records); return Task.CompletedTask; }

            public Task<MetricQueryResult> QueryAsync(MetricQuery query, CancellationToken cancellationToken = default)
            {
                var rows = Records.Where(r => r.OrganizationId == query.OrganizationId && r.Metric == query.Metric && r.Timestamp >= query.Start && r.Timestamp <= query.End).ToList();
                foreach (var filter in query.Dimensions) rows = rows.Where(r => r.Dimensions.TryGetValue(filter.Key, out var value) && value == filter.Value).ToList();
                if (query.Bucket.HasValue)
                {
                    var values = rows.GroupBy(r => new DateTime((r.Timestamp.Ticks / query.Bucket.Value.Ticks) * query.Bucket.Value.Ticks, DateTimeKind.Utc))
                        .Select(g => new MetricValue(g.Key, Aggregate(g.Select(r => r.Value).ToArray(), query.Aggregate))).ToArray();
                    return Task.FromResult(new MetricQueryResult(values));
                }
                return Task.FromResult(new MetricQueryResult(new[] { new MetricValue(query.Start, Aggregate(rows.Select(r => r.Value).ToArray(), query.Aggregate)) }));
            }

            private static double Aggregate(double[] values, MetricAggregate aggregate)
            {
                if (values.Length == 0) return 0;
                return (int)aggregate switch
                {
                    0 => values.Length,
                    1 => values.Sum(),
                    2 => values.Average(),
                    3 => values.Min(),
                    4 => values.Max(),
                    5 => Percentile(values, 0.50),
                    6 => Percentile(values, 0.95),
                    _ => throw new ArgumentOutOfRangeException(nameof(aggregate))
                };
            }

            private static double Percentile(double[] input, double percentile)
            {
                var values = input.OrderBy(value => value).ToArray();
                var rank = percentile * (values.Length - 1);
                var lower = (int)Math.Floor(rank);
                var upper = (int)Math.Ceiling(rank);
                if (lower == upper) return values[lower];
                return values[lower] + ((values[upper] - values[lower]) * (rank - lower));
            }
        }
    }
}
