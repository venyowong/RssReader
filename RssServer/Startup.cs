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

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
