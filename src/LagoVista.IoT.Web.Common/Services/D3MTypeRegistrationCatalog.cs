using LagoVista.CloudStorage.Storage;
using System;

namespace LagoVista.IoT.Web.Common.Services
{
    /// <summary>
    /// Code-owned allowlist for interactive D3M authoring. These are only direct reference
    /// declarations, not reflected source accessors, stable-source readers, or import adapters.
    /// Concrete domain resolvers must be implemented and verified before graph assembly.
    /// </summary>
    public static class D3MTypeRegistrationCatalog
    {
        public static void Configure(D3MTypeRegistry registry)
        {
            if (registry == null) throw new ArgumentNullException(nameof(registry));

            registry
                .Register(new D3MTypeRegistration("VirtualTeamMember", D3MStorageKind.EntityBase, allowTopLevelSelection: true)
                    .AddRelationship("Department", "VirtualTeamDepartment", D3MCardinality.One)
                    .AddRelationship("AgentPersonaDefinition", "AgentPersonaDefinition", D3MCardinality.One)
                    .AddRelationship("DefaultAgentContext", "AgentContext", D3MCardinality.One)
                    .AddRelationship("AvatarImage", "MediaResource", D3MCardinality.One)
                    .AddRelationship("EditorialImages", "MediaResource", D3MCardinality.Many)
                    .AddRelationship("PromptTokenProduct", "Product", D3MCardinality.One)
                    .AddRelationship("CompletionTokenProduct", "Product", D3MCardinality.One)
                    .AddRelationship("BusinessCapabilities", "BusinessCapability", D3MCardinality.Many)
                    .AddRelationship("EssentialJobActivities", "EssentialJobActivity", D3MCardinality.Many))
                .Register(new D3MTypeRegistration("VirtualTeamDepartment", D3MStorageKind.EntityBase)
                    .AddRelationship("HeroImage", "MediaResource", D3MCardinality.One)
                    .AddRelationship("EditorialImages", "MediaResource", D3MCardinality.Many)
                    .AddRelationship("LandscapeVideoBackground", "MediaResource", D3MCardinality.One)
                    .AddRelationship("PortraitVideoBackground", "MediaResource", D3MCardinality.One)
                    .AddRelationship("DepartmentHead", "VirtualTeamMember", D3MCardinality.One,
                        role: D3MRelationshipRole.NavigationOnly)
                    .AddRelationship("VirtualTeamMembers", "VirtualTeamMember", D3MCardinality.Many,
                        role: D3MRelationshipRole.NavigationOnly))
                .Register(new D3MTypeRegistration("AgentContext", D3MStorageKind.EntityBase)
                    .AddRelationship("DefaultAgentPersona", "AgentPersonaDefinition", D3MCardinality.One)
                    .AddRelationship("ToolBoxes", "AgentToolBox", D3MCardinality.Many)
                    .AddRelationship("ActiveTools", "AgentTool", D3MCardinality.Many)
                    .AddRelationship("AvailableTools", "AgentTool", D3MCardinality.Many)
                    .AddRelationship("InstructionDdrs", "DDR", D3MCardinality.Many)
                    .AddRelationship("ReferenceDdrs", "DDR", D3MCardinality.Many)
                    .AddRelationship("DefaultPromptTokenProduct", "Product", D3MCardinality.One)
                    .AddRelationship("DefaultCompletionTokenProduct", "Product", D3MCardinality.One)
                    .AddRelationship("DefaultReviewPromptTokenProduct", "Product", D3MCardinality.One)
                    .AddRelationship("DefaultReviewCompletionTokenProduct", "Product", D3MCardinality.One)
                    .AddRelationship("PremiumImageGenerationProduct", "Product", D3MCardinality.One)
                    .AddRelationship("StandardImageGenerationProduct", "Product", D3MCardinality.One)
                    .AddRelationship("EmbeddingProduct", "Product", D3MCardinality.One))
                .Register(new D3MTypeRegistration("AgentToolBox", D3MStorageKind.EntityBase)
                    .AddRelationship("ActiveTools", "AgentTool", D3MCardinality.Many)
                    .AddRelationship("AvailableTools", "AgentTool", D3MCardinality.Many)
                    .AddRelationship("InstructionDdrs", "DDR", D3MCardinality.Many)
                    .AddRelationship("ReferenceDdrs", "DDR", D3MCardinality.Many))
                .Register(new D3MTypeRegistration("AgentPersonaDefinition", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("AgentTool", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("DDR", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("BusinessCapability", D3MStorageKind.EntityBase)
                    .AddRelationship("Sops", "StandardOperatingProcedure", D3MCardinality.Many)
                    .AddRelationship("BusinessHealthCategory", "BusinessHealthCategory", D3MCardinality.One)
                    .AddRelationship("Product", "Product", D3MCardinality.One))
                .Register(new D3MTypeRegistration("EssentialJobActivity", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("StandardOperatingProcedure", D3MStorageKind.EntityBase)
                    .AddRelationship("OutputArtifactSpecification", "ArtifactSpecification", D3MCardinality.One)
                    .AddRelationship("AgentContext", "AgentContext", D3MCardinality.One)
                    .AddRelationship("Tools", "AgentTool", D3MCardinality.Many))
                .Register(new D3MTypeRegistration("ArtifactSpecification", D3MStorageKind.EntityBase)
                    .AddRelationship("ParentArtifactSpecification", "ArtifactSpecification", D3MCardinality.One)
                    .AddRelationship("Product", "Product", D3MCardinality.One)
                    .AddRelationship("SampleArtifactDocument", "MediaResource", D3MCardinality.One)
                    .AddRelationship("CanonicalSystemRecord", "MediaResource", D3MCardinality.One))
                .Register(new D3MTypeRegistration("BusinessHealthCategory", D3MStorageKind.EntityBase))
                .Register(new D3MTypeRegistration("Product", D3MStorageKind.Product))
                .Register(new D3MTypeRegistration("ProductCategory", D3MStorageKind.ProductCategory))
                .Register(new D3MTypeRegistration("MediaResource", D3MStorageKind.Media))
                .Register(new D3MTypeRegistration("ConversationFlowState", D3MStorageKind.ApplicationData,
                    applicationDataCollection: "ConversationFlowState"))
                .Register(new D3MTypeRegistration("ConversationFlowTransitionDefinition", D3MStorageKind.ApplicationData,
                    applicationDataCollection: "ConversationFlowTransitionDefinition"));
        }
    }
}
