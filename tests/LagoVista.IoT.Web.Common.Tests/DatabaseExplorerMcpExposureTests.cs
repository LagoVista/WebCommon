using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public sealed class DatabaseExplorerMcpExposureTests
    {
        [TestMethod]
        public void EveryExplorerToolIsAvailableInSysAndReadOnly()
        {
            var types = new[] { typeof(PostgresAdminController), typeof(CassandraAdminController),
                typeof(ValkeyAdminController), typeof(MongoAdminController) };
            var tools = new HashSet<string>(StringComparer.Ordinal);

            foreach (var controller in types)
            {
                Assert.IsNotNull(controller.GetCustomAttribute<AuthorizeAttribute>(), controller.Name);
                Assert.IsNotNull(controller.GetCustomAttribute<SystemAdminAttribute>(), controller.Name);

                foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    var marker = method.GetCustomAttribute<McpEndpointAttribute>();
                    if (marker == null) continue;
                    Assert.AreEqual("sys", marker.FamilyId, method.Name);
                    Assert.IsFalse(marker.DevOnly, method.Name);
                    Assert.IsNotNull(method.GetCustomAttribute<HttpGetAttribute>(), method.Name);
                    Assert.IsTrue(tools.Add(marker.Name), marker.Name);
                    // The API's reflection MCP bridge only accepts string and CancellationToken
                    // parameters for [ApiController] SystemAdmin GET actions.
                    Assert.IsTrue(method.GetParameters().All(parameter =>
                        parameter.ParameterType == typeof(string) ||
                        parameter.ParameterType == typeof(System.Threading.CancellationToken)), method.Name);
                }
            }

            foreach (var name in new[]
            {
                "postgres-databases", "postgres-schemas", "postgres-tables", "postgres-query",
                "cassandra-keyspace", "cassandra-tables", "cassandra-query",
                "valkey-scan", "valkey-get",
                "mongo-databases", "mongo-collections", "mongo-query", "document-raw-get"
            })
                Assert.IsTrue(tools.Contains(name), name);
        }
    }
}
