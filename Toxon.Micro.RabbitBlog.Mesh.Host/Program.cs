﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Toxon.Micro.RabbitBlog.Plugins.Reflection;

namespace Toxon.Micro.RabbitBlog.Mesh.Host
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<Options>(args);
            if (result is Parsed<Options> parsed)
            {
                await Run(parsed.Value);
            }
        }

        private static async Task Run(Options opts)
        {
            var wellKnownBases = new WellKnownBases(opts.BaseAddresses);

            var pluginLoaders = Bootstrapper.LoadPlugins(new [] { opts.AssemblyPath });
            var assembly = pluginLoaders.Single().Assembly;
            var plugins = PluginDiscoverer.Discover(assembly);

            var models = new List<RoutingModel>();
            foreach (var plugin in plugins)
            {
                var model = new RoutingModel(plugin.ServiceKey, wellKnownBases, new RoutingModelOptions());
                models.Add(model);

                var configuredModel = Bootstrapper.ConfigureModel(model);

                await Bootstrapper.RegisterPluginAsync(configuredModel, plugin);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            foreach (var model in models)
            {
                await model.StartAsync();
            }

            Console.WriteLine($"Running {string.Join(", ", plugins.Select(x => x.ServiceKey))}... press enter to exit!");
            Console.ReadLine();
        }
    }
}