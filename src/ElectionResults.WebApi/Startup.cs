using System;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using ElectionResults.Core.Services;
using ElectionResults.Core.Services.BlobContainer;
using ElectionResults.Core.Services.CsvDownload;
using ElectionResults.Core.Services.CsvProcessing;
using ElectionResults.Core.Storage;
using ElectionResults.WebApi.Scheduler;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace ElectionResults.WebApi
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppConfig>(options => Configuration.GetSection("settings").Bind(options));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddTransient<IResultsRepository, ResultsRepository>();
            services.AddTransient<IResultsAggregator, ResultsAggregator>();
            services.AddTransient<ICsvDownloaderJob, CsvDownloaderJob>();
            services.AddTransient<IBucketUploader, BucketUploader>();
            services.AddTransient<IElectionConfigurationSource, ElectionConfigurationSource>();
            services.AddTransient<IFileProcessor, FileProcessor>();
            services.AddTransient<IStatisticsAggregator, StatisticsAggregator>();
            services.AddTransient<IBucketRepository, BucketRepository>();
            services.AddTransient<IAdminRepository, AdminRepository>();
            services.AddTransient<IFileRepository, FileRepository>();
            services.AddTransient<IVoterTurnoutAggregator, VoterTurnoutAggregator>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rezultate Vot API", Version = "v1" });
            });
            if (_environment.IsDevelopment())
            {
                services.AddSingleton<IAmazonDynamoDB>(sp =>
                {
                    var clientConfig = new AmazonDynamoDBConfig
                    {
                        ServiceURL = Consts.DynamoDbServiceUrl,
                        UseHttp = true
                    };
                    return new AmazonDynamoDBClient(new BasicAWSCredentials("abc", "def"), clientConfig);
                });
            }
            else
            {
                services.AddAWSService<IAmazonDynamoDB>();
            }

            if (_environment.IsDevelopment())
            {
                var amazonS3 = new AmazonS3Client(new BasicAWSCredentials("abc", "def"), new AmazonS3Config
                {
                    ServiceURL = Consts.S3ServiceUrl,
                    ForcePathStyle = true,
                    UseHttp = true
                });

                services.AddTransient(typeof(IAmazonS3), provider => amazonS3);
            }
            else
            {
                services.AddAWSService<IAmazonS3>(new AWSOptions
                {
                    Profile = "default",
                    Region = RegionEndpoint.EUCentral1
                });
                services.AddDefaultAWSOptions(new AWSOptions
                {
                    Region = RegionEndpoint.EUCentral1
                });
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySQL(Configuration.GetConnectionString("DefaultConnection"));
            });
            
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o => o.LoginPath = "/web/login");
            services.AddLazyCache();
            services.AddSingleton<IHostedService, ScheduleTask>();
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/build";
            });
            services.AddLogging(builder =>
            {
                var config = Configuration.GetSection("AWS.Logging");
                builder
                    .AddConfiguration(config)
                    .AddConsole()
                    .AddDebug()
                    .AddAWSProvider(Configuration.GetAWSLoggingConfigSection().Config);
            });

            services.AddCors(options =>
            {
                options.AddPolicy("origins",
                    builder =>
                    {
                        builder.WithOrigins("*");
                    });
            });
        }

        private bool InDocker { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; } }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, ApplicationDbContext context)
        {
            Log.SetLogger(loggerFactory.CreateLogger<Startup>());
            MigrateDatabase(context);
            app.UseHsts();
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Rezultate Vot API V1");
            });
            app.UseCors("origins");
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var exceptionHandlerPathFeature =
                        context.Features.Get<IExceptionHandlerPathFeature>();

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(exceptionHandlerPathFeature.Error));

                });
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
                if (env.IsDevelopment()) // this should be used only in Development mode
                {
                    try
                    {
                        if (!InDocker) // and only when started from the dotnet CLI or Visual Studio
                        {
                            spa.Options.StartupTimeout = TimeSpan.FromSeconds(120); 
                            spa.UseReactDevelopmentServer(npmScript: "start");
                        }
                    }
                    catch
                    {
                        //this will crash when running in Docker due to a react/npm bug, so it's safe to ignore
                    }
                }
            });
        }

        private static void MigrateDatabase(ApplicationDbContext context)
        {
            try
            {
                context.Database.Migrate();
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        }
    }
}
