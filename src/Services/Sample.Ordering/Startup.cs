using Kros.KORM.Extensions.Asp;
using Kros.Swagger.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Sample.Ordering.Domain;
using Sample.Ordering.Infrastructure;
using System;
using System.Net.Sockets;

namespace Sample.Users
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
            services.AddSwaggerDocumentation(Configuration);

            services.AddTransient<IOrderRepository, OrderRepository>();
            services.AddTransient<DummyDataInitializer>();
            services.AddControllers();

            KormBuilder kormBuilder = services.AddKorm(Configuration)
               .UseDatabaseConfiguration<DatabaseConfiguration>()
               .AddKormMigrations();

            Migrate(kormBuilder);
        }

        private static void Migrate(KormBuilder kormBuilder)
        {
            var policy = Policy
                .Handle<SqlException>()
                .OrInner<SocketException>()
                .OrInner<SqlException>()
                .WaitAndRetry(10, retryAttempt =>
                {
                    Console.WriteLine($"=== Migrate retry attempt: {retryAttempt}");
                    return TimeSpan.FromSeconds(2);
                });

            policy.Execute(kormBuilder.Migrate);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DummyDataInitializer dataInitializer)
        {
            if (Configuration.GetValue<bool>("IsDocker"))
            {
                dataInitializer.Init();
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwaggerDocumentation(Configuration);

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
