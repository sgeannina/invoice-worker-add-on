﻿using Fintech.InvoiceWorker.Business.Models;
using Fintech.InvoiceWorker.Business.Repositories;
using Fintech.InvoiceWorker.Business.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Fintech.InvoiceWorker.AddOn
{
    class Program
    {

        static int Main(string[] args)
        {
            var cmd = new RootCommand(
                description: "Converts invoices data from a feed into PDF files.");

            var inputFeedOption = new Option(
              aliases: new string[] { "--feed-url", "-f" },
              description: "The url to the Feed with the invoices data.");
            cmd.AddOption(inputFeedOption);

            var inputStorageOption = new Option(
              aliases: new string[] { "--invoice-dir", "-i" },
              description: "The path to the directory to store the PDF files.");
            cmd.AddOption(inputStorageOption);

            cmd.AddOption(new Option(new[] { "--verbose", "-v" }, "Show the logs."));
            cmd.TreatUnmatchedTokensAsErrors = true;

            cmd.Handler = CommandHandler.Create<string, string, bool, IConsole>(HandleReadFeed);
 
            return cmd.Invoke(args);
        }

        static void HandleReadFeed(string feedUrl, string invoicesDirectory, bool verbose, IConsole console)
        {
            var serviceProvider = ConfigureServices(new InvoiceWorkerOptions
            {
                FeedUrl = feedUrl,
                InvoicesDirectory = invoicesDirectory
            }, verbose);

            while(true)
            {
                serviceProvider.GetService<IInvoiceWorkerService>().ProcessInvoices();
                Console.ReadKey(true);
            }

            //DisposeServices(serviceProvider);
        }

        private static ServiceProvider ConfigureServices(InvoiceWorkerOptions options, bool verbose)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env}.json", true, true)
                .AddEnvironmentVariables();

            var config = builder.Build();

            var services = new ServiceCollection()
                .AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options =>
                    options.MinLevel = GetLogLevel(verbose))
                .AddSingleton(o => options)
                .AddScoped<IInvoiceWorkerService, InvoiceWorkerService>()
                .AddScoped<IInvoiceEventsRepository, InvoiceEventsRepository>()
                .AddHttpClient();
            var serviceProvider = services.BuildServiceProvider(true);
            return serviceProvider;
        }

        private static void DisposeServices(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                return;
            }
            if (serviceProvider is IDisposable)
            {
                ((IDisposable)serviceProvider).Dispose();
            }
        }

        private static LogLevel GetLogLevel(bool verbose)
        {
            return verbose ? LogLevel.Debug : LogLevel.Error;
        }
    }
}
