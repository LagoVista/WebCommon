// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 7cbc948ba45f9f5e7b14065d8636bec1a80e88ecf96ac861764f55205ac02c54
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Interfaces;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    public class CoreController : LagoVistaBaseController
    {
        private readonly ICacheProvider _cacheProvider;

        public CoreController(UserManager<AppUser> userManager, IAdminLogger logger, ICacheProvider cacheProvider) : base(userManager, logger)
        {
            _cacheProvider = cacheProvider;
        }

        [HttpGet("/api/core/schedule/factory")]
        public DetailResponse<Core.Models.Schedule> CreateSchedule()
        {
            return DetailResponse<Core.Models.Schedule>.Create();
        }

        [HttpGet("/api/core/cache/clear/{key}")]
        public Task RemoveKey(string key)
        {
            return _cacheProvider.RemoveAsync(key);
        }

    }
}
