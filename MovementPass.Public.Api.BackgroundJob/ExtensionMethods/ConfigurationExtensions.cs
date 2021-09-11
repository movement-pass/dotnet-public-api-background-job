namespace MovementPass.Public.Api.BackgroundJob.ExtensionMethods
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class ConfigurationExtensions
    {
        private static readonly IEnumerable<string> Suffixes =
            new[] {"Configuration", "Config", "Options", "Option"};

        public static void Apply<TConfig>(
            this IConfiguration instance,
            IServiceCollection services)
            where TConfig : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.Configure<TConfig>(GetSection<TConfig>(instance));
        }

        private static IConfigurationSection GetSection<TType>(
            IConfiguration instance)
        {
            var key = typeof(TType).Name;

            foreach (var suffix in Suffixes)
            {
                if (!key.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                key = key.Substring(0, key.Length - suffix.Length);
                break;
            }

            return instance.GetSection(key);
        }
    }
}
