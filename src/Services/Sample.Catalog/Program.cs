using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kros.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sample.Catalog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var variables = new Dictionary<string, string>() { { "IsDocker", "false" } };
                    string isDocker = Environment.GetEnvironmentVariable("IS_DOCKER_RUN");
                    if (!isDocker.IsNullOrWhiteSpace() && bool.TryParse(isDocker, out bool isDockerRun) && isDockerRun)
                    {
                        config.AddJsonFile(
                            "appsettings.Docker.json",
                            optional: true,
                            reloadOnChange: true);
                        variables["IsDocker"] = "true";
                    }
                    config.AddInMemoryCollection(variables);
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseUrls("http://0.0.0.0:5200");
                });
    }
}
