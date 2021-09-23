using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.Browser
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddBrowserApi(this IApiFrameworkBuilder builder, string endpoint = null, BrowserOptions configuration = null)
        {
            builder.Services.RegisterPlugin(endpoint, configuration);

            return builder;
        }
        
        public static IServiceCollection AddBrowserApi(this IServiceCollection services, string endpoint = null, BrowserOptions configuration = null)
        {
            services.RegisterPlugin(endpoint, configuration);

            return services;
        }

    }
}
