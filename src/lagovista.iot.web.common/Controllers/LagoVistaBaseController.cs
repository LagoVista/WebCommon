// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: e150edd5581e5f26d306e006cb7eae48a463b81dcc44ea82406a05d7d3183704
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.AspNetCore.Identity;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LagoVista.Core;
using LagoVista.Core.Interfaces;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Models.UIMetaData;

namespace LagoVista.IoT.Web.Common.Controllers
{
    public class LagoVistaBaseController : Controller
    {
        readonly UserManager<AppUser> _userManager;
        readonly IAdminLogger _logger;

        public LagoVistaBaseController(UserManager<AppUser> userManager, IAdminLogger logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        protected void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
        }

        protected EntityHeader UserEntityHeader
        {
            get
            {
                return new EntityHeader()
                {
                    Id = CurrentUserId,
                    Text = CurrentFullName
                };
            }
        }

        private String GetClaimValue(String claimId)
        {
            var claim = User.Claims.Where(clm => clm.Type == claimId).FirstOrDefault();
            var value = claim == null ? "<????>" : claim.Value;
            return value;
        }

        //TODO: Need checks if user is not logged in.
        protected String CurrentUserId
        {
            get { return GetClaimValue(ClaimsFactory.CurrentUserId); }
        }

        protected String CurrentUserName
        {
            get { return GetClaimValue(ClaimTypes.NameIdentifier); }
        }

        protected String CurrentUserEmail
        {
            get { return GetClaimValue(ClaimTypes.Email); }
        }

        protected String CurrentFullName
        {
            get{ return $"{GetClaimValue(ClaimTypes.GivenName)} {GetClaimValue(ClaimTypes.Surname)}"; }
        }

        protected void SetAuditProperties(IAuditableEntitySimple entity)
        {
            var createDate = DateTime.Now.ToJSONString();

            entity.CreationDate = createDate;
            entity.LastUpdatedDate = createDate;
            entity.CreatedBy = UserEntityHeader;
            entity.LastUpdatedBy = UserEntityHeader;
        }

        public List<string> PrimaryOrgIds
        {
            get
            {
                return new List<string>()
                {
                    "AA2C78499D0140A5A9CE4B7581EF9691",
                    "C8AD4589F26842E7A1AEFBAEFC979C9B"
                };
            }
        }

        /// <summary>
        /// Primary Org is Software Logistics or the owner of the software.
        /// </summary>
        public bool IsPrimaryOrg
        {
            get
            {
                if (String.IsNullOrEmpty(CurrentOrgId))
                    return false;

                return PrimaryOrgIds.Contains(CurrentOrgId);
            }
        }

        protected void SetUpdatedProperties(IAuditableEntitySimple entity)
        {
            if(entity == null)
            {
                throw new NullReferenceException("NULL Entity Passed to SetUpdatedProperties");
            }

            entity.LastUpdatedDate = DateTime.Now.ToJSONString();
            entity.LastUpdatedBy = UserEntityHeader;
        }

        protected void SetOwnedProperties(IOwnedEntity entity)
        {
            entity.OwnerOrganization = OrgEntityHeader;
        }

        protected EntityHeader OrgEntityHeader
        {
            get
            {
                var orgId = User.Claims.Where(claim => claim.Type == ClaimsFactory.CurrentOrgId).FirstOrDefault();
                var orgName = User.Claims.Where(claim => claim.Type == ClaimsFactory.CurrentOrgName).FirstOrDefault();
                if (orgId == null)
                {
                    return new EntityHeader()
                    {
                        Text = Resources.CommonResources.Common_None,
                        Id = String.Empty
                    };
                }

                if (orgName == null)
                {
                    _logger.AddError("ControllerBase.CurrentUserOrg", $"Org has id, but no name for user id: {CurrentUserId}");
                    return new EntityHeader()
                    {
                        Text = "???????",
                        Id = String.Empty
                    };
                }

                return new EntityHeader()
                {
                    Id = orgId.Value,
                    Text = orgName.Value,
                };

            }
        }

        protected String CurrentOrgId
        {
            get { return GetClaimValue(ClaimsFactory.CurrentOrgId); }
        }

        protected String CurrentUserProfileimageUrl
        {
            get { return User.Claims.Where(claim => claim.Type == ClaimsFactory.CurrentUserProfilePictureurl).FirstOrDefault().Value; }
        }

        protected enum PopulateModes
        {
            Create,
            Update
        }

        public bool IsFinanceAdmin
        {
            get
            {
                return User.Claims.Where(clm => clm.Type == ClaimsFactory.IsFinancceAdmin).Any();
            }
        }

        public bool IsSysAdmin
        {
            get
            {
                return User.Claims.Where(clm => clm.Type == ClaimsFactory.IsSystemAdmin).Any();
            }
        }

        public bool IsOrgAdmin
        {
            get
            {
                return User.Claims.Where(clm => clm.Type == ClaimsFactory.IsOrgAdmin).Any();
            }
        }

        protected Task<AppUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
        }

        public UserManager<AppUser> UserManager
        {
            get { return _userManager; }
        }

        protected IAdminLogger Logger
        {
            get { return _logger; }
        }

        public ListRequest GetListRequestFromHeader()
        {
            var listRequest = new ListRequest();
            listRequest.Url = Request.Path;

            if(Request.Headers.ContainsKey("x-nextrowkey"))
            {
                listRequest.NextRowKey = Request.Headers["x-nextrowkey"];
            }

            if (Request.Headers.ContainsKey("x-categorykey"))
            {
                listRequest.CategoryKey = Request.Headers["x-categorykey"];
            }

            if (Request.Headers.ContainsKey("x-nextpartitionkey"))
            {
                listRequest.NextPartitionKey = Request.Headers["x-nextpartitionkey"];
            }

            if (Request.Headers.ContainsKey("x-filter-startdate"))
            {
                listRequest.StartDate = Request.Headers["x-filter-startdate"];
            }

            if (Request.Headers.ContainsKey("x-filter-enddate"))
            {
                listRequest.EndDate = Request.Headers["x-filter-enddate"];
            }

            if (Request.Headers.ContainsKey("x-show-drafts"))
            {
                listRequest.ShowDrafts = Request.Headers["x-show-drafts"] == "true";
            }

            if (Request.Headers.ContainsKey("x-show-deleted"))
            {
                listRequest.ShowDeleted = Request.Headers["x-show-deleted"] == "true";
            }

            if (Request.Headers.ContainsKey("x-group-by"))
            {
                listRequest.GroupBy = Request.Headers["x-group-by"];
            }
            
            if (Request.Headers.ContainsKey("x-group-by-size"))
            {
                if (Int32.TryParse(Request.Headers["x-group-by-size"], out int groupBySize))
                {
                    listRequest.GroupBySize = groupBySize;
                }
                else
                {
                    listRequest.GroupBySize = 1;
                }
            }
            else
            {
                listRequest.GroupBySize = 1;
            }

            if (Request.Headers.ContainsKey("x-pagesize"))
            {
                if (Int32.TryParse(Request.Headers["x-pagesize"], out int pageSize))
                {
                    listRequest.PageSize = pageSize;
                }
                else
                {
                    listRequest.PageSize = 100;
                }
            }
            else
            {
                listRequest.PageSize = 100;
            }

            if (Request.Headers.ContainsKey("x-pageindex"))
            {
                if (Int32.TryParse(Request.Headers["x-pageindex"], out int pageIndex))
                {
                    listRequest.PageIndex = pageIndex;
                }
                else
                {
                    listRequest.PageIndex = 1;
                }
            }
            else
            {
                listRequest.PageIndex = 1;
            }

            return listRequest;
        }
    }
}
