using LagoVista.Core.Exceptions;
using LagoVista.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LagoVista.IoI.Web.Common.Attributes
{
    public class ValidateViewModelAttribute : ActionFilterAttribute, IExceptionFilter
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments.Values.Where(v => v is IValidateable))
            {
                var result = Validator.Validate(argument as IValidateable);
                if (!result.IsValid)
                {
                    var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState);
                    var idx = 1;
                    foreach (var err in result.Errors.Where(err => err.SystemError == false))
                    {
                        context.ModelState.AddModelError(idx.ToString(), err.Message);
                    }

                    viewData.Model = argument;
                    context.Result = new ViewResult()
                    {
                        ViewName = null,
                        ViewData = viewData,
                    };
                }

                context.RouteData.Values.Add("Model", argument);
            }

            base.OnActionExecuting(context);
        }

        public void OnException(ExceptionContext context)
        {

            if (context.Exception is ValidationException)
            {
                var validationException = context.Exception as ValidationException;
                var systemErrors = validationException.Errors.Where(err => err.SystemError == true);
                var userErrors = validationException.Errors.Where(err => err.SystemError == false);

                var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState);
                var idx = 1;
                foreach (var err in userErrors)
                {
                    context.ModelState.AddModelError((idx++).ToString(), err.Message);
                }

                viewData.Model = context.RouteData.Values["Model"];
                context.Result = new ViewResult()
                {
                    ViewName = null,
                    ViewData = viewData,
                };

                context.ExceptionHandled = true;
            }
        }

    }
}