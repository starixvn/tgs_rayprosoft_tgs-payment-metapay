using System.Reflection;
using Tgs.Payment.MetaPay;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMetaPay(
        this IServiceCollection services,
        Action<MetaPayOptions> configureOptions,
        Action<IHttpClientBuilder> configureMetaPayClient)
    {
        configureMetaPayClient?.Invoke(services.AddHttpClient<MetaPayClient>());

        return services
            .AddOptions<MetaPayOptions>()
            .Configure(configureOptions)
            .Services
            .RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
            .AddSingleton<MetaPayCryptoer>()
            .AddSingleton<MetaPaySigner>();
    }
}