// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: e34e92869c5b84b4e80f42fdca4bc066f8a8fb4f20bf165bc3b925d0efdb4026
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using LagoVista.CloudStorage;
using LagoVista.Core.Validation;
using LagoVista.Core.Models;
using LagoVista.UserAdmin.Models.Users;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Interfaces;

namespace LagoVista.IoT.Web.Common.Controllers
{
    namespace LagoVista.ProjectManagement.Rest.Controllers
    {
        [ConfirmedUser]
        [Authorize]
        public class CategoryController : LagoVistaBaseController
        {
            private readonly ICategoryManager _categoryManager;

            public CategoryController(ICategoryManager categoryManager, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
            {
                this._categoryManager = categoryManager ?? throw new ArgumentNullException(nameof(categoryManager));
            }

            [HttpPost("/api/category")]
            public Task<InvokeResult> AddCategoryIntegration([FromBody] Category category)
            {
                return _categoryManager.AddCategoryAsync(category, OrgEntityHeader, UserEntityHeader);
            }

            [HttpPut("/api/category")]
            public Task<InvokeResult> UpdateCategoryIntegration([FromBody] Category category)
            {
                return _categoryManager.UpdateCategoryAsync(category, OrgEntityHeader, UserEntityHeader);
            }

            [HttpGet("/api/categories/{categorytype}")]
            public Task<ListResponse<Category>> GetCategoresForOrg(String categoryType)
            {
                return _categoryManager.GetCategoriesAsync(categoryType, GetListRequestFromHeader(), OrgEntityHeader, UserEntityHeader);
            }

            [HttpGet("/api/category/{id}")]
            public async Task<Category> GetCategory(string id)
            {
                return await _categoryManager.GetCategoryAsync(id, OrgEntityHeader, UserEntityHeader);
            }

            [HttpDelete("/api/category/{id}")]
            public Task<InvokeResult> DeleteCategory(string id)
            {
                return _categoryManager.DeleteCategoryAsync(id, OrgEntityHeader, UserEntityHeader);
            }

            [HttpGet("/api/category/{categorytype}/factory")]
            public Category NewCategory(string categoryType)
            {
                var category = new Category(categoryType);
                category.CategoryType = categoryType;
                SetOwnedProperties(category);
                SetAuditProperties(category);
                return category;
            }
        }
    }
}
