using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [SystemAdmin]
    [Authorize]
    [ApiController]
    [Route("api/sysadmin/database/valkey")]
    public sealed class ValkeyAdminController : ControllerBase
    {
        private readonly IValkeyAdminRepo _repo;

        public ValkeyAdminController(IValkeyAdminRepo repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        [HttpPost("scan")]
        public async Task<InvokeResult<List<ValkeyKeyInfo>>> ScanAsync(
            [FromBody] ValkeyScanRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _repo.ScanAsync(request, ct);
                return InvokeResult<List<ValkeyKeyInfo>>.Create(result.ToList());
            }
            catch (Exception ex)
            {
                return InvokeResult<List<ValkeyKeyInfo>>.FromError($"Valkey scan failed: {ex.Message}");
            }
        }

        [HttpGet("{database:int}/{key}")]
        public async Task<InvokeResult<ValkeyKeyInfo>> GetAsync(
            [FromRoute] int database,
            [FromRoute] string key,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _repo.GetAsync(database, key, ct);
                return InvokeResult<ValkeyKeyInfo>.Create(result);
            }
            catch (Exception ex)
            {
                return InvokeResult<ValkeyKeyInfo>.FromError($"Valkey read failed: {ex.Message}");
            }
        }

        [McpEndpoint("valkey-scan", FamilyId = "sys",
            Description = "Scan for Valkey cache keys by database and glob pattern; at most 100 keys. SystemAdmin; read-only.")]
        [HttpGet("mcp/scan")]
        public Task<InvokeResult<List<ValkeyKeyInfo>>> ScanMcpAsync(
            [FromQuery] string database, [FromQuery] string pattern = null, CancellationToken ct = default)
        {
            if (!int.TryParse(database, out var index) || index < 0)
                return Task.FromResult(InvokeResult<List<ValkeyKeyInfo>>.FromError("A nonnegative database index is required."));
            return ScanAsync(new ValkeyScanRequest
            {
                Database = index,
                Pattern = String.IsNullOrWhiteSpace(pattern) ? "*" : pattern,
                PageSize = 100
            }, ct);
        }

        [McpEndpoint("valkey-get", FamilyId = "sys",
            Description = "Read one Valkey key's type, value and TTL from a nonnegative database index. SystemAdmin; read-only.")]
        [HttpGet("mcp/get")]
        public Task<InvokeResult<ValkeyKeyInfo>> GetMcpAsync(
            [FromQuery] string database, [FromQuery] string key, CancellationToken ct = default)
        {
            if (!int.TryParse(database, out var index) || index < 0 || String.IsNullOrWhiteSpace(key))
                return Task.FromResult(InvokeResult<ValkeyKeyInfo>.FromError("Nonnegative database index and key are required."));
            return GetAsync(index, key, ct);
        }

        [HttpPost("set")]
        public async Task<InvokeResult> SetAsync(
            [FromBody] ValkeySetRequest request,
            CancellationToken ct = default)
        {
            try
            {
                await _repo.SetAsync(request, ct);
                return InvokeResult.Success;
            }
            catch (Exception ex)
            {
                return InvokeResult.FromError($"Valkey set failed: {ex.Message}");
            }
        }

        [HttpDelete("{database:int}/{key}")]
        public async Task<InvokeResult> DeleteAsync(
            [FromRoute] int database,
            [FromRoute] string key,
            CancellationToken ct = default)
        {
            try
            {
                var deleted = await _repo.DeleteAsync(database, key, ct);
                return deleted ? InvokeResult.Success : InvokeResult.FromError("Key not found.");
            }
            catch (Exception ex)
            {
                return InvokeResult.FromError($"Valkey delete failed: {ex.Message}");
            }
        }
    }
}
