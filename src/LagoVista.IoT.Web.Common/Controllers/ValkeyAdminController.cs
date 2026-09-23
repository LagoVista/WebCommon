using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Authentication;
using LagoVista.Core.Models;
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
