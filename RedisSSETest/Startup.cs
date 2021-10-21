using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Funq;
using ServiceStack;
using ServiceStack.Configuration;
using RedisSSETest.ServiceInterface;
using ServiceStack.Redis;

namespace RedisSSETest
{
    public class Startup : ModularStartup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public new void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost
            {
                AppSettings = new NetCoreAppSettings(Configuration)
            });
        }
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("RedisSSETest", typeof(MyServices).Assembly) { }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false)
            });

            this.Plugins.Add(new ServerEventsFeature()
            {
                // Just to speed things up for example
                IdleTimeout = TimeSpan.FromSeconds(15)
            });

            var redisHost = AppSettings.GetString("RedisHost");

            if (redisHost != null)
            {
                container.Register<IRedisClientsManager>(
                    new RedisManagerPool(redisHost));

                container.Register<IServerEvents>(c =>
                    new RedisServerEvents(c.Resolve<IRedisClientsManager>()));

                //((RedisServerEvents)container.Resolve<IServerEvents>()).Local.NotifyLeaveAsync = async (sub) =>
                //{
                //    logger.Info("NotifyLeaveAsync Should Trigger Here...");
                //};

                container.Resolve<IServerEvents>().Start();
            }
        }
    }
}
