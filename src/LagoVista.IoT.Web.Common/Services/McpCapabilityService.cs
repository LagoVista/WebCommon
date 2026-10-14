using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Services
{
    public sealed class McpCapabilityService : IMcpCapabilityService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ConcurrentDictionary<string, McpServerConnection> _servers =
            new ConcurrentDictionary<string, McpServerConnection>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, McpServerCapabilitySnapshot> _snapshots =
            new ConcurrentDictionary<string, McpServerCapabilitySnapshot>(StringComparer.OrdinalIgnoreCase);

        public McpCapabilityService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public Task RegisterAsync(McpServerConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (String.IsNullOrWhiteSpace(connection.Id)) throw new ArgumentException("MCP server id is required.");
            if (String.IsNullOrWhiteSpace(connection.Endpoint)) throw new ArgumentException("MCP server endpoint is required.");
            if (!Uri.TryCreate(connection.Endpoint, UriKind.Absolute, out var endpoint) ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("MCP server endpoint must be an absolute HTTP(S) URI.");

            _servers[connection.Id] = connection;
            _snapshots.TryRemove(connection.Id, out _);
            return Task.CompletedTask;
        }

        public Task<McpServerCapabilitySnapshot> TestConnectionAsync(string serverId) => DiscoverAsync(serverId);

        public async Task<McpServerCapabilitySnapshot> DiscoverAsync(string serverId)
        {
            var server = RequireServer(serverId);
            var result = await SendRpcAsync(server, "tools/list", new { });
            var tools = new List<McpToolCapability>();

            if (!result.TryGetProperty("tools", out var toolArray) || toolArray.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("MCP tools/list response did not contain a tools array.");

            foreach (var tool in toolArray.EnumerateArray())
            {
                var name = tool.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                if (String.IsNullOrWhiteSpace(name)) continue;
                var description = tool.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()
                    : null;
                var schema = tool.TryGetProperty("inputSchema", out var schemaElement)
                    ? schemaElement.GetRawText()
                    : "{}";

                tools.Add(new McpToolCapability
                {
                    ServerId = server.Id,
                    ToolName = name,
                    Description = description,
                    InputSchemaJson = schema,
                    ContractHash = ComputeContractHash(name, description, schema)
                });
            }

            var snapshot = new McpServerCapabilitySnapshot
            {
                ServerId = server.Id,
                ServerName = server.Name,
                Endpoint = server.Endpoint,
                DiscoveredAtUtc = DateTime.UtcNow.ToString("o"),
                Tools = tools.OrderBy(tool => tool.ToolName, StringComparer.OrdinalIgnoreCase).ToList()
            };
            _snapshots[server.Id] = snapshot;
            return snapshot;
        }

        public Task<IReadOnlyCollection<McpServerCapabilitySnapshot>> GetRegisteredServersAsync()
        {
            IReadOnlyCollection<McpServerCapabilitySnapshot> result = _servers.Values
                .OrderBy(server => server.Name ?? server.Id, StringComparer.OrdinalIgnoreCase)
                .Select(server => _snapshots.TryGetValue(server.Id, out var snapshot)
                    ? snapshot
                    : new McpServerCapabilitySnapshot
                    {
                        ServerId = server.Id,
                        ServerName = server.Name,
                        Endpoint = server.Endpoint,
                        Tools = new List<McpToolCapability>()
                    })
                .ToArray();
            return Task.FromResult(result);
        }

        public async Task<McpToolInvocationResult> InvokeAsync(McpToolInvocationRequest request)
        {
            if (request?.Binding == null) throw new ArgumentException("An MCP tool binding is required.");
            var binding = request.Binding;
            var started = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var invocation = new McpToolInvocationResult
            {
                ServerId = binding.ServerId,
                ToolName = binding.ToolName,
                StartedAtUtc = started.ToString("o")
            };

            try
            {
                var snapshot = await DiscoverAsync(binding.ServerId);
                var capability = snapshot.Tools.FirstOrDefault(tool =>
                    tool.ToolName.Equals(binding.ToolName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("The bound MCP tool is no longer advertised by the server.");

                invocation.ObservedContractHash = capability.ContractHash;
                if (!String.IsNullOrWhiteSpace(binding.ToolContractHash) &&
                    !String.Equals(binding.ToolContractHash, capability.ContractHash, StringComparison.Ordinal))
                    throw new InvalidOperationException("The bound MCP tool contract has changed since the process definition was authored.");

                var arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in binding.StaticInputs ?? new Dictionary<string, object>())
                    arguments[item.Key] = item.Value;

                foreach (var mapping in binding.InputMappings ?? new Dictionary<string, string>())
                {
                    if (request.ProcessContext == null || !request.ProcessContext.TryGetValue(mapping.Value, out var value))
                        throw new InvalidOperationException($"Process context value '{mapping.Value}' required for MCP argument '{mapping.Key}' was not supplied.");
                    arguments[mapping.Key] = value;
                }

                var result = await SendRpcAsync(RequireServer(binding.ServerId), "tools/call", new
                {
                    name = binding.ToolName,
                    arguments
                });

                invocation.Succeeded = true;
                invocation.ResultJson = result.GetRawText();
            }
            catch (Exception ex)
            {
                invocation.Succeeded = false;
                invocation.Error = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                invocation.DurationMilliseconds = stopwatch.ElapsedMilliseconds;
                invocation.CompletedAtUtc = DateTime.UtcNow.ToString("o");
            }

            return invocation;
        }

        private McpServerConnection RequireServer(string serverId)
        {
            if (String.IsNullOrWhiteSpace(serverId) || !_servers.TryGetValue(serverId, out var server))
                throw new InvalidOperationException("The requested MCP server is not registered.");
            return server;
        }

        private async Task<JsonElement> SendRpcAsync(McpServerConnection server, string method, object parameters)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, server.Endpoint);
            request.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
            foreach (var header in server.Headers ?? new Dictionary<string, string>())
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            var payload = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString("N"),
                method,
                @params = parameters
            });
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("McpCapability");
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"MCP server returned HTTP {(int)response.StatusCode}: {body}");

            var json = NormalizeJsonResponse(body);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error))
                throw new InvalidOperationException($"MCP server returned JSON-RPC error: {error.GetRawText()}");
            if (!root.TryGetProperty("result", out var result))
                throw new InvalidOperationException("MCP server response did not contain a JSON-RPC result.");
            return result.Clone();
        }

        private static string NormalizeJsonResponse(string body)
        {
            var trimmed = (body ?? String.Empty).Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal)) return trimmed;

            foreach (var line in trimmed.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    return line.Substring(5).Trim();

            throw new InvalidOperationException("MCP server returned an unsupported response payload.");
        }

        private static string ComputeContractHash(string name, string description, string schema)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{name}\n{description}\n{schema}");
            return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
        }
    }
}
