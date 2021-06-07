using Microsoft.Extensions.DependencyInjection;
using Slacek.Client.Core;
using System;
using System.Collections.Generic;

namespace Slacek.Client.Desktop
{
    public static partial class ServiceProvider
    {
        private static readonly IServiceProvider serviceProvider;

        static ServiceProvider()
        {
            serviceProvider = Setup();
        }

        private static IServiceProvider Setup()
        {
            return new ServiceCollection()
                .AddSingleton<MainWindowViewModel>()
                .AddTransient<CreateOrJoinGroupWindowViewModel>()
                .AddTransient<LoginPageViewModel>()
                .AddTransient<ChatPageViewModel>()
                .AddSingleton(sp => new ConnectionService("127.0.0.1", 7641))
                .AddSingleton<DataManager>()
                .BuildServiceProvider();
        }

        public static IServiceScope CreateScope()
        {
            return serviceProvider.CreateScope();
        }

        public static object GetRequiredService(Type serviceType)
        {
            return serviceProvider.GetRequiredService(serviceType);
        }

        public static T GetRequiredService<T>() where T : notnull
        {
            return serviceProvider.GetRequiredService<T>();
        }

#nullable enable

        public static object? GetService(Type serviceType)
        {
            return serviceProvider.GetService(serviceType);
        }

        public static T? GetService<T>()

        {
            return serviceProvider.GetService<T>();
        }

        public static IEnumerable<object?> GetServices(Type serviceType)

        {
            return serviceProvider.GetServices(serviceType);
        }

#nullable disable

        public static IEnumerable<T> GetServices<T>()
        {
            return serviceProvider.GetServices<T>();
        }
    }
}
