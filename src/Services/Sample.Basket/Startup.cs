using Kros.KORM;
using Kros.KORM.Extensions.Asp;
using Kros.Swagger.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Sample.Basket.Domain;
using Sample.Basket.Infrastructure;
using System;
using System.Net.Sockets;

namespace Sample.Basket
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerDocumentation(Configuration);
            KormBuilder kormBuilder = services.AddKorm(Configuration)
                .UseDatabaseConfiguration<DatabaseConfiguration>()
                .AddKormMigrations();

            Migrate(kormBuilder);

            services.AddScoped<IBasketRepository, BasketRepository>();
            services.AddScoped<DummyDataInitializer>();
            services.AddControllers();
        }

        private static void Migrate(KormBuilder kormBuilder)
        {
            var policy = Policy
                .Handle<SqlException>()
                .OrInner<SocketException>()
                .OrInner<SqlException>()
                .WaitAndRetry(40, retryAttempt =>
                {
                    Console.WriteLine($"=== Migrate retry attempt: {retryAttempt}");
                    return TimeSpan.FromSeconds(8);
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
