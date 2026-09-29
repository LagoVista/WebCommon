using LagoVista.Core.Models.UIMetaData;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Logging.Models;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// Signed service-to-service diagnostic surface for trusted platform automation.
    /// Returns lightweight recent error summaries first, with full records available by id.
    /// </summary>
    [RequireSignedRequest]
    [Route("api/service/diagnostics/errors")]
    public class ServiceDiagnosticsController : ControllerBase
    {
        private readonly IApplicationErrorRepository _errors;

        public ServiceDiagnosticsController(IApplicationErrorRepository errors)
        {
            _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        [HttpGet]
        public async Task<ListResponse<ApplicationErrorSummary>> GetRecentErrorsAsync(
            [FromQuery] int take = 100,
            [FromQuery] string application = null,
            CancellationToken ct = default)
        {
            if (take <= 0) take = 100;
            if (take > 1000) take = 1000;

            try
            {
                var errors = await _errors.GetRecentErrorsAsync(take, application, ct);
                return ListResponse<ApplicationErrorSummary>.Create(errors);
            }
            catch (Exception ex)
            {
                return ListResponse<ApplicationErrorSummary>.FromError($"Failed to load recent application errors: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvokeResult<LogRecord>>> GetErrorAsync(
            [FromRoute] string id,
            CancellationToken ct = default)
        {
            if (String.IsNullOrWhiteSpace(id))
                return BadRequest(InvokeResult<LogRecord>.FromError("id is required."));

            try
            {
                var error = await _errors.GetErrorAsync(id.Trim(), ct);
                if (error == null)
                    return NotFound(InvokeResult<LogRecord>.FromError("Error record not found."));

                return Ok(InvokeResult<LogRecord>.Create(error));
            }
            catch (Exception ex)
            {
                return StatusCode(500, InvokeResult<LogRecord>.FromError($"Failed to load application error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Accepts one signed service-to-service diagnostic error and writes it to the
        /// same bounded local diagnostic store used by server-side application errors.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<InvokeResult<LogRecord>>> WriteErrorAsync(
            [FromBody] LogRecord error,
            CancellationToken ct = default)
        {
            if (error == null)
                return BadRequest(InvokeResult<LogRecord>.FromError("Error record is required."));

            if (String.IsNullOrWhiteSpace(error.Message))
                return BadRequest(InvokeResult<LogRecord>.FromError("Error message is required."));

            try
            {
                if (String.IsNullOrWhiteSpace(error.LogLevel))
                    error.LogLevel = "Error";

                if (error.TimeStamp == default)
                    error.TimeStamp = DateTime.UtcNow;

                await _errors.WriteErrorAsync(error, ct);
                return Ok(InvokeResult<LogRecord>.Create(error));
            }
            catch (Exception ex)
            {
                return StatusCode(500, InvokeResult<LogRecord>.FromError($"Failed to persist application error: {ex.Message}"));
            }
        }
    }
}
