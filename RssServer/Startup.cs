using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Coravel;
using Coravel.Scheduling.Schedule.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niolog;
using Niolog.Interfaces;
using RssServer.Helpers;
using RssServer.User.Interfaces;
using RssServer.User.Orleans;

namespace RssServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions()
                .Configure<AppSettings>(this.Configuration);
            services.AddSingleton<Helper>();
            services.AddTransient<RssFetcher>();
            // 若不希望 RssServer 使用用户功能，将 RssServer.User.Orleans 的项目引用去掉，以及注释以下这行代码
            // 若是想要更换用户功能实现，只需将 RssServer.User.Orleans 的项目引用去掉即可
            services.AddSingleton<IUserService, UserService>();
            services.AddControllers();
            services.AddScheduler();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory,
            IOptions<AppSettings> appSettings, ILogger<IScheduler> schedulerLogger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            NiologManager.DefaultWriters = new ILogWriter[]
            {
                new FileLogWriter(appSettings?.Value?.Log?.Path, 10),
                new ConsoleLogWriter()
            };
            loggerFactory.AddProvider(new LoggerProvider());

            var provider = app.ApplicationServices;
            provider.UseScheduler(scheduler =>
            {
                scheduler.Schedule<RssFetcher>()
                    .EveryFiveMinutes()
                    .PreventOverlapping("RssFetcher");
            })
            .LogScheduledTaskProgress(schedulerLogger)
            .OnError(e =>
            {
                var logger = NiologManager.CreateLogger();
                logger.Warn()
                    .Message("Something goes wrong...")
                    .Exception(e, true)
                    .Write();
            });

            app.UseRouting();
            app.UseStaticFiles();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
