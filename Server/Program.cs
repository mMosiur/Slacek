using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Slacek.Server
{
    internal static class Program
    {
        private const string DefaultAddress = "127.0.0.1";
        private const int DefaultPort = 7641;

        private static void Main(string[] args)
        {
            IServiceProvider serviceProvider = BuildServiceProvider(args);
            using ServerManager serverManager = serviceProvider.GetRequiredService<ServerManager>();
            serverManager.Run();
        }

        private static IServiceProvider BuildServiceProvider(string[] args)
        {
            string address = args.Length < 1 ? DefaultAddress : args[0];
            int port = args.Length < 2 ? DefaultPort : int.Parse(args[1]);
            return new ServiceCollection()
                .AddSingleton(sp =>
                {
                    return new ServerManager(
                        sp.GetRequiredService<ILogger>(),
                        new ConnectionManagerConfiguration(port, address),
                        sp.GetRequiredService<DatabaseManager>());
                })
                .AddTransient<ILogger, ConsoleLogger>(sp => new ConsoleLogger(LogLevel.Debug))
                .AddSingleton<DatabaseManager>()
                .AddDbContext<DatabaseContext>()
                .BuildServiceProvider();
        }
    }
}
