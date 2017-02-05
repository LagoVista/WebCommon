using LagoVista.IoT.Web.Common.Resources;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Text;

namespace LagoVista.IoT.Web.Common.TagHelpers
{
    [HtmlTargetElement("form-submit")]
    public class SubmitTagHelper : TagHelper
    {
        [HtmlAttributeName("label")]
        public String Label { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "";

            if (String.IsNullOrEmpty(Label))
                Label = CommonResources.Common_Save;

            var tagBuilder = new StringBuilder();
            tagBuilder.AppendLine($@"<div class=""form-group"">");
            tagBuilder.AppendLine($@"  <div class=""col-md-offset-2 col-md-10"" >");
            tagBuilder.AppendLine($@"    <input type=""submit"" value=""{Label}"" class=""btn btn-success"" />");
            tagBuilder.AppendLine($@" </div>");
            tagBuilder.AppendLine($@"</div>");

            output.Content.SetHtmlContent(tagBuilder.ToString());
        }
    }
}
