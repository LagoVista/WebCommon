using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Services
{
    public sealed class QdrantAdminCollectionInfo
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string OptimizerStatus { get; set; }
        public long? PointsCount { get; set; }
        public long? IndexedVectorsCount { get; set; }
        public long? SegmentsCount { get; set; }
        public string VectorsJson { get; set; }
        public string PayloadSchemaJson { get; set; }
    }

    public sealed class QdrantAdminPointInfo
    {
        public string Id { get; set; }
        public string PayloadJson { get; set; }
        public bool PayloadTruncated { get; set; }
    }

    public sealed class QdrantAdminPointsPage
    {
        public string Collection { get; set; }
        public List<QdrantAdminPointInfo> Points { get; set; } = new List<QdrantAdminPointInfo>();
        public string NextOffset { get; set; }
    }

    public interface IQdrantAdminRepo
    {
        Task<IReadOnlyList<string>> GetCollectionsAsync(CancellationToken ct = default);
        Task<QdrantAdminCollectionInfo> GetCollectionAsync(string collection, CancellationToken ct = default);
        Task<QdrantAdminPointsPage> ScrollPointsAsync(string collection, int limit, string offset, CancellationToken ct = default);
        Task<QdrantAdminPointInfo> GetPointAsync(string collection, string id, CancellationToken ct = default);
    }

    /// <summary>
    /// Read-only Qdrant inventory. Uses the same Qdrant:Uri and Qdrant:ApiKey
    /// configuration as platform health and application RAG. Does not initialize
    /// collections, create indexes, or write vectors.
    /// </summary>
    public sealed class QdrantAdminRepo : IQdrantAdminRepo
    {
        private const int MaxResponseBytes = 1024 * 1024;
        private const int MaxPayloadCharacters = 32768;
        private const int MaxPageSize = 50;
        private readonly IHttpClientFactory _factory;
        private readonly Uri _endpoint;
        private readonly string _apiKey;

        public QdrantAdminRepo(IConfiguration configuration, IHttpClientFactory factory)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            var section = configuration.GetSection("Qdrant");
            var uri = section["Uri"];
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) ||
                !String.IsNullOrEmpty(parsed.UserInfo))
                throw new InvalidOperationException("Qdrant:Uri must be an HTTP(S) endpoint in host configuration.");
            _endpoint = parsed;
            _apiKey = section["ApiKey"];
        }

        public static void ValidateCollection(string collection)
        {
            if (String.IsNullOrWhiteSpace(collection) || !Regex.IsMatch(collection,
                @"^[a-zA-Z0-9._-]{1,255}$", RegexOptions.CultureInvariant))
                throw new ArgumentException("Invalid Qdrant collection name.", nameof(collection));
        }

        public static void ValidatePointId(string pointId)
        {
            if (String.IsNullOrWhiteSpace(pointId) || !Regex.IsMatch(pointId,
                @"^[a-zA-Z0-9-]{1,80}$", RegexOptions.CultureInvariant))
                throw new ArgumentException("Invalid Qdrant point ID.", nameof(pointId));
        }

        public static int BoundPageSize(int limit) => Math.Min(MaxPageSize, Math.Max(1, limit));

        private async Task<JsonDocument> SendAsync(HttpMethod method, string path, string json, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, new Uri(_endpoint, "/" + path.TrimStart('/')));
            if (!String.IsNullOrWhiteSpace(_apiKey))
                request.Headers.TryAddWithoutValidation("api-key", _apiKey);
            if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _factory.CreateClient("QdrantAdmin")
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Qdrant returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(chunk, 0, chunk.Length, ct).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + count > MaxResponseBytes)
                    throw new InvalidOperationException("Qdrant response exceeds the 1 MB administrative preview limit.");
                buffer.Write(chunk, 0, count);
            }
            buffer.Position = 0;
            return await JsonDocument.ParseAsync(buffer, cancellationToken: ct).ConfigureAwait(false);
        }

        private static JsonElement Result(JsonDocument document)
        {
            if (!document.RootElement.TryGetProperty("result", out var result) ||
                result.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Qdrant response does not contain a result.");
            return result;
        }

        private static string StringProperty(JsonElement item, string name)
        {
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var v))
                return v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText();
            return null;
        }

        private static long? LongProperty(JsonElement item, string name)
        {
            return item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var v) &&
                v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var value) ? value : (long?)null;
        }

        private static string JsonProperty(JsonElement item, string name)
        {
            return item.ValueKind == JsonValueKind.Object && item.TryGetProperty(name, out var value)
                ? value.GetRawText() : null;
        }

        public async Task<IReadOnlyList<string>> GetCollectionsAsync(CancellationToken ct = default)
        {
            using var json = await SendAsync(HttpMethod.Get, "collections", null, ct).ConfigureAwait(false);
            var result = Result(json);
            if (!result.TryGetProperty("collections", out var collections) || collections.ValueKind != JsonValueKind.Array)
                return Array.Empty<string>();
            return collections.EnumerateArray()
                .Select(x => StringProperty(x, "name")).Where(x => !String.IsNullOrEmpty(x))
                .OrderBy(x => x, StringComparer.Ordinal)
                .Take(200).ToList();
        }

        public async Task<QdrantAdminCollectionInfo> GetCollectionAsync(string collection, CancellationToken ct = default)
        {
            ValidateCollection(collection);
            using var json = await SendAsync(HttpMethod.Get, $"collections/{Uri.EscapeDataString(collection)}", null, ct).ConfigureAwait(false);
            var data = Result(json);
            var config = data.TryGetProperty("config", out var cfg) ? cfg : default;
            var parameters = config.ValueKind == JsonValueKind.Object &&
                config.TryGetProperty("params", out var p) ? p : default;
            var payloadSchema = JsonProperty(data, "payload_schema");
            return new QdrantAdminCollectionInfo
            {
                Name = collection,
                Status = StringProperty(data, "status"),
                OptimizerStatus = StringProperty(data, "optimizer_status"),
                PointsCount = LongProperty(data, "points_count"),
                IndexedVectorsCount = LongProperty(data, "indexed_vectors_count"),
                SegmentsCount = LongProperty(data, "segments_count"),
                VectorsJson = JsonProperty(parameters, "vectors"),
                PayloadSchemaJson = payloadSchema
            };
        }

        private static QdrantAdminPointInfo ToPoint(JsonElement point)
        {
            var id = point.TryGetProperty("id", out var idValue) ?
                (idValue.ValueKind == JsonValueKind.String ? idValue.GetString() : idValue.GetRawText()) : null;
            var payload = JsonProperty(point, "payload");
            var oversized = payload != null && payload.Length > MaxPayloadCharacters;
            return new QdrantAdminPointInfo
            {
                Id = id,
                PayloadJson = oversized ? "{\"_preview\":\"Payload exceeds 32 KB; not returned.\"}" : payload,
                PayloadTruncated = oversized
            };
        }

        public static string EncodeOffset(JsonElement offset)
        {
            return offset.ValueKind == JsonValueKind.Null || offset.ValueKind == JsonValueKind.Undefined
                ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.GetRawText()));
        }

        private static JsonElement DecodeOffset(string token)
        {
            if (token.Length > 2048) throw new ArgumentException("Invalid page offset.", nameof(token));
            byte[] data;
            try { data = Convert.FromBase64String(token); }
            catch (FormatException) { throw new ArgumentException("Invalid page offset.", nameof(token)); }
            using var json = JsonDocument.Parse(data);
            if (json.RootElement.ValueKind != JsonValueKind.String &&
                json.RootElement.ValueKind != JsonValueKind.Number)
                throw new ArgumentException("Invalid page offset.", nameof(token));
            return json.RootElement.Clone();
        }

        public async Task<QdrantAdminPointsPage> ScrollPointsAsync(string collection, int limit, string offset, CancellationToken ct = default)
        {
            ValidateCollection(collection);
            var body = new Dictionary<string, object>
            {
                ["limit"] = BoundPageSize(limit),
                ["with_payload"] = true,
                ["with_vector"] = false
            };
            if (!String.IsNullOrWhiteSpace(offset)) body["offset"] = DecodeOffset(offset);
            using var json = await SendAsync(HttpMethod.Post,
                $"collections/{Uri.EscapeDataString(collection)}/points/scroll",
                JsonSerializer.Serialize(body), ct).ConfigureAwait(false);
            var result = Result(json);
            var page = new QdrantAdminPointsPage { Collection = collection };
            if (result.TryGetProperty("points", out var points) && points.ValueKind == JsonValueKind.Array)
                page.Points = points.EnumerateArray().Take(MaxPageSize).Select(ToPoint).ToList();
            if (result.TryGetProperty("next_page_offset", out var next))
                page.NextOffset = EncodeOffset(next);
            return page;
        }

        public async Task<QdrantAdminPointInfo> GetPointAsync(string collection, string id, CancellationToken ct = default)
        {
            ValidateCollection(collection);
            ValidatePointId(id);
            using var json = await SendAsync(HttpMethod.Get,
                $"collections/{Uri.EscapeDataString(collection)}/points/{Uri.EscapeDataString(id)}",
                null, ct).ConfigureAwait(false);
            return ToPoint(Result(json));
        }
    }
}
