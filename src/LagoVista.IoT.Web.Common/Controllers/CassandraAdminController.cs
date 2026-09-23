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
    [Route("api/sysadmin/database/cassandra")]
    public sealed class CassandraAdminController : ControllerBase
    {
        private readonly ICassandraAdminRepo _repo;

        public CassandraAdminController(ICassandraAdminRepo repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        [HttpGet("keyspace")]
        public InvokeResult<string> GetKeyspace()
        {
            return InvokeResult<string>.Create(_repo.Keyspace);
        }

        [HttpGet("tables")]
        public async Task<InvokeResult<List<CassandraTableInfo>>> GetTablesAsync(CancellationToken ct = default)
        {
            try
            {
                var tables = await _repo.GetTablesAsync(ct);
                return InvokeResult<List<CassandraTableInfo>>.Create(tables.ToList());
            }
            catch (Exception ex)
            {
                return InvokeResult<List<CassandraTableInfo>>.FromError($"Cassandra table discovery failed: {ex.Message}");
            }
        }

        [HttpPost("query")]
        public async Task<InvokeResult<CassandraQueryResult>> QueryAsync(
            [FromBody] CassandraQueryRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var result = await _repo.QueryAsync(request, ct);
                return InvokeResult<CassandraQueryResult>.Create(result);
            }
            catch (Exception ex)
            {
                return InvokeResult<CassandraQueryResult>.FromError($"Cassandra query failed: {ex.Message}");
            }
        }

        [HttpPost("execute")]
        public async Task<InvokeResult> ExecuteAsync(
            [FromBody] CassandraExecuteRequest request,
            CancellationToken ct = default)
        {
            try
            {
                await _repo.ExecuteAsync(request, ct);
                return InvokeResult.Success;
            }
            catch (Exception ex)
            {
                return InvokeResult.FromError($"Cassandra mutation failed: {ex.Message}");
            }
        }
    }
}
