using LagoVista.CloudStorage.Interfaces;
using Microsoft.AspNetCore.Http;
using System;

namespace LagoVista.IoT.Web.Common.Utils
{

    public class CacheAborter : ICacheAborter
    {
        IHttpContextAccessor _httpContextAccessor;

        public CacheAborter(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public bool AbortCache 
        {   
            get
            {
                return _httpContextAccessor.HttpContext.Request.Query.ContainsKey("cache-abort");
            } 
        }
    }
}