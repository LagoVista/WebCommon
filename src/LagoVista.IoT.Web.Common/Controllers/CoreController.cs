using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LagoVista.IoT.Web.Common.Controllers
{
    public class CoreController : LagoVistaBaseController
    {
        public CoreController(UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
        }

        [HttpGet("/api/core/schedule/factory")]
        public DetailResponse<Core.Models.Schedule> CreateSchedule()
        {
            return DetailResponse<Core.Models.Schedule>.Create();
        }
    }
}
