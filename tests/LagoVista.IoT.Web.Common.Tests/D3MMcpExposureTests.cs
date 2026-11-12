using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.IoT.Web.Common.Controllers;
using LagoVista.IoT.Web.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Tests
{
    [TestClass]
    public sealed class D3MMcpExposureTests
    {
        [TestMethod]
        public void AllAuthoringActionsAreDevOnlySysToolsWithAdminAuthorization()
        {
            var controller = typeof(D3MDefinitionController);
            Assert.IsNotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
            Assert.IsNotNull(controller.GetCustomAttribute<SystemAdminAttribute>());
            Assert.IsNull(controller.GetCustomAttribute<ApiControllerAttribute>(),
                "MCP's reflection adapter does not execute ApiController validation filters on writes.");
            Assert.AreEqual("api/sysadmin/d3m", controller.GetCustomAttribute<RouteAttribute>()?.Template);

            var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttribute<McpEndpointAttribute>() != null).ToList();
            var expected = new[]
            {
                "d3m-draft-list", "d3m-draft-get", "d3m-draft-create",
                "d3m-draft-update", "d3m-validate", "d3m-publish",
                "d3m-published-get", "d3m-revision-get", "d3m-revisions", "d3m-retire"
            };
            CollectionAssert.AreEquivalent(expected, actions
                .Select(action => action.GetCustomAttribute<McpEndpointAttribute>().Name).ToArray());
            foreach (var action in actions)
            {
                var marker = action.GetCustomAttribute<McpEndpointAttribute>();
                Assert.AreEqual("sys", marker.FamilyId, action.Name);
                Assert.IsTrue(marker.DevOnly, action.Name);
                Assert.IsFalse(String.IsNullOrWhiteSpace(marker.Description), action.Name);
                Assert.IsTrue(action.IsDefined(typeof(HttpGetAttribute), false) ||
                    action.IsDefined(typeof(HttpPostAttribute), false) ||
                    action.IsDefined(typeof(HttpPutAttribute), false), action.Name);
            }
        }

        [TestMethod]
        public void D3MListsExposeHouseStyleListResponseAndPagingInputs()
        {
            var drafts = typeof(D3MDefinitionController).GetMethod(nameof(D3MDefinitionController.ListDraftsAsync));
            var revisions = typeof(D3MDefinitionController).GetMethod(nameof(D3MDefinitionController.ListRevisionsAsync));

            Assert.AreEqual(typeof(Task<ListResponse<D3MDefinitionRecord>>), drafts.ReturnType);
            Assert.AreEqual(typeof(Task<ListResponse<D3MPublishedDefinitionRecord>>), revisions.ReturnType);

            var draftParameters = drafts.GetParameters().Select(parameter => parameter.Name).ToArray();
            var revisionParameters = revisions.GetParameters().Select(parameter => parameter.Name).ToArray();
            foreach (var parameterName in new[] { "pageSize", "pageIndex", "nextPartitionKey", "nextRowKey" })
            {
                CollectionAssert.Contains(draftParameters, parameterName);
                CollectionAssert.Contains(revisionParameters, parameterName);
            }

            Assert.AreEqual("drafts", drafts.GetCustomAttribute<HttpGetAttribute>()?.Template);
            Assert.AreEqual("revisions/{typeKey}", revisions.GetCustomAttribute<HttpGetAttribute>()?.Template);
        }

        [TestMethod]
        public void SourceTypeRegistryAllowsReviewedVtmImmediateReferences()
        {
            var registry = new D3MTypeRegistry();
            D3MTypeRegistrationCatalog.Configure(registry);
            Assert.IsTrue(registry.TryGet("VirtualTeamMember", out var type));
            Assert.IsTrue(type.AllowTopLevelSelection);
            Assert.AreEqual(D3MStorageKind.EntityBase, type.StorageKind);
            Assert.IsTrue(type.TryGetRelationship("Department", out var department));
            Assert.AreEqual("VirtualTeamDepartment", department.TargetTypeKey);
            Assert.AreEqual(D3MCardinality.One, department.Cardinality);
            Assert.IsTrue(type.TryGetRelationship("EditorialImages", out var media));
            Assert.AreEqual(D3MCardinality.Many, media.Cardinality);
            Assert.IsFalse(type.TryGetRelationship("Nonexistent", out _));

            var draft = new D3MDefinitionRecord
            {
                Id = D3MIdentity.Definition("VirtualTeamMember"),
                TypeKey = "VirtualTeamMember",
                Organization = new EntityHeader { Id = "ORG-A" },
                Name = "Virtual Team Member",
                StorageKind = D3MStorageKind.EntityBase,
                Enabled = true,
                TopLevelSelectable = true,
                Dependencies = new List<D3MDependencyRule>
                {
                    new D3MDependencyRule
                    {
                        PropertyPath = "Department",
                        TargetTypeKey = "VirtualTeamDepartment",
                        Cardinality = D3MCardinality.One,
                        Presence = D3MPresence.Required
                    },
                    new D3MDependencyRule
                    {
                        PropertyPath = "EditorialImages",
                        TargetTypeKey = "MediaResource",
                        Cardinality = D3MCardinality.Many,
                        Presence = D3MPresence.Optional
                    }
                }
            };

            var validation = new D3MValidator(registry).Validate(draft);
            Assert.IsTrue(validation.IsValid,
                String.Join("; ", validation.Issues.Select(issue => issue.Code + ": " + issue.Message)));

            draft.Dependencies[0].PropertyPath = "UnregisteredProperty";
            Assert.IsFalse(new D3MValidator(registry).Validate(draft).IsValid);
        }

        [TestMethod]
        public void NoUnknownTypeOrAutomaticLeafAssumption()
        {
            var registry = new D3MTypeRegistry();
            D3MTypeRegistrationCatalog.Configure(registry);
            Assert.IsFalse(registry.Contains("UntrustedCLRType"));
            var draft = new D3MDefinitionRecord
            {
                Id = D3MIdentity.Definition("AgentContext"),
                TypeKey = "AgentContext",
                Organization = new EntityHeader { Id = "ORG-A" },
                Name = "Agent Context",
                StorageKind = D3MStorageKind.EntityBase,
                Enabled = true
            };
            Assert.IsFalse(new D3MValidator(registry).Validate(draft).IsValid);
        }
    }
}
