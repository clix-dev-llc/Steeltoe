﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Hypermedia;
using System;
using System.Linq;

namespace Steeltoe.Management.CloudFoundry
{
    public static class CloudFoundryHostBuilderExtensions
    {
        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="webHostBuilder">Your Hostbuilder</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static IWebHostBuilder AddCloudFoundryActuators(this IWebHostBuilder webHostBuilder, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            return webHostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.CloudFoundry, buildCorsPolicy);
        }

        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="hostBuilder">Your Hostbuilder</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static IHostBuilder AddCloudFoundryActuators(this IHostBuilder hostBuilder, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            return hostBuilder.AddCloudFoundryActuators(MediaTypeVersion.V1, ActuatorContext.CloudFoundry, buildCorsPolicy);
        }

        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="webHostBuilder">Your Hostbuilder</param>
        /// <param name="mediaTypeVersion">Spring Boot media type version to use with responses</param>
        /// <param name="actuatorContext">Select how targeted to Apps Manager actuators should be</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static IWebHostBuilder AddCloudFoundryActuators(this IWebHostBuilder webHostBuilder, MediaTypeVersion mediaTypeVersion, ActuatorContext actuatorContext, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            return webHostBuilder
                .ConfigureLogging(ConfigureDynamicLogging)
                .ConfigureServices((context, collection) => ConfigureServices(collection, context.Configuration, mediaTypeVersion, actuatorContext, buildCorsPolicy));
        }

        /// <summary>
        /// Adds all Actuators supported by Apps Manager. Also configures DynamicLogging if not previously setup.
        /// </summary>
        /// <param name="hostBuilder">Your Hostbuilder</param>
        /// <param name="mediaTypeVersion">Spring Boot media type version to use with responses</param>
        /// <param name="actuatorContext">Select how targeted to Apps Manager actuators should be</param>
        /// <param name="buildCorsPolicy">Customize the CORS policy. </param>
        public static IHostBuilder AddCloudFoundryActuators(this IHostBuilder hostBuilder, MediaTypeVersion mediaTypeVersion, ActuatorContext actuatorContext, Action<CorsPolicyBuilder> buildCorsPolicy = null)
        {
            return hostBuilder
                .ConfigureLogging(ConfigureDynamicLogging)
                .ConfigureServices((context, collection) => ConfigureServices(collection, context.Configuration, mediaTypeVersion, actuatorContext, buildCorsPolicy));
        }

        private static readonly Action<ILoggingBuilder> ConfigureDynamicLogging = (logbuilder) =>
        {
            var dynamicDescriptor = logbuilder.Services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IDynamicLoggerProvider));
            if (dynamicDescriptor == null)
            {
                // remove the original ConsoleLoggerProvider to prevent duplicate logging
                var serviceDescriptor = logbuilder.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == typeof(ConsoleLoggerProvider));
                if (serviceDescriptor != null)
                {
                    logbuilder.Services.Remove(serviceDescriptor);
                }

                // make sure logger provider configurations are available
                if (!logbuilder.Services.Any(descriptor => descriptor.ServiceType == typeof(ILoggerProviderConfiguration<ConsoleLoggerProvider>)))
                {
                    logbuilder.AddConfiguration();
                }

                logbuilder.AddDynamicConsole();
            }
        };

        private static void ConfigureServices(IServiceCollection collection, IConfiguration configuration, MediaTypeVersion mediaTypeVersion, ActuatorContext actuatorContext, Action<CorsPolicyBuilder> buildCorsPolicy)
        {
            collection.AddCloudFoundryActuators(configuration, mediaTypeVersion, actuatorContext, buildCorsPolicy);
            collection.AddSingleton<IStartupFilter>(new CloudFoundryActuatorsStartupFilter(mediaTypeVersion, actuatorContext));
        }
    }
}
