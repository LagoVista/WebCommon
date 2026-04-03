using k8s;
using LagoVista.Core.Models.Diagnostics;
using LagoVista.IoT.Web.Common.Interfaces.Services;
using LagoVista.IoT.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Services
{
    public class KubernetesPodDiscoveryService : IKubernetesPodDiscoveryService
    {
        private const string NamespaceFilePath = "/var/run/secrets/kubernetes.io/serviceaccount/namespace";
        private readonly IKubernetes _kubernetesClient;

        public KubernetesPodDiscoveryService(IKubernetes kubernetesClient)
        {
            _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
        }

        public async Task<List<HostedServiceDiagnosticPodTarget>> GetHostedServiceDiagnosticPodsAsync()
        {
            var currentNamespace = GetCurrentNamespace();
            var currentPodName = Environment.GetEnvironmentVariable("HOSTNAME");

            var podList = await _kubernetesClient.CoreV1.ListNamespacedPodAsync(namespaceParameter: currentNamespace, labelSelector: "app=nuviot-web").ConfigureAwait(false);

            return podList.Items
                .Where(pod => pod?.Metadata?.Labels != null)
                .Where(pod => pod.Metadata.Labels.TryGetValue("module", out _))
                .Where(pod => pod.Metadata.Labels.TryGetValue("tier", out var tier) && IsTrackedTier(tier))
                .Where(pod => String.Equals(pod.Status?.Phase, "Running", StringComparison.OrdinalIgnoreCase))
                .Where(pod => !String.IsNullOrWhiteSpace(pod.Status?.PodIP))
                .Select(pod => new HostedServiceDiagnosticPodTarget
                {
                    PodName = pod.Metadata.Name,
                    PodIp = pod.Status.PodIP,
                    Namespace = pod.Metadata.NamespaceProperty,
                    Module = pod.Metadata.Labels.TryGetValue("module", out var module) ? module : null,
                    Tier = pod.Metadata.Labels["tier"],
                    NodeName = pod.Spec?.NodeName,
                    IsCurrentPod = String.Equals(pod.Metadata.Name, currentPodName, StringComparison.OrdinalIgnoreCase),
                    Labels = pod.Metadata.Labels.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                })
                .OrderBy(pod => pod.Module)
                .ThenBy(pod => pod.PodName)
                .ToList();
        }

        private static bool IsTrackedTier(string tier)
        {
            return String.Equals(tier, "portal-services", StringComparison.OrdinalIgnoreCase)
                || String.Equals(tier, "http-services", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCurrentNamespace()
        {
            var envNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE");
            if (!String.IsNullOrWhiteSpace(envNamespace))
            {
                return envNamespace;
            }

            if (File.Exists(NamespaceFilePath))
            {
                var fileNamespace = File.ReadAllText(NamespaceFilePath)?.Trim();
                if (!String.IsNullOrWhiteSpace(fileNamespace))
                {
                    return fileNamespace;
                }
            }

            return "default";
        }
    }
}