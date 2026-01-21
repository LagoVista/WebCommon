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
                if(_httpContextAccessor.HttpContext == null)
                {
                    return false;
                }


                if (_httpContextAccessor.HttpContext.Request == null)
                {
                    return false;
                }

                if (_httpContextAccessor.HttpContext.Request.Query == null)
                {
                    return false;
                }

                return _httpContextAccessor.HttpContext.Request.Query.ContainsKey("cache-abort") ||
                       _httpContextAccessor.HttpContext.Request.Headers.ContainsKey("cache-abort");
            } 
        }
    }
}