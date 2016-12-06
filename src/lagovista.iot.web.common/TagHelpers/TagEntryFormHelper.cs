using LagoVista.Core.Attributes;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using lgv = LagoVista.Core.Attributes;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LagoVista.IoT.Web.Common.Resources;

namespace LagoVista.IoT.Web.Common.TagHelpers
{
    [HtmlTargetElement("form-entry")]
    public class FormEntryTagHelper : TagHelper
    {
        [HtmlAttributeName("for")]
        public ModelExpression For { get; set; }

        [HtmlAttributeName("items")]
        public IEnumerable<lgv.SelectListItem> Items { get; set; }

        public FormEntryTagHelper(IHtmlGenerator generator)
        {
            Generator = generator;
        }

        public IHtmlHelper _helper;

        protected IHtmlGenerator Generator { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        private void CreateCheckBox(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label, String placeholder, String value)
        {
            htmlTagBuilder.AppendLine($@"<div class=""col-md-offset-2 col-md-10"">");
            htmlTagBuilder.AppendLine($@"  <div class=""checkbox"">");
            htmlTagBuilder.AppendLine($@"    <label for=""{For.Name}"">");
            htmlTagBuilder.AppendLine($@"      <input id=""{For.Name}"" name=""{For.Name}"" type=""checkbox"" value=""true"" />");
            htmlTagBuilder.AppendLine($@"       {label}");
            htmlTagBuilder.AppendLine($@"    </label>");
            htmlTagBuilder.AppendLine($@"  </div>");
            htmlTagBuilder.AppendLine($@"</div>");
        }

        private void AddMinMaxLengthValidation(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label)
        {
            if (attr.MinLength.HasValue && attr.MaxLength.HasValue)
            {
                var valMsg = CommonResources.Validation_Common_StringLength_MinMax.Replace("[FIELDNAME]", label).Replace("[MINLENGTH]", attr.MinLength.Value.ToString()).Replace("[MAXLENGTH]", attr.MaxLength.Value.ToString());
                htmlTagBuilder.Append($@"data-val-length-min=""{attr.MinLength.Value}"" data-val-length-max=""{attr.MaxLength.Value}"" data-val-length=""{valMsg}"" data-val=""true"" ");
            }
            else if (attr.MinLength.HasValue)
            {
                var valMsg = CommonResources.Validation_Common_StringLength_Min.Replace("[FIELDNAME]", label).Replace("[MINLENGTH]", attr.MinLength.Value.ToString());
                htmlTagBuilder.Append($@"data-val-length-min=""{attr.MinLength.Value}""  data-val-length=""{valMsg}"" data-val=""true"" ");
            }
            else if (attr.MaxLength.HasValue)
            {
                var valMsg = CommonResources.Validation_Common_StringLength_Max.Replace("[FIELDNAME]", label).Replace("[MAXLENGTH]", attr.MaxLength.Value.ToString());
                htmlTagBuilder.Append($@"data-val-length-max=""{attr.MaxLength.Value}"" data-val-length=""{valMsg}"" data-val=""true"" ");
            }
        }

        private void AddCompareToValidation(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label)
        {
            if (!String.IsNullOrEmpty(attr.CompareTo))
            {
                htmlTagBuilder.Append($@"data-val-equalto-other=""*.{attr.CompareTo}"" data-val=""true"" ");
                if (!String.IsNullOrEmpty(attr.CompareToMsgResource))
                {

                    var compareToMsgProperty = attr.ResourceType.GetProperty(attr.CompareToMsgResource, BindingFlags.Static | BindingFlags.Public);
                    var compareToMsg = (string)compareToMsgProperty.GetValue(compareToMsgProperty.DeclaringType, null);

                    htmlTagBuilder.Append($@"data-val-equalto=""{compareToMsg}"" ");
                }
            }
        }

        private void AddNamespaceValiation(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label)
        {
            if (attr.FieldType == FieldTypes.NameSpace)
            {

                htmlTagBuilder.Append(@"data-val-regex-pattern=""^[a-z0-9]{6,30}$"" ");
                htmlTagBuilder.Append($@"data-val-regex=""{Resources.CommonResources.Validation_RegEx_Namespace}"" ");
                /*  var namespaceMessageProperty = attr.ResourceType.GetProperty(attr.NamespaceUniqueMessageResource, BindingFlags.Static | BindingFlags.Public);
                  if (namespaceMessageProperty != null)
                  {
                      var namespaceMessage = (string)namespaceMessageProperty.GetValue(namespaceMessageProperty.DeclaringType, null);
                      htmlTagBuilder.Append($@"data-val-namespaceunique=""{namespaceMessage}"" ");
                      htmlTagBuilder.Append($@"data-val-namespaceunique-type=""{attr.NamespaceType.ToString().ToLower()}"" ");
                  }*/
            }
        }

        private void AddRequiredValidation(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label)
        {
            if (attr.IsRequired)
            {
                if (!String.IsNullOrEmpty(attr.RequiredMessageResource))
                {
                    var validationProperty = attr.ResourceType.GetProperty(attr.RequiredMessageResource, BindingFlags.Static | BindingFlags.Public);
                    var validationMessage = (string)validationProperty.GetValue(validationProperty.DeclaringType, null);
                    htmlTagBuilder.Append($@"data-val-required=""{validationMessage}"" data-val=""true"" ");
                }
                else
                {
                    htmlTagBuilder.Append($@"data-val-required=""{Resources.CommonResources.Validation_Common_IsRequired.Replace("[FIELDNAME]", label)}"" data-val=""true"" ");
                }
            }
        }

        private void AddTextFieldTypeAttributes(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label)
        {
            switch (attr.FieldType)
            {
                case FieldTypes.Password:
                    htmlTagBuilder.Append(@"type=""password"" ");
                    break;
                case FieldTypes.Email:
                    htmlTagBuilder.Append(@"type=""text"" ");
                    var valMsg = Resources.CommonResources.Validation_Common_InvalidEmailAddress.Replace("[FIELDNAME]", label);
                    htmlTagBuilder.Append($@"data-val-email=""{valMsg}"" data-val=""true"" ");
                    break;
                default:
                    htmlTagBuilder.Append(@"type=""text"" ");
                    break;
            }
        }

        private String GetHelp(FormFieldAttribute attr)
        {
            if (!String.IsNullOrEmpty(attr.HelpResource))
            {
                var helpProperty = attr.ResourceType.GetProperty(attr.HelpResource, BindingFlags.Static | BindingFlags.Public);
                return (string)helpProperty.GetValue(helpProperty.DeclaringType, null);
            }
            else
            {
                return null;
            }
        }

        private void CreateTextEntry(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label, String placeholder, String value)
        {
            htmlTagBuilder.AppendLine($@"<label class=""col-md-2 control-label"" for=""{For.Name}"">{label}</label>");
            htmlTagBuilder.AppendLine($@"  <div class=""col-md-10"">");
            var helpProperty = GetHelp(attr);

            htmlTagBuilder.AppendLine($@"    <input class=""form-control"" name=""{For.Name}"" id=""{For.Name}"" placeholder=""{placeholder}"" value=""{value}"" ");

            if (attr.FieldType == FieldTypes.Hidden)
            {
                htmlTagBuilder.Append(@"type=""hidden"" ");
            }

            AddTextFieldTypeAttributes(htmlTagBuilder, attr, label);
            AddRequiredValidation(htmlTagBuilder, attr, label);
            AddMinMaxLengthValidation(htmlTagBuilder, attr, label);
            AddCompareToValidation(htmlTagBuilder, attr, label);
            AddNamespaceValiation(htmlTagBuilder, attr, label);

            htmlTagBuilder.AppendLine($@" />");

            htmlTagBuilder.AppendLine($@"    <span class=""text-danger field-validation-valid"" data-valmsg-replace=""true"" data-valmsg-for=""{For.Name}"" ></span>");

            if (!String.IsNullOrEmpty(helpProperty))
            {
                htmlTagBuilder.AppendLine($@"    <p class=""help-block"">{helpProperty}</p>");
            }

            htmlTagBuilder.AppendLine($@"  </div>");
        }

        private void CreateMultiLineTextEntry(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label, String placeholder, String value)
        {
            htmlTagBuilder.AppendLine($@"<label class=""col-md-2 control-label"" for=""{For.Name}"">{label}</label>");
            htmlTagBuilder.AppendLine($@"  <div class=""col-md-10"">");
            var helpProperty = GetHelp(attr);

            htmlTagBuilder.AppendLine($@"    <textarea class=""form-control"" name=""{For.Name}"" id=""{For.Name}"" placeholder=""{placeholder}"" value=""{value}"" ");
            AddRequiredValidation(htmlTagBuilder, attr, label);
            htmlTagBuilder.AppendLine(@"></textarea>");

            htmlTagBuilder.AppendLine($@"    <span class=""text-danger field-validation-valid"" data-valmsg-replace=""true"" data-valmsg-for=""{For.Name}"" ></span>");

            if (!String.IsNullOrEmpty(helpProperty))
            {
                htmlTagBuilder.AppendLine($@"    <p class=""help-block"">{helpProperty}</p>");
            }

            htmlTagBuilder.AppendLine($@"  </div>");
        }

        private void CreateOptionsList(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label, String placeholder, String value)
        {
            if (Items == null)
            {
                throw new Exception("Must provide a non-null value for Items if list type is OptionsList");
            }

            htmlTagBuilder.AppendLine($@"<label class=""col-md-2 control-label"" for=""{For.Name}"">{label}</label>");
            htmlTagBuilder.AppendLine($@"<div class=""col-md-10"">");
            htmlTagBuilder.AppendLine($@"  <select class=""form-control"" name=""{For.Name}"" id=""{For.Name}"" >");

            foreach (var option in Items)
            {
                htmlTagBuilder.AppendLine($@"  <option value=""{option.Key}"" >{option.Value}</option>");
            }
            htmlTagBuilder.AppendLine($@"  </select>");

            htmlTagBuilder.AppendLine($@"</div>");
        }

        private void CreatePicker(StringBuilder htmlTagBuilder, FormFieldAttribute attr, String label, String placeholder, String value)
        {

        }


        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "";

            var properties = For.Metadata.ContainerType.GetProperties();
            var thisProperty = properties.Where(prop => prop.Name == For.Name).FirstOrDefault();

            var formElementAttribute = thisProperty.CustomAttributes.FirstOrDefault();
            var attr = thisProperty.GetCustomAttributes(typeof(FormFieldAttribute), true).OfType<FormFieldAttribute>().FirstOrDefault();

            var value = String.Empty;
            if (For.Model != null)
                value = For.Model.ToString();

            if (attr.FieldType == FieldTypes.Hidden)
            {
                output.Content.SetHtmlContent($@"    <input class=""form-control"" name=""{For.Name}"" id=""{For.Name}"" value=""{value}"" type=""hidden"" />");
                return;
            }

            String label = String.Empty;
            String placeholder = String.Empty;
            if (!String.IsNullOrEmpty(attr.LabelDisplayResource))
            {
                var labelProperty = attr.ResourceType.GetProperty(attr.LabelDisplayResource, BindingFlags.Static | BindingFlags.Public);
                label = (string)labelProperty.GetValue(labelProperty.DeclaringType, null);
            }

            if (!String.IsNullOrEmpty(attr.WaterMark))
            {
                var placeholderProperty = attr.ResourceType.GetProperty(attr.WaterMark, BindingFlags.Static | BindingFlags.Public);
                placeholder = placeholderProperty == null ? String.Empty : (string)placeholderProperty.GetValue(placeholderProperty.DeclaringType, null);
            }



            var htmlTagBuilder = new StringBuilder();
            //     as FormFieldAttribute;
            htmlTagBuilder.AppendLine(@"<div class=""form-group"">");
            switch (attr.FieldType)
            {
                case FieldTypes.CheckBox: CreateCheckBox(htmlTagBuilder, attr, label, placeholder, value); break;
                case FieldTypes.OptionsList: CreateCheckBox(htmlTagBuilder, attr, label, placeholder, value); break;
                case FieldTypes.MultiLineText: CreateMultiLineTextEntry(htmlTagBuilder, attr, label, placeholder, value); break;
                case FieldTypes.Picker: CreatePicker(htmlTagBuilder, attr, label, placeholder, value); break;
                default: CreateTextEntry(htmlTagBuilder, attr, label, placeholder, value); break;
            }

            htmlTagBuilder.AppendLine("</div>");

            output.Content.SetHtmlContent(htmlTagBuilder.ToString());
        }
    }
}
