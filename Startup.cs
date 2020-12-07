using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NSZUNews.Controllers;
using NSZUNews.Services;
using Quartz;

namespace NSZUNews
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
            services.AddScoped<NewsService>();
            services.AddScoped<ArticleRepository>();
            services.AddSingleton<NewsParser>();
            services.AddTransient<NewsParserJob>();
            services.AddHostedService(x => x.GetService<NewsParser>());
            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "nszu_news", Version = "v1" });
            });

            services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));
            services.AddQuartz(q =>
            {
                q.SchedulerId = "News Main";
                var jobKey = new JobKey("News Parser", null);
                q.AddJob<NewsParserJob>(j => j.WithIdentity(jobKey));
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.AddTrigger(t =>
                    t.ForJob(jobKey) 
                        .WithSimpleSchedule(x =>
                            x.WithIntervalInHours(
                                    Configuration.GetSection("NewsParser")
                                    .GetValue("Reparse", 12))
                            .RepeatForever())
                    );

            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });
             

            services.AddDbContext<ArticlesContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("ArticlesContext")));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "nszu_news v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseCors();

            app.UseHsts();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
