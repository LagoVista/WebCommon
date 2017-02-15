using LagoVista.Core.Attributes;
using LagoVista.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.IoT.Web.Portal.Common
{
    [HtmlTargetElement("form-summary")]
    public class FormSummaryTagHelper : TagHelper
    {
        [HtmlAttributeName("for")]
        public ModelExpression For { get; set; }

        public FormSummaryTagHelper(IHtmlGenerator generator)
        {
            Generator = generator;
        }

        protected IHtmlGenerator Generator { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }


        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var properties = For.Metadata.ContainerType.GetProperties();
            var propertyParts = For.Name.Split('.');

            PropertyInfo thisProperty = null;

            for (var idx = 0; idx < propertyParts.Count(); ++idx)
            {
                thisProperty = properties.Where(prop => prop.Name == propertyParts[idx]).FirstOrDefault();
                /*if (thisProperty.GetType() == typeof(EntityHeader))
                {
                  //  break;
                }
                else
                {
                    //    properties = properties.GetType().GetTypeInfo().GetProperties();
                }*/
            }

            var attr = thisProperty.GetCustomAttributes(typeof(FormFieldAttribute), true).OfType<FormFieldAttribute>().FirstOrDefault();

            var labelProperty = attr.ResourceType.GetProperty(attr.LabelDisplayResource, BindingFlags.Static | BindingFlags.Public);
            var label = (string)labelProperty.GetValue(labelProperty.DeclaringType, null);

            var value = For.Model;
            if (value == null)
                value = String.Empty;

            output.TagName = "";

            var tagBuilder = new StringBuilder();
            tagBuilder.AppendLine($@"<div class=""form-group"">");
            tagBuilder.AppendLine($@"  <label class=""col-md-2 control-label"" >{label}</label>");
            tagBuilder.AppendLine($@"  <div class=""col-md-10"" >");
            tagBuilder.AppendLine($@"    <p class=""form-control-static"">{value}</p>");
            tagBuilder.AppendLine($@" </div>");
            tagBuilder.AppendLine($@"</div>");

            output.Content.SetHtmlContent(tagBuilder.ToString());
        }
    }
}
