using LagoVista.Core.Interfaces;
using LagoVista.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/process")]
    public sealed class ProcessController : ControllerBase
    {
        private readonly IProcessManager _manager;

        public ProcessController(IProcessManager manager)
        {
            _manager = manager;
        }

        [HttpPost("definitions")]
        public Task<ProcessDefinition> CreateDefinition([FromBody] ProcessDefinition definition)
            => _manager.CreateDefinitionAsync(definition);

        [HttpGet("definitions/{id}")]
        public Task<ProcessDefinition> GetDefinition(string id)
            => _manager.GetDefinitionAsync(id);

        [HttpPut("definitions/{id}")]
        public Task<ProcessDefinition> UpdateDefinition(string id, [FromBody] ProcessDefinition definition)
        {
            definition.Id = id;
            return _manager.UpdateDefinitionAsync(definition);
        }

        [HttpPost("definitions/{definitionId}/instances")]
        public Task<ProcessInstance> Start(string definitionId)
            => _manager.StartAsync(definitionId);

        [HttpGet("instances/{id}")]
        public Task<ProcessInstance> GetInstance(string id)
            => _manager.GetInstanceAsync(id);

        [HttpPost("instances/{id}/transitions/{transitionId}")]
        public Task<ProcessInstance> Transition(string id, string transitionId)
            => _manager.TransitionAsync(id, transitionId);

        [HttpPost("instances/{id}/pause")]
        public Task<ProcessInstance> Pause(string id)
            => _manager.PauseAsync(id);

        [HttpPost("instances/{id}/resume")]
        public Task<ProcessInstance> Resume(string id)
            => _manager.ResumeAsync(id);
    }
}
