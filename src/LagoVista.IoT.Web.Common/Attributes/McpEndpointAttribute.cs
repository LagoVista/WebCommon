using System;

namespace LagoVista.IoT.Web.Common.Attributes
{
    /// <summary>
    /// Explicitly exposes an existing REST controller action as an MCP tool.
    /// The API host discovers these markers; attributes do not change REST routing or authorization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class McpEndpointAttribute : Attribute
    {
        public McpEndpointAttribute(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("MCP tool name is required.", nameof(name));

            Name = name;
        }

        public string Name { get; }
        public string Description { get; set; }

        /// <summary>
        /// Optional MCP tool family. An unset family makes the tool available in every family endpoint.
        /// </summary>
        public string FamilyId { get; set; }
    }
}
