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

            [McpEndpoint("categories-create", Description = "Create a category for a requested entity category type in the authenticated organization.")]
            [HttpPost("/api/category")]
            public Task<InvokeResult> AddCategoryIntegration([FromBody] Category category)
            {
                return _categoryManager.AddCategoryAsync(category, OrgEntityHeader, UserEntityHeader);
            }

            [McpEndpoint("categories-update", Description = "Update a category using the complete existing category record.")]
            [HttpPut("/api/category")]
            public Task<InvokeResult> UpdateCategoryIntegration([FromBody] Category category)
            {
                return _categoryManager.UpdateCategoryAsync(category, OrgEntityHeader, UserEntityHeader);
            }

            [McpEndpoint("categories-list", Description = "List categories for an entity category type, for example sitecontent.")]
            [HttpGet("/api/categories/{categorytype}")]
            public Task<ListResponse<Category>> GetCategoresForOrg(String categoryType)
            {
                return _categoryManager.GetCategoriesAsync(categoryType, GetListRequestFromHeader(), OrgEntityHeader, UserEntityHeader);
            }

            [McpEndpoint("categories-get", Description = "Get a category by ID within the current organization.")]
            [HttpGet("/api/category/{id}")]
            public async Task<Category> GetCategory(string id)
            {
                return await _categoryManager.GetCategoryAsync(id, OrgEntityHeader, UserEntityHeader);
            }

            [McpEndpoint("categories-get-for-edit", Description = "Get a complete category model for full-record editing in the current organization.")]
            [HttpGet("/api/category/{id}/edit")]
            public async Task<DetailResponse<Category>> GetCategoryForEdit(string id)
            {
                var category = await _categoryManager.GetCategoryAsync(id, OrgEntityHeader, UserEntityHeader);
                return DetailResponse<Category>.Create(category);
            }

            [HttpDelete("/api/category/{id}")]
            public Task<InvokeResult> DeleteCategory(string id)
            {
                return _categoryManager.DeleteCategoryAsync(id, OrgEntityHeader, UserEntityHeader);
            }

            [McpEndpoint("categories-factory", Description = "Create an unsaved category for a category type, for example sitecontent.")]
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
