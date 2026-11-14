using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public sealed class SeaweedAdminMcpExposureTests
    {
        [TestMethod]
        public void InventoryToolsAreSystemAdminProtectedReadOnlyAndEnvironmentIndependent()
        {
            var type = typeof(SeaweedAdminController);
            Assert.IsNotNull(type.GetCustomAttribute<AuthorizeAttribute>());
            Assert.IsNotNull(type.GetCustomAttribute<SystemAdminAttribute>());
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var marker = method.GetCustomAttribute<McpEndpointAttribute>();
                Assert.IsNotNull(marker, method.Name);
                Assert.AreEqual("sys", marker.FamilyId);
                Assert.IsFalse(marker.DevOnly);
                Assert.IsNotNull(method.GetCustomAttribute<HttpGetAttribute>(), method.Name);
                Assert.IsTrue(names.Add(marker.Name), marker.Name);
                Assert.IsTrue(method.GetParameters().All(p =>
                    p.ParameterType == typeof(string) || p.ParameterType == typeof(CancellationToken)));
            }

            foreach (var name in new[] { "seaweed-buckets", "seaweed-objects", "seaweed-object", "seaweed-read-url" })
                Assert.IsTrue(names.Contains(name), name);
        }
    }
}
