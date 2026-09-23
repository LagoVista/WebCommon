using LagoVista.CloudStorage.Interfaces;
using LagoVista.CloudStorage.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
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
        public async Task<ListResponse<MongoDatabaseInfo>> GetDatabasesAsync(CancellationToken ct = default)
        {
            try
            {
                return await _mongoAdminRepo.GetDatabasesAsync(GetListRequestFromHeader(), ct);
            }
            catch (Exception ex)
            {
                return ListResponse<MongoDatabaseInfo>.FromError($"Failed to load Mongo databases: {ex.Message}");
            }
        }

        [HttpGet("{database}/collections")]
        public async Task<ListResponse<MongoCollectionInfo>> GetCollectionsAsync(
            [FromRoute] string database,
            CancellationToken ct = default)
        {
            try
            {
                return await _mongoAdminRepo.GetCollectionsAsync(database, GetListRequestFromHeader(), ct);
            }
            catch (Exception ex)
            {
                return ListResponse<MongoCollectionInfo>.FromError($"Failed to load Mongo collections: {ex.Message}");
            }
        }

        [HttpPost("{database}/{collection}/query")]
        public async Task<ListResponse<MongoDocumentInfo>> QueryAsync(
            [FromRoute] string database,
            [FromRoute] string collection,
            [FromBody] MongoQueryRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                return ListResponse<MongoDocumentInfo>.FromError("Request body is required.");

            try
            {
                var listRequest = GetListRequestFromHeader();
                request.PageIndex = listRequest.PageIndex;
                request.PageSize = listRequest.PageSize;
                request.NextPartitionKey = listRequest.NextPartitionKey;
                request.NextRowKey = listRequest.NextRowKey;

                return await _mongoAdminRepo.QueryAsync(database, collection, request, ct);
            }
            catch (Exception ex)
            {
                return ListResponse<MongoDocumentInfo>.FromError($"Mongo query failed: {ex.Message}");
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

        [HttpPost("{database}/{collection}/document")]
        public async Task<InvokeResult<string>> InsertDocumentAsync(
            [FromRoute] string database,
            [FromRoute] string collection,
            [FromBody] MongoDocumentWriteRequest request,
            CancellationToken ct = default)
        {
            if (request == null || String.IsNullOrWhiteSpace(request.Json))
                return InvokeResult<string>.FromError("json is required.");

            try
            {
                var id = await _mongoAdminRepo.InsertDocumentAsync(database, collection, request.Json, ct);
                return InvokeResult<string>.Create(id);
            }
            catch (Exception ex)
            {
                return InvokeResult<string>.FromError($"Mongo insert failed: {ex.Message}");
            }
        }

        [HttpDelete("{database}/{collection}/{id}")]
        public async Task<InvokeResult> DeleteDocumentAsync(
            [FromRoute] string database,
            [FromRoute] string collection,
            [FromRoute] string id,
            CancellationToken ct = default)
        {
            try
            {
                var deleted = await _mongoAdminRepo.DeleteDocumentAsync(database, collection, id, ct);
                return deleted
                    ? InvokeResult.Success
                    : InvokeResult.FromError("Document not found.");
            }
            catch (Exception ex)
            {
                return InvokeResult.FromError($"Mongo delete failed: {ex.Message}");
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
