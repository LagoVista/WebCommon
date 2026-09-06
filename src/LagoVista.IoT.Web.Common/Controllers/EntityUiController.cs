using LagoVista.Core.Models.UIMetaData;
using LagoVista.IoT.Web.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace LagoVista.IoT.Web.Common.Controllers
{
    [ConfirmedUser]
    [Route("api/ui/entity")]
    public class EntityUiController : Controller
    {
        [HttpGet("{entityType}")]
        public EntityUiBootstrap GetEntity(string entityType)
        {
            if (String.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Entity type is required.", nameof(entityType));

            var summary = GetUniqueEntitySummary(entityType);
            if (summary == null)
                return null;

            var description = GetEntityDescription(summary.ClassName);
            return EntityUiBootstrap.Create(summary.ShortClassName, description);
        }

        [HttpGet("resolve")]
        public EntityUiBootstrap Resolve([FromQuery] string path)
        {
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path is required.", nameof(path));

            var normalizedPath = NormalizePath(path);
            var matches = MetaDataHelper.Instance.EntitySummaries
                .Select(summary => TryResolve(summary, normalizedPath))
                .Where(result => result != null)
                .ToList();

            if (matches.Count == 0)
                return null;

            if (matches.Count > 1)
                throw new InvalidOperationException($"Multiple entity UI routes matched '{normalizedPath}'. This is a metadata design-time error.");

            var match = matches.Single();
            var uniqueSummary = GetUniqueEntitySummary(match.EntityType);
            if (uniqueSummary == null)
                return null;

            var description = GetEntityDescription(uniqueSummary.ClassName);
            var result = EntityUiBootstrap.Create(uniqueSummary.ShortClassName, description);
            if (result == null)
                return null;

            result.Mode = match.Mode;
            result.Id = match.Id;

            return result;
        }

        private static EntitySummary GetUniqueEntitySummary(string entityType)
        {
            var matches = MetaDataHelper.Instance.EntitySummaries
                .Where(entity => String.Equals(entity.ShortClassName, entityType, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
                return null;

            if (matches.Count > 1)
                throw new InvalidOperationException($"Multiple entity descriptions were found for class name '{entityType}'. Class names must be globally unique.");

            return matches.Single();
        }

        private static EntityDescription GetEntityDescription(string className)
        {
            if (String.IsNullOrWhiteSpace(className))
                return null;

            var matches = MetaDataHelper.Instance.Entities
                .Where(entity => String.Equals(entity.Name, className, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
                return null;

            if (matches.Count > 1)
                throw new InvalidOperationException($"Multiple entity descriptions were found for '{className}'. This is a metadata design-time error.");

            return matches.Single();
        }

        private static EntityRouteMatch TryResolve(EntitySummary summary, string path)
        {
            if (PathEquals(summary.UiCreateUrl, path))
                return new EntityRouteMatch(summary.ShortClassName, EntityUiModes.Create, null);

            if (PathEquals(summary.UiGetListUrl, path))
                return new EntityRouteMatch(summary.ShortClassName, EntityUiModes.List, null);

            if (TryMatchEditRoute(summary.UiEditUrl, path, out var id))
                return new EntityRouteMatch(summary.ShortClassName, EntityUiModes.Edit, id);

            return null;
        }

        private static bool PathEquals(string configuredPath, string requestedPath)
        {
            if (String.IsNullOrWhiteSpace(configuredPath))
                return false;

            return String.Equals(NormalizePath(configuredPath), requestedPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryMatchEditRoute(string template, string requestedPath, out string id)
        {
            id = null;

            if (String.IsNullOrWhiteSpace(template))
                return false;

            var normalizedTemplate = NormalizePath(template);
            const string token = "{id}";
            var tokenIndex = normalizedTemplate.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (tokenIndex < 0)
                return false;

            var prefix = normalizedTemplate.Substring(0, tokenIndex);
            var suffix = normalizedTemplate.Substring(tokenIndex + token.Length);

            if (!requestedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                !requestedPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return false;

            var idLength = requestedPath.Length - prefix.Length - suffix.Length;
            if (idLength <= 0)
                return false;

            id = requestedPath.Substring(prefix.Length, idLength);
            return !id.Contains("/");
        }

        private static string NormalizePath(string path)
        {
            var value = path.Trim();

            if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
                value = absoluteUri.AbsolutePath;
            else
            {
                var queryIndex = value.IndexOf('?');
                if (queryIndex >= 0)
                    value = value.Substring(0, queryIndex);

                var fragmentIndex = value.IndexOf('#');
                if (fragmentIndex >= 0)
                    value = value.Substring(0, fragmentIndex);
            }

            value = Uri.UnescapeDataString(value);

            if (!value.StartsWith("/"))
                value = "/" + value;

            if (value.Length > 1)
                value = value.TrimEnd('/');

            return value;
        }

        private sealed class EntityRouteMatch
        {
            public EntityRouteMatch(string entityType, string mode, string id)
            {
                EntityType = entityType;
                Mode = mode;
                Id = id;
            }

            public string EntityType { get; }
            public string Mode { get; }
            public string Id { get; }
        }
    }

    public static class EntityUiModes
    {
        public const string List = "list";
        public const string Create = "create";
        public const string Edit = "edit";
    }

    public class EntityUiBootstrap
    {
        public string EntityType { get; set; }
        public string Mode { get; set; }
        public string Id { get; set; }

        public string GetListUrl { get; set; }
        public string FactoryUrl { get; set; }
        public string GetUrl { get; set; }

        public string ListUIUrl { get; set; }
        public string CreateUIUrl { get; set; }
        public string EditUIUrl { get; set; }

        public static EntityUiBootstrap Create(string entityType, EntityDescription description)
        {
            if (description == null)
                return null;

            return new EntityUiBootstrap
            {
                EntityType = entityType,
                GetListUrl = description.GetListUrl,
                FactoryUrl = description.FactoryUrl,
                GetUrl = description.GetUrl,
                ListUIUrl = description.ListUIUrl,
                CreateUIUrl = description.CreateUIUrl,
                EditUIUrl = description.EditUIUrl
            };
        }
    }
}
