using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [SystemAdmin]
    [Authorize]
    [ApiController]
    [Route("api/sysadmin/database/mongo")]
    public class MongoAdminController : LagoVistaBaseController
    {
        private readonly IMongoAdminRepo _mongoAdminRepo;

        public MongoAdminController(
            IMongoAdminRepo mongoAdminRepo,
            UserManager<AppUser> userManager,
            IAdminLogger logger)
            : base(userManager, logger)
        {
            _mongoAdminRepo = mongoAdminRepo ?? throw new ArgumentNullException(nameof(mongoAdminRepo));
        }

        [HttpGet("databases")]
        public async Task<InvokeResult<IReadOnlyList<string>>> GetDatabasesAsync(CancellationToken ct = default)
        {
            try
            {
                var databases = await _mongoAdminRepo.GetDatabasesAsync(ct);
                return InvokeResult<IReadOnlyList<string>>.Create(databases);
            }
            catch (Exception ex)
            {
                return InvokeResult<IReadOnlyList<string>>.FromError($"Failed to load Mongo databases: {ex.Message}");
            }
        }

        [HttpGet("{database}/collections")]
        public async Task<InvokeResult<IReadOnlyList<MongoCollectionInfo>>> GetCollectionsAsync(
            [FromRoute] string database,
            CancellationToken ct = default)
        {
            try
            {
                var collections = await _mongoAdminRepo.GetCollectionsAsync(database, ct);
                return InvokeResult<IReadOnlyList<MongoCollectionInfo>>.Create(collections);
            }
            catch (Exception ex)
            {
                return InvokeResult<IReadOnlyList<MongoCollectionInfo>>.FromError($"Failed to load Mongo collections: {ex.Message}");
            }
        }

        [HttpPost("{database}/{collection}/query")]
        public async Task<InvokeResult<MongoQueryResult>> QueryAsync(
            [FromRoute] string database,
            [FromRoute] string collection,
            [FromBody] MongoQueryRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                return InvokeResult<MongoQueryResult>.FromError("Request body is required.");

            try
            {
                var result = await _mongoAdminRepo.QueryAsync(database, collection, request, ct);
                return InvokeResult<MongoQueryResult>.Create(result);
            }
            catch (Exception ex)
            {
                return InvokeResult<MongoQueryResult>.FromError($"Mongo query failed: {ex.Message}");
            }
        }

        [HttpGet("{database}/{collection}/{id}")]
        public async Task<InvokeResult<string>> GetDocumentAsync(
            [FromRoute] string database,
            [FromRoute] string collection,
            [FromRoute] string id,
            CancellationToken ct = default)
        {
            try
            {
                var json = await _mongoAdminRepo.GetDocumentAsync(database, collection, id, ct);
                return json == null
                    ? InvokeResult<string>.FromError("Document not found.")
                    : InvokeResult<string>.Create(json);
            }
            catch (Exception ex)
            {
                return InvokeResult<string>.FromError($"Failed to load Mongo document: {ex.Message}");
            }
        }

        [HttpPost("{database}/{collection}/patch")]
        public async Task<InvokeResult<MongoPatchResult>> PatchManyAsync(
            [FromRoute] string database,
            [FromRoute] string collection,
            [FromBody] MongoPatchRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                return InvokeResult<MongoPatchResult>.FromError("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Patch))
                return InvokeResult<MongoPatchResult>.FromError("patch is required.");

            try
            {
                var result = await _mongoAdminRepo.PatchManyAsync(database, collection, request, ct);
                return InvokeResult<MongoPatchResult>.Create(result);
            }
            catch (Exception ex)
            {
                return InvokeResult<MongoPatchResult>.FromError($"Mongo patch failed: {ex.Message}");
            }
        }
    }
}
