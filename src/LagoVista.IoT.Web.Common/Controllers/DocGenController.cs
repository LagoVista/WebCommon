using LagoVista.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.Iot.Web.Common.Controllers
{
    [Route("metadata/dox")]
    public class DocGenController : Controller
    {
        [HttpGet("domains")]
        public List<DomainDescription> GetDomains()
        {
            var domains = new List<DomainDescription>();

            return domains;
        }

        [HttpGet("domains/{domain}")]
        public List<EntityDescriptionAttribute> GetEntities(String domain)
        {
            var entities = new List<EntityDescriptionAttribute>();

            return entities;
        }

        [HttpGet("domains/{domain}/{entity}")]
        public List<FormFieldAttribute> GetEntities(String domain, String entity)
        {
            var formFields = new List<FormFieldAttribute>();

            return formFields;
        }
    }
}
