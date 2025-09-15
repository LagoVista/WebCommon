using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{

    public class EntityOperationsController : LagoVistaBaseController
    {
        IStorageUtils _storageUtils;

        public EntityOperationsController(IStorageUtils storageUtils, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _storageUtils = storageUtils ?? throw new ArgumentNullException(nameof(storageUtils));
        }

        [HttpPut("/api/entity/{entityid}/rate/{rating}")]
        public async Task<InvokeResult<RatedEntity>> RateEntityAsync(string entityid, int rating)
        {
            var result = await _storageUtils.AddRatingAsync(entityid, rating, OrgEntityHeader, UserEntityHeader);
            return InvokeResult<RatedEntity>.Create(result);
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
