using LagoVista.UserAdmin.Models.Account;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Claims
{
    public class ClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, Role>
    {
        public const string None = "-";

        public const string CurrentOrgName = "com.lagovista.iot.currentorgname";
        public const string CurrentOrgId = "com.lagovista.iot.currentorgid";
        public const string EmailVerified = "com.lagovista.iot.emailverified";
        public const string PhoneVerfiied = "com.lagovista.iot.phoneverified";
        public const string IsSystemAdmin = "com.lagovista.iot.issystemadmin";
        public const string CurrentUserProfilePictureurl = "com.lagovista.iot.currentprofilepictureurl";

        public ClaimsPrincipalFactory(
            UserManager<AppUser> userManager,
            RoleManager<Role> roleManager,
            IOptions<IdentityOptions> optionsAccessor) : base(userManager, roleManager, optionsAccessor)
        {
        }

        public async override Task<ClaimsPrincipal> CreateAsync(AppUser user)
        {
            var principal = await base.CreateAsync(user);

            ((ClaimsIdentity)principal.Identity).AddClaims(new[] {
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(EmailVerified, user.EmailConfirmed.ToString()),
            new Claim(PhoneVerfiied, user.PhoneNumberConfirmed.ToString()),
            new Claim(IsSystemAdmin, user.IsSystemAdmin.ToString()),
            new Claim(CurrentOrgName, user.CurrentOrganization == null ? None : user.CurrentOrganization.Text),
            new Claim(CurrentOrgId, user.CurrentOrganization == null ? None : user.CurrentOrganization.Id),
            new Claim(CurrentUserProfilePictureurl, user.ProfileImageUrl.ImageUrl),
            });

            return principal;
        }
    }
}
