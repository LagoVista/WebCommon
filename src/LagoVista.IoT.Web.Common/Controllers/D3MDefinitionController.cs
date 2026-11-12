using LagoVista.AspNetCore.Identity.Managers;
using LagoVista.CloudStorage.Storage;
using LagoVista.Core.Models;
using LagoVista.Core.Validation;
using LagoVista.IoT.Logging.Loggers;
using LagoVista.IoT.Web.Common.Attributes;
using LagoVista.UserAdmin.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Common.Controllers
{
    /// <summary>
    /// Administrative D3M authoring. Every MCP action is marked for the sys family,
    /// and DEV-only is enforced by both the MCP host and the REST action itself.
    /// There is no cross-environment import, raw type activation, or caller-selected org.
    /// </summary>
    [Authorize]
    [SystemAdmin]
    [Route("api/sysadmin/d3m")]
    public sealed class D3MDefinitionController : LagoVistaBaseController
    {
        private readonly D3MDefinitionService _definitions;
        private readonly IWebHostEnvironment _environment;

        public D3MDefinitionController(
            D3MDefinitionService definitions,
            IWebHostEnvironment environment,
            UserManager<AppUser> userManager,
            IAdminLogger logger)
            : base(userManager, logger)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        [McpEndpoint("d3m-draft-list", FamilyId = "sys", DevOnly = true,
            Description = "List editable D3M definition drafts for the authenticated operator's current organization in DEV. Read only.")]
        [HttpGet("drafts")]
        public async Task<InvokeResult<StoragePageResult<D3MDefinitionRecord>>> ListDraftsAsync(
            [FromQuery] int pageSize = 100,
            [FromQuery] string continuationToken = null,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<StoragePageResult<D3MDefinitionRecord>>.FromError(scope.Error);
            try
            {
                var page = await _definitions.ListDraftsAsync(scope.Organization.Id,
                    new StoragePageRequest(pageSize, continuationToken), ct);
                return InvokeResult<StoragePageResult<D3MDefinitionRecord>>.Create(page);
            }
            catch (ArgumentException ex)
            {
                return InvokeResult<StoragePageResult<D3MDefinitionRecord>>.FromError(ex.Message);
            }
        }

        [McpEndpoint("d3m-draft-get", FamilyId = "sys", DevOnly = true,
            Description = "Get a D3M draft and its opaque expectedVersion concurrency token for the current organization. Fetch before updating or publishing.")]
        [HttpGet("draft/{typeKey}")]
        public async Task<InvokeResult<D3MDraftView>> GetDraftAsync(
            [FromRoute] string typeKey,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<D3MDraftView>.FromError(scope.Error);
            if (String.IsNullOrWhiteSpace(typeKey))
                return InvokeResult<D3MDraftView>.FromError("typeKey is required.");

            var current = await _definitions.GetDraftAsync(typeKey.Trim(), scope.Organization.Id, ct);
            return current == null
                ? InvokeResult<D3MDraftView>.FromError("D3M draft not found.")
                : InvokeResult<D3MDraftView>.Create(new D3MDraftView
                {
                    Definition = current.Record,
                    ExpectedVersion = current.ConcurrencyToken.Value,
                    Validation = _definitions.Validate(current.Record)
                });
        }

        [McpEndpoint("d3m-draft-create", FamilyId = "sys", DevOnly = true,
            Description = "Create an editable D3M draft in DEV for a registered domain type. The server assigns the current organization, ID and revision; this does not publish the definition.")]
        [HttpPost("draft")]
        public async Task<InvokeResult<D3MDraftView>> CreateDraftAsync(
            [FromBody] D3MDraftInput input,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<D3MDraftView>.FromError(scope.Error);
            if (input == null || String.IsNullOrWhiteSpace(input.TypeKey))
                return InvokeResult<D3MDraftView>.FromError("A draft and registered typeKey are required.");

            var draft = ToRecord(input, scope.Organization);
            try
            {
                await _definitions.CreateDraftAsync(draft, ct);
                var current = await _definitions.GetDraftAsync(draft.TypeKey, scope.Organization.Id, ct);
                return current == null
                    ? InvokeResult<D3MDraftView>.FromError("Draft created but cannot be retrieved.")
                    : InvokeResult<D3MDraftView>.Create(new D3MDraftView
                    {
                        Definition = current.Record,
                        ExpectedVersion = current.ConcurrencyToken.Value,
                        Validation = _definitions.Validate(current.Record)
                    });
            }
            catch (InvalidOperationException ex)
            {
                return InvokeResult<D3MDraftView>.FromError(ex.Message);
            }
        }

        [McpEndpoint("d3m-draft-update", FamilyId = "sys", DevOnly = true,
            Description = "Replace the editable D3M draft using the full draft fields and opaque expectedVersion obtained from d3m-draft-get. Reject stale versions; preserve publication history.")]
        [HttpPut("draft/{typeKey}")]
        public async Task<InvokeResult<D3MDraftView>> UpdateDraftAsync(
            [FromRoute] string typeKey,
            [FromBody] D3MDraftUpdateRequest request,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<D3MDraftView>.FromError(scope.Error);
            if (request?.Draft == null || String.IsNullOrWhiteSpace(request.ExpectedVersion) ||
                String.IsNullOrWhiteSpace(typeKey) || !String.Equals(typeKey, request.Draft.TypeKey, StringComparison.Ordinal))
                return InvokeResult<D3MDraftView>.FromError("Matching typeKey, full draft, and expectedVersion are required.");

            try
            {
                await _definitions.UpdateDraftIfVersionAsync(ToRecord(request.Draft, scope.Organization),
                    ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct);
                return await GetDraftAsync(typeKey, ct);
            }
            catch (InvalidOperationException ex)
            {
                return InvokeResult<D3MDraftView>.FromError(ex.Message);
            }
        }

        [McpEndpoint("d3m-validate", FamilyId = "sys", DevOnly = true,
            Description = "Validate a proposed D3M without saving or publishing it; checks the allowlisted type, immediate relationships and publication invariants.")]
        [HttpPost("validate")]
        public async Task<InvokeResult<D3MValidationResult>> ValidateAsync(
            [FromBody] D3MDraftInput input,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<D3MValidationResult>.FromError(scope.Error);
            if (input == null || String.IsNullOrWhiteSpace(input.TypeKey))
                return InvokeResult<D3MValidationResult>.FromError("A definition and typeKey are required.");
            return InvokeResult<D3MValidationResult>.Create(
                _definitions.Validate(ToRecord(input, scope.Organization)));
        }

        [McpEndpoint("d3m-publish", FamilyId = "sys", DevOnly = true,
            Description = "Publish a validated D3M draft as an immutable revision in DEV. Requires reviewed expectedVersion from d3m-draft-get. Uses authenticated SystemAdmin and current organization; does not promote content to LIVE.")]
        [HttpPost("publish/{typeKey}")]
        public async Task<InvokeResult<D3MPublishedDefinitionRecord>> PublishAsync(
            [FromRoute] string typeKey,
            [FromBody] D3MVersionRequest request,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<D3MPublishedDefinitionRecord>.FromError(scope.Error);
            if (String.IsNullOrWhiteSpace(typeKey) || String.IsNullOrWhiteSpace(request?.ExpectedVersion))
                return InvokeResult<D3MPublishedDefinitionRecord>.FromError("typeKey and expectedVersion are required.");
            try
            {
                var snapshot = await _definitions.PublishAsync(typeKey.Trim(), scope.Organization.Id,
                    scope.UserId, ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct);
                return InvokeResult<D3MPublishedDefinitionRecord>.Create(snapshot);
            }
            catch (InvalidOperationException ex)
            {
                return InvokeResult<D3MPublishedDefinitionRecord>.FromError(ex.Message);
            }
        }

        [McpEndpoint("d3m-published-get", FamilyId = "sys", DevOnly = true,
            Description = "Get the latest active, immutable published D3M for a registered type in the current organization. Read only.")]
        [HttpGet("published/{typeKey}")]
        public async Task<InvokeResult<D3MPublishedDefinitionRecord>> GetPublishedAsync(
            [FromRoute] string typeKey,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<D3MPublishedDefinitionRecord>.FromError(scope.Error);
            if (String.IsNullOrWhiteSpace(typeKey))
                return InvokeResult<D3MPublishedDefinitionRecord>.FromError("typeKey is required.");

            var record = await _definitions.GetPublishedAsync(typeKey.Trim(), scope.Organization.Id, ct);
            return record == null
                ? InvokeResult<D3MPublishedDefinitionRecord>.FromError("No active published D3M.")
                : InvokeResult<D3MPublishedDefinitionRecord>.Create(record);
        }

        [McpEndpoint("d3m-revision-get", FamilyId = "sys", DevOnly = true,
            Description = "Read one specific immutable D3M publication revision, including retired or superseded versions; read only.")]
        [HttpGet("revision/{typeKey}/{revision}")]
        public async Task<InvokeResult<D3MPublishedDefinitionRecord>> GetRevisionAsync(
            [FromRoute] string typeKey,
            [FromRoute] int revision,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<D3MPublishedDefinitionRecord>.FromError(scope.Error);
            if (String.IsNullOrWhiteSpace(typeKey) || revision < 1)
                return InvokeResult<D3MPublishedDefinitionRecord>.FromError("typeKey and positive revision are required.");
            var record = await _definitions.GetRevisionAsync(typeKey.Trim(), scope.Organization.Id, revision, ct);
            return record == null
                ? InvokeResult<D3MPublishedDefinitionRecord>.FromError("D3M revision not found.")
                : InvokeResult<D3MPublishedDefinitionRecord>.Create(record);
        }

        [McpEndpoint("d3m-revisions", FamilyId = "sys", DevOnly = true,
            Description = "List immutable published D3M revision history for one registered type in the current organization. Read only.")]
        [HttpGet("revisions/{typeKey}")]
        public async Task<InvokeResult<StoragePageResult<D3MPublishedDefinitionRecord>>> ListRevisionsAsync(
            [FromRoute] string typeKey,
            [FromQuery] int pageSize = 100,
            [FromQuery] string continuationToken = null,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<StoragePageResult<D3MPublishedDefinitionRecord>>.FromError(scope.Error);
            if (String.IsNullOrWhiteSpace(typeKey))
                return InvokeResult<StoragePageResult<D3MPublishedDefinitionRecord>>.FromError("typeKey is required.");
            try
            {
                var page = await _definitions.ListRevisionsAsync(typeKey.Trim(), scope.Organization.Id,
                    new StoragePageRequest(pageSize, continuationToken), ct);
                return InvokeResult<StoragePageResult<D3MPublishedDefinitionRecord>>.Create(page);
            }
            catch (ArgumentException ex)
            {
                return InvokeResult<StoragePageResult<D3MPublishedDefinitionRecord>>.FromError(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return InvokeResult<StoragePageResult<D3MPublishedDefinitionRecord>>.FromError(ex.Message);
            }
        }

        [McpEndpoint("d3m-retire", FamilyId = "sys", DevOnly = true,
            Description = "Retire an active D3M definition in DEV using a reviewed expectedVersion. Previously published immutable revisions are retained.")]
        [HttpPost("retire/{typeKey}")]
        public async Task<InvokeResult<string>> RetireAsync(
            [FromRoute] string typeKey,
            [FromBody] D3MVersionRequest request,
            CancellationToken ct = default)
        {
            var scope = await AuthorizedScopeAsync();
            if (!scope.Allowed) return InvokeResult<string>.FromError(scope.Error);
            if (String.IsNullOrWhiteSpace(typeKey) || String.IsNullOrWhiteSpace(request?.ExpectedVersion))
                return InvokeResult<string>.FromError("typeKey and expectedVersion are required.");
            try
            {
                var token = await _definitions.RetireIfVersionAsync(typeKey.Trim(), scope.Organization.Id,
                    ApplicationDataConcurrencyToken.FromValue(request.ExpectedVersion), ct);
                return InvokeResult<string>.Create(token.Value);
            }
            catch (InvalidOperationException ex)
            {
                return InvokeResult<string>.FromError(ex.Message);
            }
        }

        private async Task<D3MOperatorScope> AuthorizedScopeAsync()
        {
            // SystemAdminAttribute is an MVC action filter and is not executed by MCP
            // reflection. Re-check persisted user and organization in this method for
            // identical REST and MCP security semantics.
            if (!String.Equals(_environment.EnvironmentName, "DEV", StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(_environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
                return D3MOperatorScope.Deny("D3M authoring is only available in DEV.");

            if (User?.Identity?.IsAuthenticated != true ||
                !User.HasClaim(ClaimsFactory.IsSystemAdmin, Boolean.TrueString))
                return D3MOperatorScope.Deny("An authenticated SystemAdmin is required.");

            var user = await GetCurrentUserAsync();
            var organization = OrgEntityHeader;
            var userId = CurrentUserId;
            if (user?.IsSystemAdmin != true ||
                String.IsNullOrWhiteSpace(userId) ||
                !String.Equals(user.Id, userId, StringComparison.Ordinal) ||
                String.IsNullOrWhiteSpace(organization.Id) ||
                !String.Equals(user.CurrentOrganization?.Id, organization.Id, StringComparison.Ordinal))
                return D3MOperatorScope.Deny("SystemAdmin authorization or selected organization has changed.");

            return D3MOperatorScope.Allow(organization, userId);
        }

        private static D3MDefinitionRecord ToRecord(D3MDraftInput input, EntityHeader organization)
            => new D3MDefinitionRecord
            {
                Id = D3MIdentity.Definition(input.TypeKey),
                Organization = organization,
                TypeKey = input.TypeKey,
                Name = input.Name,
                Description = input.Description,
                StorageKind = input.StorageKind,
                ApplicationDataCollection = input.ApplicationDataCollection,
                Enabled = input.Enabled,
                TopLevelSelectable = input.TopLevelSelectable,
                ExplicitLeaf = input.ExplicitLeaf,
                Dependencies = input.Dependencies ?? new List<D3MDependencyRule>()
            };

        private sealed class D3MOperatorScope
        {
            public bool Allowed { get; private set; }
            public string Error { get; private set; }
            public EntityHeader Organization { get; private set; }
            public string UserId { get; private set; }
            public static D3MOperatorScope Deny(string message) => new D3MOperatorScope { Error = message };
            public static D3MOperatorScope Allow(EntityHeader organization, string userId) =>
                new D3MOperatorScope { Allowed = true, Organization = organization, UserId = userId };
        }
    }

    /// <summary>Editable fields only. Caller cannot set ID, owner, audit, or publication provenance.</summary>
    public sealed class D3MDraftInput
    {
        public string TypeKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public D3MStorageKind StorageKind { get; set; }
        public string ApplicationDataCollection { get; set; }
        public bool Enabled { get; set; } = true;
        public bool TopLevelSelectable { get; set; }
        public bool ExplicitLeaf { get; set; }
        public List<D3MDependencyRule> Dependencies { get; set; } = new List<D3MDependencyRule>();
    }

    public sealed class D3MDraftUpdateRequest
    {
        public string ExpectedVersion { get; set; }
        public D3MDraftInput Draft { get; set; }
    }

    public sealed class D3MVersionRequest
    {
        public string ExpectedVersion { get; set; }
    }

    public sealed class D3MDraftView
    {
        public D3MDefinitionRecord Definition { get; set; }
        public string ExpectedVersion { get; set; }
        public D3MValidationResult Validation { get; set; }
    }
}
