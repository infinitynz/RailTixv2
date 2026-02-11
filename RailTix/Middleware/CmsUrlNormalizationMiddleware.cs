using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RailTix.Services.Cms;

namespace RailTix.Middleware
{
    public class CmsUrlNormalizationMiddleware
    {
        private readonly RequestDelegate _next;

        public CmsUrlNormalizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICmsUrlService urlService)
        {
            var path = context.Request.Path.Value ?? "/";

            if (!ShouldNormalize(context.Request.Method, path))
            {
                await _next(context);
                return;
            }

            var normalized = urlService.NormalizePath(path);
            if (!string.Equals(path, normalized, StringComparison.Ordinal))
            {
                var newUrl = normalized + context.Request.QueryString;
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = newUrl;
                return;
            }

            await _next(context);
        }

        private static bool ShouldNormalize(string method, string path)
        {
            if (!HttpMethods.IsGet(method) && !HttpMethods.IsHead(method))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return false;
            }

            if (Path.HasExtension(path))
            {
                return false;
            }

            return true;
        }
    }
}

