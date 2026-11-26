using System.IO;
using System.Text;
using System.Threading.Tasks;
using LagoVista.Notifications.Execution;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace LagoVista.IoT.Web.Common.Notifications
{
    /// <summary>Typed registration input without logging malformed secret-bearing JSON.</summary>
    internal sealed class NotificationRegistrationInputFormatter : TextInputFormatter
    {
        public NotificationRegistrationInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
            SupportedEncodings.Add(Encoding.UTF8);
        }

        protected override bool CanReadType(System.Type type) => type == typeof(EndpointRegistrationRequest);

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            try
            {
                using var stream = new StreamReader(context.HttpContext.Request.Body, encoding, false, 4096, leaveOpen: true);
                var buffer = new char[4096];
                var body = new StringBuilder();
                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(), context.HttpContext.RequestAborted)) > 0)
                {
                    if (body.Length + read > 64 * 1024) return await Invalid(context);
                    body.Append(buffer, 0, read);
                }
                var model = JsonConvert.DeserializeObject<EndpointRegistrationRequest>(body.ToString(),
                    new JsonSerializerSettings { DateParseHandling = DateParseHandling.None, MaxDepth = 16,
                        CheckAdditionalContent = true });
                return model == null ? await Invalid(context) : await InputFormatterResult.SuccessAsync(model);
            }
            catch (JsonException) { return await Invalid(context); }
        }

        private static Task<InputFormatterResult> Invalid(InputFormatterContext context)
        {
            context.ModelState.TryAddModelError(context.ModelName, "Invalid notification registration material.");
            return InputFormatterResult.FailureAsync();
        }
    }
}
