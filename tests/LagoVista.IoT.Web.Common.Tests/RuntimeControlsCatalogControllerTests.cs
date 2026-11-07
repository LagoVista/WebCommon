using LagoVista.CloudStorage.RuntimeControls;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public sealed class RuntimeControlsCatalogControllerTests
    {
        [TestMethod]
        public void Get_UsesRegisteredCatalogWithoutListingUnwiredControls()
        {
            var controller = new RuntimeControlsCatalogController();

            var response = controller.Get();

            Assert.IsTrue(response.Successful);
            Assert.IsNotNull(response.Result);
            var expected = RuntimeControlCatalog.Definitions.Where(definition => definition.VisibleInAdmin).ToList();
            CollectionAssert.AreEquivalent(
                expected.Select(definition => definition.Key).ToList(),
                response.Result.Select(definition => definition.Key).ToList());
            Assert.IsTrue(response.Result.All(definition => !String.IsNullOrWhiteSpace(definition.Title)));
            Assert.IsTrue(response.Result.Any(definition => definition.Key == RuntimeControlCatalog.S3FileStorageDiagnostics));
            Assert.IsFalse(response.Result.Any(definition => definition.Key == RuntimeControlCatalog.SlowQueryThreshold));
        }

        [TestMethod]
        public void Endpoint_RequiresAuthenticatedSystemAdmin()
        {
            var controller = typeof(RuntimeControlsCatalogController);
            Assert.IsNotNull(Attribute.GetCustomAttribute(controller, typeof(AuthorizeAttribute)));
            Assert.IsNotNull(Attribute.GetCustomAttribute(controller, typeof(SystemAdminAttribute)));
            var route = (RouteAttribute)Attribute.GetCustomAttribute(controller, typeof(RouteAttribute));
            Assert.AreEqual("api/sysadmin/runtime-controls", route.Template);
        }
    }
}
