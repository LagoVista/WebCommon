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

        [McpEndpoint("cassandra-keyspace", FamilyId = "sys", DevOnly = true,
            Description = "Read the configured Cassandra keyspace name. SystemAdmin, DEV only.")]
        [HttpGet("keyspace")]
        public InvokeResult<string> GetKeyspace()
        {
            return InvokeResult<string>.Create(_repo.Keyspace);
        }

        [McpEndpoint("cassandra-tables", FamilyId = "sys", DevOnly = true,
            Description = "List tables in the configured Cassandra keyspace. SystemAdmin, DEV only.")]
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

        [McpEndpoint("cassandra-query", FamilyId = "sys", DevOnly = true,
            Description = "Run a read-only SELECT CQL query in the configured Cassandra keyspace with a maximum of 500 rows. SystemAdmin, DEV only.")]
        [HttpGet("mcp/query")]
        public Task<InvokeResult<CassandraQueryResult>> QueryMcpAsync(
            [FromQuery] string cql, [FromQuery] string pageSize = null, CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(cql) || cql.Contains(";"))
                return Task.FromResult(InvokeResult<CassandraQueryResult>.FromError(
                    "Supply one read-only SELECT statement without semicolons."));
            return QueryAsync(new CassandraQueryRequest
            {
                Cql = cql,
                PageSize = int.TryParse(pageSize, out var parsed) ? Math.Max(1, Math.Min(parsed, 500)) : 100
            }, ct);
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
