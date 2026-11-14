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
    public sealed class PostgresAdminMcpExposureTests
    {
        [TestMethod]
        public void McpToolsAreReadOnlyAcrossEnvironmentsAndSystemAdminProtected()
        {
            var controller = typeof(PostgresAdminController);
            Assert.IsNotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            Assert.IsNotNull(controller.GetCustomAttribute<SystemAdminAttribute>());

            foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var marker = method.GetCustomAttribute<McpEndpointAttribute>();
                if (marker == null) continue;

                Assert.AreEqual("sys", marker.FamilyId);
                Assert.IsFalse(marker.DevOnly);
                Assert.IsNotNull(method.GetCustomAttribute<HttpGetAttribute>(), method.Name);
            }

            Assert.AreEqual("postgres-query",
                controller.GetMethod(nameof(PostgresAdminController.QueryMcpAsync))
                    ?.GetCustomAttribute<McpEndpointAttribute>()?.Name);

            Assert.IsNull(controller.GetMethod(nameof(PostgresAdminController.QueryAsync))
                ?.GetCustomAttribute<McpEndpointAttribute>());
        }
    }
}
