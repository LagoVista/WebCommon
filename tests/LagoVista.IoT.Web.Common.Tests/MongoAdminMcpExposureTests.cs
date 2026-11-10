using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public sealed class MongoAdminMcpExposureTests
    {
        [TestMethod]
        public void RawDocumentGet_IsDevOnlyAndSystemAdminProtected()
        {
            var controller = typeof(MongoAdminController);
            Assert.IsNotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            Assert.IsNotNull(controller.GetCustomAttribute<SystemAdminAttribute>());

            var method = controller.GetMethod(nameof(MongoAdminController.GetDocumentAsync));
            Assert.IsNotNull(method);
            var marker = method.GetCustomAttribute<McpEndpointAttribute>();
            Assert.IsNotNull(marker);
            Assert.AreEqual("document-raw-get", marker.Name);
            Assert.AreEqual("sys", marker.FamilyId);
            Assert.IsTrue(marker.DevOnly);
            Assert.AreEqual("{database}/{collection}/{id}", method.GetCustomAttribute<HttpGetAttribute>()?.Template);
        }

        [TestMethod]
        public void MutationEndpoints_AreNeverMcpExposed()
        {
            var methods = typeof(MongoAdminController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (var method in methods.Where(method =>
                method.IsDefined(typeof(HttpPostAttribute), true) ||
                method.IsDefined(typeof(HttpPutAttribute), true) ||
                method.IsDefined(typeof(HttpPatchAttribute), true) ||
                method.IsDefined(typeof(HttpDeleteAttribute), true)))
            {
                Assert.IsNull(method.GetCustomAttribute<McpEndpointAttribute>(),
                    $"Write action {method.Name} must not be MCP exposed.");
            }
        }
    }
}
