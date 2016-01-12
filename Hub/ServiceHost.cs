﻿namespace Hub
{
    using System;
    using System.IO;

    using Autofac;

    using Extensions;

    using log4net.Config;

    using Microsoft.Owin.Hosting;

    using NServiceBus;
    using NServiceBus.Log4Net;
    using NServiceBus.Logging;

    using Topshelf;

    public class ServiceHost : IServiceHost
    {
        private static IStartableBus bus;
        private static IDisposable webHost;

        private static HostControl topShelfHostControl;
        public static IContainer Container { get; private set; }

        public bool Start(HostControl hostControl)
        {
            topShelfHostControl = hostControl;

            SetUpLog4Net();
            Container = CreateContainer();
            StartOwinWebHost();
            StartBus();
            
            return true;
        }

        public bool Stop()
        {
            if (bus != null)
            {
                bus.Dispose();
            }

            if (webHost != null)
            {
                webHost.Dispose();
            }

            return true;
        }

        private void OnCriticalError(string arg1, Exception exception)
        {
            topShelfHostControl.Stop();
        }
        
        private static void StartOwinWebHost()
        {
            var httpLocalhost = "http://localhost:8093";
            webHost = WebApp.Start(httpLocalhost);
            Console.WriteLine("Successfully started the SignalR publisher on: {0}", httpLocalhost);
        }

        private void StartBus()
        {
            var busConfiguration = CreateBusConfiguration(Container);
            busConfiguration.DefineCriticalErrorAction(OnCriticalError);

            StartBus(busConfiguration);

            Console.WriteLine("Successfully started the Bus listening to the hub queue...");
        }

        private static BusConfiguration CreateBusConfiguration(IContainer container)
        {
            var busConfiguration = new BusConfiguration();
            busConfiguration.Configure(container);
            return busConfiguration;
        }

        private static void StartBus(BusConfiguration busConfiguration)
        {
            bus = Bus.Create(busConfiguration);
            bus.Start();
        }

        private static void SetUpLog4Net()
        {
            XmlConfigurator.ConfigureAndWatch(new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config"));
            LogManager.Use<Log4NetFactory>();
        }

        private static IContainer CreateContainer()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterComponents();
            var container = containerBuilder.Build();
            return container;
        }        
    }
}