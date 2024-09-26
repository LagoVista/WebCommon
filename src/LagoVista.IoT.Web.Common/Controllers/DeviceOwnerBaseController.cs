using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.Core.Exceptions;
using LagoVista.Core.Models;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [DeviceOwner]
    [Authorize]
    public class DeviceOwnerBaseController : Controller
    {
        IAdminLogger _logger;
        public DeviceOwnerBaseController(IAdminLogger adminLogger)
        {
            _logger = adminLogger ?? throw new ArgumentNullException(nameof(adminLogger));
            Console.WriteLine("======>>> We are in the Device Owner Base controller....");
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
            var claim = User.Claims.SingleOrDefault(clm => clm.Type == claimId);
            if (claim == null)
                throw new NotAuthorizedException($"Missing claim: {claimId}");

            return claim.Value;
        }

        protected String CurrentUserId
        {
            get { return GetClaimValue(ClaimsFactory.CurrentUserId); }
        }

        protected String CurrentUserName
        {
            get { return GetClaimValue(ClaimTypes.NameIdentifier); }
        }

        protected String CurrentFullName
        {
            get { return $"{GetClaimValue(ClaimTypes.GivenName)} {GetClaimValue(ClaimTypes.Surname)}"; }
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

        private bool IsDeviceAuthUser()
        {
            Console.WriteLine("------------------------");

            foreach (var claim in User.Claims)
                Console.WriteLine($"{claim.Type} = {claim.Value}");

            Console.WriteLine("------------------------");

            return User.Claims.SingleOrDefault(clm => clm.Type == ClaimsFactory.Logintype)?.Value == nameof(DeviceOwnerUser);
        }

        protected EntityHeader CurrentDevice
        {
            get
            {
                if (!IsDeviceAuthUser())
                    throw new NotAuthorizedException("Not a device pin auth user");

                var id = User.Claims.Single(clm => clm.Type == ClaimsFactory.DeviceUniqueId).Value;
                var name = User.Claims.Single(clm => clm.Type == ClaimsFactory.DeviceName).Value;
                return EntityHeader.Create(id, name);
            }
        }

        protected EntityHeader CurrentDeviceRepo
        {
            get
            {
                if (!IsDeviceAuthUser())
                    throw new NotAuthorizedException("Not a device pin auth user");

                var id = User.Claims.Single(clm => clm.Type == ClaimsFactory.DeviceRepoId).Value;
                var name = User.Claims.Single(clm => clm.Type == ClaimsFactory.DeviceRepoName).Value;
                return EntityHeader.Create(id, name);
            }
        }


    }
}
