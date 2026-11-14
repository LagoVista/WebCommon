using LagoVista.Core.Validation;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.Relational.Storage;
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
    /// Authenticated System Admin PostgreSQL explorer. Connection identity always comes
    /// from ClusterPostgres settings; caller input selects only a connectable database.
    /// </summary>
    [Authorize]
    [SystemAdmin]
    [ApiController]
    [Route("api/sysadmin/database/postgres")]
    public sealed class PostgresAdminController : ControllerBase
    {
        private readonly IPostgresAdminRepo _repo;

        public PostgresAdminController(IPostgresAdminRepo repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        [McpEndpoint("postgres-databases", FamilyId = "sys", DevOnly = true,
            Description = "List PostgreSQL databases accessible to the cluster's configured application account. SystemAdmin, DEV only; no credentials returned.")]
        [HttpGet("databases")]
        public async Task<InvokeResult<List<PostgresDatabaseInfo>>> GetDatabasesAsync(CancellationToken ct = default)
        {
            try
            {
                var databases = await _repo.GetDatabasesAsync(ct);
                return InvokeResult<List<PostgresDatabaseInfo>>.Create(databases.ToList());
            }
            catch (Exception ex)
            {
                return InvokeResult<List<PostgresDatabaseInfo>>.FromError($"PostgreSQL database discovery failed: {ex.Message}");
            }
        }

        [McpEndpoint("postgres-schemas", FamilyId = "sys", DevOnly = true,
            Description = "List visible schemas in a selected PostgreSQL database. SystemAdmin, DEV only.")]
        [HttpGet("{database}/schemas")]
        public async Task<InvokeResult<List<string>>> GetSchemasAsync([FromRoute] string database, CancellationToken ct = default)
        {
            try
            {
                var schemas = await _repo.GetSchemasAsync(database, ct);
                return InvokeResult<List<string>>.Create(schemas.ToList());
            }
            catch (Exception ex)
            {
                return InvokeResult<List<string>>.FromError($"PostgreSQL schema discovery failed: {ex.Message}");
            }
        }

        [McpEndpoint("postgres-tables", FamilyId = "sys", DevOnly = true,
            Description = "List PostgreSQL tables and views in one selected database and schema. SystemAdmin, DEV only.")]
        [HttpGet("{database}/{schema}/tables")]
        public async Task<InvokeResult<List<PostgresTableInfo>>> GetTablesAsync(
            [FromRoute] string database, [FromRoute] string schema, CancellationToken ct = default)
        {
            try
            {
                var tables = await _repo.GetTablesAsync(database, schema, ct);
                return InvokeResult<List<PostgresTableInfo>>.Create(tables.ToList());
            }
            catch (Exception ex)
            {
                return InvokeResult<List<PostgresTableInfo>>.FromError($"PostgreSQL table discovery failed: {ex.Message}");
            }
        }

        // POST is the UI's normal generated-client query contract.
        [HttpPost("query")]
        public async Task<InvokeResult<PostgresQueryResult>> QueryAsync(
            [FromBody] PostgresQueryRequest request, CancellationToken ct = default)
        {
            try
            {
                var result = await _repo.QueryAsync(request, ct);
                return InvokeResult<PostgresQueryResult>.Create(result);
            }
            catch (Exception ex)
            {
                return InvokeResult<PostgresQueryResult>.FromError($"PostgreSQL query failed: {ex.Message}");
            }
        }

        // Separate GET action lets the MCP SDK correctly advertise ReadOnly=true.
        // It delegates to precisely the same bounded, read-only repository as POST.
        [McpEndpoint("postgres-query", FamilyId = "sys", DevOnly = true,
            Description = "Execute one bounded, read-only PostgreSQL SELECT/WITH/VALUES/TABLE query in DEV. Supply database, sql and optional pageSize (1-500). SystemAdmin only; no mutations.")]
        [HttpGet("query")]
        public async Task<InvokeResult<PostgresQueryResult>> QueryMcpAsync(
            [FromQuery] string database, [FromQuery] string sql,
            [FromQuery] int pageSize = 100, CancellationToken ct = default)
        {
            return await QueryAsync(new PostgresQueryRequest
            {
                Database = database,
                Sql = sql,
                PageSize = pageSize
            }, ct);
        }
    }
}
