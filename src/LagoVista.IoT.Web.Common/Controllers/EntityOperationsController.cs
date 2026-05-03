// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 205924010bd3d3763a047e11dea701c2ca5a05ecb40db16b50e2444d9db99099
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.CloudStorage.Interfaces;
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{

    [ConfirmedUser]
    public class EntityOperationsController : LagoVistaBaseController
    {
        private readonly IStorageUtils _storageUtils;
        private readonly IEntityUtilsRepository _entityUtils; 

        public EntityOperationsController(IStorageUtils storageUtils, IEntityUtilsRepository entityUtils, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _storageUtils = storageUtils ?? throw new ArgumentNullException(nameof(storageUtils));
            _entityUtils = entityUtils ?? throw new ArgumentNullException(nameof(entityUtils));
        }

        [HttpPut("/api/entity/{entityid}/rate/{rating}")]
        public async Task<InvokeResult<RatedEntity>> RateEntityAsync(string entityid, int rating)
        {
            var result = await _storageUtils.AddRatingAsync(entityid, rating, OrgEntityHeader, UserEntityHeader);
            return InvokeResult<RatedEntity>.Create(result);
        }

        [HttpGet("/api/entity/{id}/graph")]
        public async Task<InvokeResult<EntityGraph>> GetEntityGraph(string id)
        {
            var result = await _storageUtils.GetEntityGraphAsync(id, OrgEntityHeader, UserEntityHeader);
            return InvokeResult<EntityGraph>.Create(result);
        }

        [HttpGet("/api/entity/{id}/public/set")]
        public async Task<InvokeResult> GetPublicEntityGraph(string id)
        {
            return await _storageUtils.SetEntityPublicAsync(id, OrgEntityHeader, UserEntityHeader);
        }

        [HttpGet("/api/entities/{entitytype}")]
        public async Task<InvokeResult<List<EntityHeader>>> GetEntitiesByType(string entitytype)
        {
            var result = await _storageUtils.GetEntitiesByTypeAsync(OrgEntityHeader.Id, entitytype );
            return InvokeResult<List<EntityHeader>>.Create(result);
        }

        [HttpGet("/api/entity/{entityid}/index")]
        public async Task<InvokeResult> GetEntityIndex(string entityid, CancellationToken ct = default)
        {
            return await _entityUtils.IndexEntityAsync(entityid, OrgEntityHeader, UserEntityHeader, ct);
        }

        [HttpGet("/api/entity/{entityid}/calchash")]
        public async Task<InvokeResult> GetEntityHash(string entityid, CancellationToken ct = default)
        {
            return await _entityUtils.CalculateHashAsync(entityid, ct);
        }

        [HttpDelete("/api/entity/{entityid}/rating")]
        public async Task<InvokeResult<RatedEntity>> ClearRating(string entityid)
        {
            var result = await _storageUtils.ClearRatingAsync(entityid, OrgEntityHeader, UserEntityHeader);
            return InvokeResult<RatedEntity>.Create(result);
        }

        [HttpPut("/api/entity/{entityid}/category")]
        public Task<InvokeResult> SetEntityCategory(string entityid, [FromBody] EntityHeader category)
        {
            return _storageUtils.SetCategoryAsync(entityid, category, OrgEntityHeader, UserEntityHeader);
        }
    }
}
