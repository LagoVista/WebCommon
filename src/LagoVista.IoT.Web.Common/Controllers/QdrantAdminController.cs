using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// System Admin read-only collection/point inspection using the configured
    /// Qdrant server. No writes, indexing, collection initialization, or raw vectors.
    /// </summary>
    [Authorize]
    [SystemAdmin]
    [ApiController]
    [Route("api/sysadmin/database/qdrant")]
    public sealed class QdrantAdminController : ControllerBase
    {
        private readonly IQdrantAdminRepo _repo;

        public QdrantAdminController(IQdrantAdminRepo repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        [McpEndpoint("qdrant-collections", FamilyId = "sys",
            Description = "List configured Qdrant collections, up to 200 names. SystemAdmin only; read-only.")]
        [HttpGet("collections")]
        public async Task<InvokeResult<List<string>>> GetCollectionsAsync(CancellationToken ct = default)
        {
            try
            {
                return InvokeResult<List<string>>.Create((await _repo.GetCollectionsAsync(ct)).ToList());
            }
            catch (Exception ex)
            {
                return InvokeResult<List<string>>.FromError($"Qdrant collection discovery failed: {ex.Message}");
            }
        }

        [McpEndpoint("qdrant-collection", FamilyId = "sys",
            Description = "Inspect one Qdrant collection's status, point count and vector schema. SystemAdmin; read-only.")]
        [HttpGet("collection")]
        public async Task<InvokeResult<QdrantAdminCollectionInfo>> GetCollectionAsync(
            [FromQuery] string collection, CancellationToken ct = default)
        {
            try
            {
                return InvokeResult<QdrantAdminCollectionInfo>.Create(await _repo.GetCollectionAsync(collection, ct));
            }
            catch (Exception ex)
            {
                return InvokeResult<QdrantAdminCollectionInfo>.FromError($"Qdrant collection inspection failed: {ex.Message}");
            }
        }

        [McpEndpoint("qdrant-points", FamilyId = "sys",
            Description = "Inspect up to 50 point IDs and bounded JSON payloads in one Qdrant collection, with opaque page offset; vectors excluded. SystemAdmin; read-only.")]
        [HttpGet("points")]
        public async Task<InvokeResult<QdrantAdminPointsPage>> GetPointsAsync(
            [FromQuery] string collection, [FromQuery] int limit = 20,
            [FromQuery] string offset = null, CancellationToken ct = default)
        {
            try
            {
                return InvokeResult<QdrantAdminPointsPage>.Create(
                    await _repo.ScrollPointsAsync(collection, limit, offset, ct));
            }
            catch (Exception ex)
            {
                return InvokeResult<QdrantAdminPointsPage>.FromError($"Qdrant point listing failed: {ex.Message}");
            }
        }

        [McpEndpoint("qdrant-point", FamilyId = "sys",
            Description = "Inspect one Qdrant point ID and bounded payload metadata; vector values excluded. SystemAdmin; read-only.")]
        [HttpGet("point")]
        public async Task<InvokeResult<QdrantAdminPointInfo>> GetPointAsync(
            [FromQuery] string collection, [FromQuery] string id, CancellationToken ct = default)
        {
            try
            {
                return InvokeResult<QdrantAdminPointInfo>.Create(await _repo.GetPointAsync(collection, id, ct));
            }
            catch (Exception ex)
            {
                return InvokeResult<QdrantAdminPointInfo>.FromError($"Qdrant point inspection failed: {ex.Message}");
            }
        }
    }
}
