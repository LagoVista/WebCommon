// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: b70e4ca46ef5eb64fbfc0e3fd20c1cd7078b0a80c74581263f2e7c82453af9dc
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.IoT.Logging.Rest.Services
{

    /// <summary>
    /// Logging Controller - Allows access to NuvIoT web site error logging
    /// </summary>
    [ConfirmedUser]
    [Authorize]
    public class LoggingController : LagoVistaBaseController
    {
        ILogReader _logReader;

        /// <summary>
        /// Create instance of logging cnotroller
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="userManager"></param>
        /// <param name="logger"></param>
        public LoggingController(ILogReader reader, UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {
            _logReader = reader;
        }

        /// <summary>
        /// Returns errors logged for the NuvIoT Web and API servers.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/logging/errors/webtools")]
        public async Task<ListResponse<LogRecord>> GetWebToolsErrorsAsync()
        {
            var result = await _logReader.GetErrorsAsync(ResourceType.WebTools, GetListRequestFromHeader());
            result.Title = "Portal Errors";
            return result;
        }

        /// <summary>
        /// Returns errors logged for the NuvIoT Web and API servers.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/logging/errors/api")]
        public async Task<ListResponse<LogRecord>> GetAPIErrorsAsync()
        {
            var result = await _logReader.GetErrorsAsync(ResourceType.API, GetListRequestFromHeader());
            result.Title = "API Errors";
            return result;
        }

        /// <summary>
        /// Returns errors logged for the NuvIoT Web and API servers.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/logging/events/webtools")]
        public async Task<ListResponse<LogRecord>> GetWebToolsEventsAsync()
        {
            var result = await _logReader.GetLogRecordsAsync(ResourceType.WebTools, GetListRequestFromHeader());
            result.Title = "Portal Events";
            return result;
        }

        /// <summary>
        /// Returns entries logged for the NuvIoT Reporting Services.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/logging/events/jobs")]
        public async Task<ListResponse<LogRecord>> GetJobEventsAsync()
        {
            var result = await _logReader.GetLogRecordsAsync(ResourceType.Scheduler, GetListRequestFromHeader());
            result.Title = "Job Logs";
            return result;
        }

        /// <summary>
        /// Returns errors logged for the NuvIoT Web and API servers.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/api/logging/events/api")]
        public async Task<ListResponse<LogRecord>> GetAPIEventsAsync()
        {
            var result = await _logReader.GetLogRecordsAsync(ResourceType.API, GetListRequestFromHeader());
            result.Title = "API Logs";
            return result;
        }
    }
}
