using LagoVista.Core.Models;
using LagoVista.Core.PlatformSupport;
using LagoVista.IoT.Web.Common.Claims;
using LagoVista.UserAdmin.Models.Account;
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
            return claim == null ? "<????>" : claim.Value;
        }

        //TODO: Need checks if user is not logged in.
        protected String CurrentUserId
        {
            get { return GetClaimValue(ClaimsPrincipalFactory.CurrentUserId); }
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

        protected void SetAuditProperties(IAuditableEntity entity)
        {
            var createDate = DateTime.Now.ToJSONString();

            entity.CreationDate = createDate;
            entity.LastUpdatedDate = createDate;
            entity.CreatedBy = UserEntityHeader;
            entity.LastUpdatedBy = UserEntityHeader;
        }

        protected void SetUpdatedProperties(IAuditableEntity entity)
        {
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
                var orgId = User.Claims.Where(claim => claim.Type == ClaimsPrincipalFactory.CurrentOrgId).FirstOrDefault();
                var orgName = User.Claims.Where(claim => claim.Type == ClaimsPrincipalFactory.CurrentOrgName).FirstOrDefault();
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
            get { return GetClaimValue(ClaimsPrincipalFactory.CurrentOrgId); }
        }

        protected String CurrentUserProfileimageUrl
        {
            get { return User.Claims.Where(claim => claim.Type == ClaimsPrincipalFactory.CurrentUserProfilePictureurl).FirstOrDefault().Value; }
        }

        protected enum PopulateModes
        {
            Create,
            Update
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
    }
}
