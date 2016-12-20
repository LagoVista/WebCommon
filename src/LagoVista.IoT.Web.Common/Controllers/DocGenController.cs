using LagoVista.Core.Attributes;
using LagoVista.Core.Models.UIMetaData;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [Route("metadata/dox")]
    public class DocGenController : Controller
    {
        /// <summary>
        /// List of domains
        /// </summary>
        /// <returns></returns>
        [HttpGet("domains")]
        public List<DomainDescription> GetDomains()
        {
            return MetaDataHelper.Instance.Domains;
        }

        /// <summary>
        /// List of entities for a  domain
        /// </summary>
        /// <param name="domain">Domain Name (key)</param>
        /// <returns></returns>
        [HttpGet("domains/{domain}")]
        public IEnumerable<EntitySummary> GetEntities(String domain)
        {
            return MetaDataHelper.Instance.EntitySummaries.Where(ent => ent.DomainKey.ToLower() == domain.ToLower());
        }

        /// <summary>
        /// Entity Detail
        /// </summary>
        /// <param name="domain">Domain Name (key)</param>
        /// <param name="classname">Class Name</param>
        /// <returns></returns>
        [HttpGet("entity/{domain}/{classname}")]
        public EntityDescription GetEntity(String domain, String classname)
        {
            return MetaDataHelper.Instance.Entities.Where(ent => ent.DomainName.ToLower() == domain.ToLower() && ent.Name.ToLower() == classname.ToLower()).FirstOrDefault();
        }
    }
}
