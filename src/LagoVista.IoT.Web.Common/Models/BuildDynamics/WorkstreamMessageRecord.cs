using LagoVista.Core.Interfaces;
using System;

namespace LagoVista.IoT.Web.Common.Models.BuildDynamics
{
    public sealed class WorkstreamMessageRecord : IActivityRecord
    {
        public string Id { get; set; } = String.Empty;
        public string OrganizationId { get; set; } = String.Empty;
        public string Organization { get; set; } = String.Empty;
        public DateTime CreationDate { get; set; }
        public string WorkstreamId { get; set; } = String.Empty;
        public string TaskId { get; set; } = String.Empty;
        public string WorkspaceId { get; set; } = String.Empty;
        public string FromRole { get; set; } = String.Empty;
        public string FromId { get; set; } = String.Empty;
        public string ToRole { get; set; } = String.Empty;
        public string ToId { get; set; } = String.Empty;
        public string MessageType { get; set; } = String.Empty;
        public string Summary { get; set; } = String.Empty;
        public string Body { get; set; } = String.Empty;
        public bool AttentionRequired { get; set; }
        public string AttentionAction { get; set; } = String.Empty;
        public string ThreadId { get; set; } = String.Empty;
        public string ReplyToMessageId { get; set; } = String.Empty;
    }
}
