using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Managers
{
    public class PlatformSmokeTestManager : IPlatformSmokeTestManager
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
        private readonly IEnumerable<IPlatformSmokeTest> _tests;

        public PlatformSmokeTestManager(IEnumerable<IPlatformSmokeTest> tests)
        {
            _tests = tests ?? Enumerable.Empty<IPlatformSmokeTest>();
        }

        public async Task<PlatformSmokeTestDashboard> RunAsync(CancellationToken cancellationToken = default)
        {
            var tests = _tests.OrderBy(test => test.Category).ThenBy(test => test.Name).ToList();
            var results = await Task.WhenAll(tests.Select(test => RunTestAsync(test, cancellationToken))).ConfigureAwait(false);

            return new PlatformSmokeTestDashboard
            {
                InstanceName = Environment.MachineName,
                EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                    ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                    ?? "Unknown",
                GeneratedUtc = DateTime.UtcNow,
                Tests = results.OrderBy(result => result.Category).ThenBy(result => result.Name).ToList()
            };
        }

        private static async Task<PlatformSmokeTestResult> RunTestAsync(IPlatformSmokeTest test, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TestTimeout);

            try
            {
                var result = await test.ExecuteAsync(timeout.Token).ConfigureAwait(false) ?? new PlatformSmokeTestResult
                {
                    Status = PlatformSmokeTestStatus.Failed,
                    Message = "Smoke test returned no result."
                };

                PopulateCommonFields(result, test, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return CreateFailure(test, stopwatch.ElapsedMilliseconds, $"Timed out after {TestTimeout.TotalSeconds:0} seconds.");
            }
            catch (Exception ex)
            {
                return CreateFailure(test, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }

        private static PlatformSmokeTestResult CreateFailure(IPlatformSmokeTest test, long durationMs, string message)
        {
            return new PlatformSmokeTestResult
            {
                Key = test.Key,
                Name = test.Name,
                Category = test.Category,
                Status = PlatformSmokeTestStatus.Failed,
                Message = message,
                DurationMs = durationMs,
                CheckedUtc = DateTime.UtcNow
            };
        }

        private static void PopulateCommonFields(PlatformSmokeTestResult result, IPlatformSmokeTest test, long durationMs)
        {
            result.Key = String.IsNullOrWhiteSpace(result.Key) ? test.Key : result.Key;
            result.Name = String.IsNullOrWhiteSpace(result.Name) ? test.Name : result.Name;
            result.Category = String.IsNullOrWhiteSpace(result.Category) ? test.Category : result.Category;
            result.DurationMs = durationMs;
            result.CheckedUtc = DateTime.UtcNow;
        }
    }
}
