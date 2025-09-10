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
        public Task<RatedEntity> RateEntityAsync(string id, int rating)
        {
            return _storageUtils.AddRatingAsync(id, rating, OrgEntityHeader, UserEntityHeader);
        }


        [HttpPut("/api/entity/{entityid}/category")]
        public Task<InvokeResult> SetEntityCategory(string id, [FromBody] EntityHeader category)
        {
            return _storageUtils.SetCategoryAsync(id, category, OrgEntityHeader, UserEntityHeader);
        }
    }
}
