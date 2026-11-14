using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.IoT.Web.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public sealed class QdrantAdminMcpExposureTests
    {
        [TestMethod]
        public void AdminReadOnlyEndpointsAreNotDevRestricted()
        {
            var type = typeof(QdrantAdminController);
            Assert.IsNotNull(type.GetCustomAttribute<AuthorizeAttribute>());
            Assert.IsNotNull(type.GetCustomAttribute<SystemAdminAttribute>());
            var methods = new HashSet<string>(StringComparer.Ordinal);
            foreach (var method in type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
            {
                var marker = method.GetCustomAttribute<McpEndpointAttribute>();
                Assert.IsNotNull(marker, method.Name);
                Assert.AreEqual("sys", marker.FamilyId);
                Assert.IsFalse(marker.DevOnly);
                Assert.IsNotNull(method.GetCustomAttribute<HttpGetAttribute>());
                Assert.IsTrue(methods.Add(marker.Name));
            }
            foreach (var name in new[] { "qdrant-collections", "qdrant-collection", "qdrant-points", "qdrant-point" })
                Assert.IsTrue(methods.Contains(name), name);
        }

        [TestMethod]
        public void InspectionInputsAreBoundedAndValidated()
        {
            Assert.AreEqual(1, QdrantAdminRepo.BoundPageSize(0));
            Assert.AreEqual(50, QdrantAdminRepo.BoundPageSize(1000));
            QdrantAdminRepo.ValidateCollection("sample-vectors_2");
            Assert.ThrowsException<ArgumentException>(() => QdrantAdminRepo.ValidateCollection("../admin"));
            Assert.ThrowsException<ArgumentException>(() => QdrantAdminRepo.ValidatePointId("abc/xyz"));
        }

        [TestMethod]
        public void OpaqueScrollOffsetPreservesQdrantJsonScalar()
        {
            using var stringJson = JsonDocument.Parse("\"b6f4b8cd-1c33-4df6-9b6e-25073f14d302\"");
            using var numberJson = JsonDocument.Parse("1234");
            Assert.IsFalse(String.IsNullOrWhiteSpace(QdrantAdminRepo.EncodeOffset(stringJson.RootElement)));
            Assert.IsFalse(String.IsNullOrWhiteSpace(QdrantAdminRepo.EncodeOffset(numberJson.RootElement)));
        }
    }
}
