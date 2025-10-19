// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 6e5a94b6018340938a03c14ff7ecdf313e156d5a2413c1b6ca465f38381bb719
// IndexVersion: 0
// --- END CODE INDEX META ---
using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LagoVista.Core.Attributes.EntityDescriptionAttribute;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [Route("docs/metadata")]
    public class DocGenController : Controller
    {
        /// <summary>
        /// List of domains
        /// </summary>
        /// <returns></returns>
        [HttpGet("domains")]
        public IEnumerable<DomainDescription> GetDomains()
        {
            return MetaDataHelper.Instance.Domains.OrderBy(ent=>ent.Name);
        }

        /// <summary>
        /// List of entities for a  domain
        /// </summary>
        /// <param name="domain">Domain Name (key)</param>
        /// <returns></returns>
        [HttpGet("domains/{domain}")]
        public IEnumerable<EntitySummary> GetEntities(String domain)
        {
            return MetaDataHelper.Instance.EntitySummaries.OrderBy(ent=>ent.Name).Where(ent => ent.DomainKey.ToLower() == domain.ToLower());
        }

        /// <summary>
        /// List of entities for a  domain
        /// </summary>
        /// <param name="domain">Domain Name (key)</param>
        /// <returns></returns>
        [HttpGet("domains/icons")]
        public IEnumerable<EntitySummary> GetEntities()
        {
            var entities = MetaDataHelper.Instance.EntitySummaries;

            return entities.OrderBy(ent => ent.Name);
        }

        /// <summary>
        /// List of entities for a  domain
        /// </summary>
        /// <param name="domain">Domain Name (key)</param>
        /// <returns></returns>
        [HttpGet("entities/all")]
        public IEnumerable<EntityHeader> GetAllEntities()
        {
            var entities = MetaDataHelper.Instance.EntitySummaries;
            return entities.Select(ent=> EntityHeader.Create(ent.ShortClassName, ent.ShortClassName.ToLower(), ent.Name)).OrderBy(ent => ent.Text);
        }


        /// <summary>
        /// List of entities for a  domain
        /// </summary>
        /// <param name="domain">Domain Name (key)</param>
        /// <returns></returns>
        [HttpGet("/api/objects/cloneable")]
        public ListResponse<EntityHeader> GetCloneableObjectsAsync()
        {
            var entities = MetaDataHelper.Instance.EntitySummaries;
            var ehEntities = entities.Where(obj=> obj.Cloneable && obj.EntityType != EntityTypes.Summary ).Select(ent => EntityHeader.Create(ent.ShortClassName, ent.ShortClassName.ToLower(), ent.Name)).OrderBy(ent => ent.Text);
            return ListResponse<EntityHeader>.Create(ehEntities);
        }

        /// <summary>
        /// List of entities for a  domain
        /// </summary>
        /// <param name="domain">Domain Name (key)</param>
        /// <returns></returns>
        [HttpGet("entities/nuviot/coreobjects")]
        public IEnumerable<EntitySummary> GetAllNuvIoTObjects()
        {
            return MetaDataHelper.Instance.EntitySummaries.Where(obj=>obj.EntityType == EntityTypes.CoreIoTModel).OrderBy(ent=>ent.Name);
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

    public class foo :LagoVistaBaseController
    {
        public foo(UserManager<AppUser> userManager, IAdminLogger logger) : base(userManager, logger)
        {

        }
    }
}
