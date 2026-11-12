# D3M authoring through the sys MCP family

**Implementation:** `LagoVista/WebCommon`  
**Backend:** `LagoVista/CloudStorage` `D3MDefinitionService` (stable CloudStorage package with ListRequest/ListResponse D3M lists)  
**Host:** existing API-host MCP action discovery, **not** a separate MCP server  
**MCP family URL:** `/sys/mcp` (plus aggregate `/mcp`)  
**REST prefix:** `/api/sysadmin/d3m`

## Tools

| MCP tool | REST method + path | Notes |
| --- | --- | --- |
| `d3m-draft-list` | `GET /drafts` | Org-scoped `ListResponse<D3MDefinitionRecord>`, cursor-paged |
| `d3m-draft-get` | `GET /draft/{typeKey}` | Returns complete draft, validation issues, `expectedVersion` |
| `d3m-draft-create` | `POST /draft` | Takes `D3MDraftInput`; assigns organization and ID on server |
| `d3m-draft-update` | `PUT /draft/{typeKey}` | Takes `D3MDraftUpdateRequest` (full draft plus `expectedVersion`) |
| `d3m-validate` | `POST /validate` | Takes `D3MDraftInput`; no write |
| `d3m-publish` | `POST /publish/{typeKey}` | Takes `D3MVersionRequest` with `expectedVersion`; creates immutable revision |
| `d3m-published-get` | `GET /published/{typeKey}` | Read current active published revision |
| `d3m-revision-get` | `GET /revision/{typeKey}/{revision}` | Read exact immutable revision |
| `d3m-revisions` | `GET /revisions/{typeKey}` | `ListResponse<D3MPublishedDefinitionRecord>`, cursor-paged |
| `d3m-retire` | `POST /retire/{typeKey}` | Takes `D3MVersionRequest`; disables active definition, retains revision history |

All actions are annotated with `[McpEndpoint(..., FamilyId = "sys", DevOnly = true)]`, `[Authorize]`, and `[SystemAdmin]`. The controller explicitly verifies the deployment is DEV, checks the authenticated SystemAdmin user's current persisted privileges and selected organization, and derives the organization and actor exclusively from that identity. The API MCP adapter independently checks OIDC, user, organization, privileges and the DEV environment. **No tool accepts an organization ID, custom collection, type activation, or a destination for promotion.**

## Standard ListRequest / ListResponse paging

Both D3M list methods accept the platform's `ListRequest` and return `ListResponse<T>`, rather than returning `StoragePageResult<T>` or wrapping it in `InvokeResult`. The underlying storage provider still uses opaque continuation tokens internally, but the external wire contract follows our house style:

- REST clients set `x-pagesize` (default 100), `x-pageindex` (default 1), and then `x-nextpartitionkey` / `x-nextrowkey` from the prior list response.
- MCP callers supply the optional `pageSize`, `pageIndex`, `nextPartitionKey`, and `nextRowKey` tool arguments (because the MCP transport does not carry the REST paging headers).
- The response has `model`, `recordCount` (returned page size), `pageSize`, `pageIndex`, `hasMoreRecords`, `nextPartitionKey`, and `nextRowKey`. `pageCount` is not an exact total for cursor-based results.
- After page one, provide **both** continuation keys from the previous response. Page indexes alone do not work with the cursor-backed Mongo ApplicationData query. The server rejects mismatched list scopes or missing cursors.
- Page sizes are bounded to 1–1000; authorization, organization scope, and sorting are preserved across pages.

## First authoring session

1. Review `LagoVista/CloudStorage/docs/d3m-vtm-first-review.md`; agree on direct references and their presence policies.
2. `d3m-draft-create` with `typeKey = VirtualTeamMember`, `storageKind = EntityBase`, `topLevelSelectable = true` and approved direct dependencies.
3. `d3m-draft-get` to obtain the saved full draft and opaque `expectedVersion`.
4. `d3m-validate` on the proposed complete draft. Resolve all issues; a referenced type must have a registered code-owned key/path.
5. `d3m-publish` with the retrieved `expectedVersion` after operator review. A stale draft requires a new get/review.
6. `d3m-published-get` and `d3m-revisions` verify the immutable publication and history.

Drafts and immutable published snapshots are separate ApplicationData record types/collections. They do not transfer records between environments or assert that any domain record is stable/approved. In particular, a VTM D3M being published does **not** allow executing environment promotion without stable source snapshots and published D3Ms for all referenced types.

## Code-owned registry boundaries

`D3MTypeRegistrationCatalog` lists the first source-reviewed direct reference paths for `VirtualTeamMember`, `VirtualTeamDepartment`, `AgentContext`, `AgentToolBox`, and related types, plus fixed transfer classifications and known `ConversationFlowState`/`ConversationFlowTransitionDefinition` ApplicationData collection names. It deliberately does not auto-publish these maps, and does not add an arbitrary CLR type resolver or implement a graph assembler.

The first release is *authoring-only*. Nested visitor-flow paths, additional media/video references, resolver implementation, graph assembly, stable record snapshots, DEV manifest service, LIVE planner/import and post-import indexing remain separate milestones. Do not treat the registry entries as a completed domain reader/handler.

## Deployment / verification

After CloudStorage and WebCommon packages are built and published, update the DEV API host to consume the new WebCommon package, deploy it, and reconnect/refresh the `sys` MCP connection. Check `tools/list` on `/sys/mcp` for the ten `d3m-*` tool names. Confirm a non-SystemAdmin, a different selected organization and a LIVE host cannot invoke publication. Verify create/get/validate/publish/get revision for a test organization before publishing the actual VTM map.

**Never assert an MCP tool is deployed merely because the source attribute is committed.**
