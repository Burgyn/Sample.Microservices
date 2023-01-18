using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Data.SqlClient;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Polly;
using Polly.Retry;
using Spectre.Console;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.DockerBuild);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [Parameter("Which docker image should be build")] readonly string CurrentProject = null;


    [Parameter("Services tag")] readonly string ServicesTag = null;

    [Parameter("Use docker registry")] readonly bool UseRegistry = true;

    string Tag => ServicesTag ?? (Configuration == Configuration.Release ? "latest" : "dev");

    string Registry => Configuration == Configuration.Release && UseRegistry ? "minoregistry.azurecr.io/" : string.Empty;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath BuildDirectory => RootDirectory / "build";
    AbsolutePath TempEnvFile => RootDirectory / "temp.env";
    AbsolutePath PostmanTests => RootDirectory / "tests/Postman";
    [PathExecutable("docker")] readonly Tool Docker;
    [PathExecutable("docker-compose")] readonly Tool DockerCompose;
    [PathExecutable("newman")] readonly Tool Newman;
    [PathExecutable("az")] readonly Tool Az;
    [PathExecutable("kubectl")] readonly Tool K8;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target DockerBuild => _ => _
        .After(Publish)
        .Executes(() =>
        {
            Logger.Normal("=== Start docker build");
            OutputDirectory.GlobFiles("**/Dockerfile").ForEach(f =>
            {
                if (CurrentProject is null || f.ToString().Contains(CurrentProject, StringComparison.OrdinalIgnoreCase))
                {
                    var serviceName = Path.GetFileName(f.Parent);
                    Logger.Normal($"=== [{serviceName}]: start building docker.");
                    DockerTasks.DockerBuild(x => x
                        .SetPath(".")
                        .SetFile(f)
                        .SetTag(GetImageName(GetProjectName(f.Parent))));
                }
            });
        });

    Target DockerPush => _ => _
        .Executes(() =>
        {
            OutputDirectory.GlobFiles("**/Dockerfile").ForEach(f =>
            {
                if (CurrentProject is null || f.ToString().Contains(CurrentProject, StringComparison.OrdinalIgnoreCase))
                {
                    var serviceName = Path.GetFileName(f.Parent);
                    Logger.Normal($"=== [{serviceName}]: start publish docker image to registry.");
                    PushImage(GetImageName(GetProjectName(f.Parent)));
                }
            });
        });

    private void PushImage(string name)
    {
        DockerTasks.DockerImagePush(x => x.SetName(name.ToLower()));
    }

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetConfiguration(Configuration)
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            SourceDirectory
                .GlobFiles("{Services,Gateways}/**/*.csproj")
                .ForEach(PublishProject);
        });

    Target CreateTempEnv => _ => _
        .Executes(() =>
        {
            Logger.Normal("==== Creating temp.env file.");
            var sb = new StringBuilder();
            sb.AppendLine($"SERVICES_TAG={Tag}")
                .AppendLine($"REGISTRY={Registry}");

            File.WriteAllText(TempEnvFile, sb.ToString());
            Logger.Normal("=== temp.env file content:");
            Logger.Normal(sb.ToString());
        })
        .Before(ComposeUp);

    Target ComposeUp => _ => _
        .DependsOn(CreateTempEnv)
        .Executes(() =>
        {
            DockerCompose($"{GetEnvFileOption()} up -d");
        })
        .After(DockerBuild)
        .Before(ComposeDown);

    Target IntegrationTests => _ => _
        .Before(ComposeDown)
        .Executes(() =>
        {
            string env = PostmanTests / "devel.postman_environment.json";
            WaitForSqlConnection();

            Logger.Normal("=== Start running integration tests.");
            PostmanTests.GlobFiles("*_collection.json").ForEach((f) =>
            {
                Newman($"run {f} -e {env}");
            });
        })
        .After(ComposeUp);

    private static void WaitForSqlConnection()
    {
        Logger.Normal($"=== Wait for sql connecton.");
        using var con =
            new SqlConnection("Server=localhost,1434;Database=Catalog;User Id=SA;Password=str0ngP@ass;Connect Timeout=10");

        var policy = CreateSqlRetryPolicy();
        policy.Execute(con.Open);

        using var command = new SqlCommand("SELECT TOP 1 1 FROM __KormMigrationsHistory", con);
        policy.Execute(command.ExecuteScalar);
    }

    static RetryPolicy CreateSqlRetryPolicy() =>
        Policy
            .Handle<SqlException>()
            .OrInner<SocketException>()
            .OrInner<SqlException>()
            .WaitAndRetry(20, retryAttempt =>
            {
                Logger.Warn($"==== SQL retry: {retryAttempt}");
                return TimeSpan.FromSeconds(5);
            });

    private string GetEnvFileOption()
        => FileExists(TempEnvFile) ? $"--env-file {TempEnvFile}" : string.Empty;

    Target ComposeDown => _ => _
        .AssuredAfterFailure()
        .Executes(() =>
        {
            DockerCompose($"down");
        });

    Target DockerRm => _ => _
        .DependsOn(ComposeDown)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            OutputDirectory.GlobFiles("**/Dockerfile").ForEach(f =>
            {
                if (CurrentProject is null || f.ToString().Contains(CurrentProject, StringComparison.OrdinalIgnoreCase))
                {
                    var serviceName = Path.GetFileName(f.Parent);
                    Logger.Normal($"=== [{serviceName}]: remove docker image.");
                    Docker($"image rm {GetImageName(GetProjectName(f.Parent))} -f");
                }
            });
        })
        .After(ComposeDown);

    Target CreateInfra => _ => _
        .Executes(() =>
        {
            string prefix = AnsiConsole.Ask<string>("Enter your unique [green]prefix[/] for AZURE resources:");
            string rsg = $"{prefix}aks-rsg";
            var exist = Az($"group exists --name {rsg}");

            if (!bool.TryParse(exist.FirstOrDefault().Text, out var e) || !e)
            {
                AnsiConsole.MarkupLine($"Creating resource group [green]{rsg}[/]");
                Az($"group create --name {rsg} --location westeurope");
            }

            string clusterName = $"{prefix}aks";
            string registryName = $"{prefix}registry";
            Az(
                @$"deployment group create --resource-group {rsg} --template-file .\Infrastructure\Infrastructure.bicep --parameters resourcePrefix={prefix}");
            Az($"acr login --name {registryName}");
            Az($"aks get-credentials --resource-group {rsg} --name {clusterName} --overwrite-existing");
            Az($"aks update --name {clusterName} --resource-group {rsg} --attach-acr {registryName}");
            
            //vytvorenie searchu
            // nakopirovanie dat do searchu 
            //  kubectl exec search-fdb679fb-dmzsq -- sh -c ' mkdir -p /srv/data/catalog  >/dev/null 2>&1'
            //  kubectl cp .\SearchDefinitions\CatalogAzureSearch\index.json  search-fdb679fb-dmzsq:/srv/data/catalog/index.json
            //  restartovat pod 😉 aby tam nasiel dane data 
        });

    Target DestroyInfra => _ => _
        .Executes(() =>
        {
            string prefix = AnsiConsole.Ask<string>("Enter your unique [green]prefix[/] for AZURE resources:");
            string rsg = $"{prefix}aks-rsg";
            var exist = Az($"group delete --name {rsg} --yes");
        });

    Target Deploy => _ => _
        .Executes(() =>
        {
            AnsiConsole.Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Star)
                .SpinnerStyle(Style.Parse("green bold"))
                .Start("Deploying...", ctx => 
                {
                    AnsiConsole.MarkupLine("Start deploying...");
                    foreach (string deployment in RootDirectory.GlobFiles("**/deployment-*.yml"))
                    {
                        K8($"apply -f {deployment}");
                    }
                    
                    AnsiConsole.MarkupLine("Creating databases...");
                    CreateDatabases();
                    
                    AnsiConsole.MarkupLine("Creating storage account tables...");
                    CreateStorageAccountTables();
                });
        });

    void CreateDatabases()
    {
        var databaseIp = GetLoadBalancerIp("database");
        AnsiConsole.MarkupLine($"Database IP is [green]{databaseIp}[/]");

        var databases = RootDirectory.GlobFiles("**/databases.db").SelectMany(p => File.ReadAllText(p).Split(","));
        CreateDatabases(databaseIp, databases);
    }
    
    void CreateStorageAccountTables()
    {
        var storageIp = GetLoadBalancerIp("storage");
        AnsiConsole.MarkupLine($"Storage IP is [green]{storageIp}[/]");

        var tables = RootDirectory.GlobFiles("**/tables.sa").SelectMany(p => File.ReadAllText(p).Split(","));
        CreateStorageAccountTables(storageIp, tables);
    }

    string GetLoadBalancerIp(string serviceName)
    {
        var policy = Policy
            .HandleResult<string>(string.IsNullOrEmpty)
            .WaitAndRetry(20, retryAttempt =>
            {
                Logger.Warn($"==== Getting IP retry: {retryAttempt}");
                return TimeSpan.FromSeconds(2);
            });
        string jsonPath = "-o jsonpath=\"{.items[0].status.loadBalancer.ingress[0].ip}\"";
        string loadBalancerIp = policy.Execute(() =>
            K8($"get services -l service={serviceName} {jsonPath}")
                .FirstOrDefault().Text);
        return loadBalancerIp;
    }

    private void PublishProject(AbsolutePath projectPath)
    {
        const string dockerfile = "Dockerfile";
        const string dockerfileFunc = "Dockerfile-Func";
        var path = Path.GetDirectoryName(projectPath);
        var serviceName = Path.GetFileNameWithoutExtension(projectPath);

        Logger.Normal($"=== [{serviceName}]: start publishing.");

        DotNetPublish(proj =>
            proj.SetProject(path)
                .EnableNoBuild()
                .SetOutput(OutputDirectory / serviceName)
                .SetConfiguration(Configuration));
        string dockerFile = File
            .ReadAllText(BuildDirectory / (IsAzureFunction(serviceName) ? dockerfileFunc : dockerfile))
            .Replace("{PROJECT}", serviceName);

        File.WriteAllText(OutputDirectory / serviceName / dockerfile, dockerFile, Encoding.UTF8);
    }

    private static bool IsAzureFunction(string serviceName)
        => serviceName.Contains("search", StringComparison.OrdinalIgnoreCase);

    private string GetImageName(string image)
        => $"{Registry}microservices/{image}:{Tag}";

    private static string GetProjectName(AbsolutePath projectPath)
        => Path.GetFileName(projectPath)
            .Replace("Sample.", string.Empty)
            .Replace("ApiGateway", "Gateway")
            .ToLower();

    private static void CreateStorageAccountTables(string storageIp, IEnumerable<string> tables)
    {
        var connectionString =
            $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1; AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://{storageIp}:10001/devstoreaccount1;TableEndpoint=http://{storageIp}:10002/devstoreaccount1;";
        var serviceClient = new TableServiceClient(connectionString);

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetry(20, retryAttempt =>
            {
                Logger.Warn($"==== Storage creating table retry: {retryAttempt}");
                return TimeSpan.FromSeconds(5);
            });

        foreach (string tableName in tables)
        {
            policy.Execute(() => serviceClient.CreateTableIfNotExists(tableName));
        }
    }

    private static void CreateDatabases(string databaseIp, IEnumerable<string> databases)
    {
        using var sqlConnection = new SqlConnection($"Server={databaseIp};Database=master;User Id=sa;Password=str0ngP@ass;");
        var policy = CreateSqlRetryPolicy();
        policy.Execute(sqlConnection.Open);

        using var sqlCommand = sqlConnection.CreateCommand();
        foreach (string database in databases)
        {
            string sql = @$"IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = '{database}')
  BEGIN
    CREATE DATABASE [{database}]
  END";
            sqlCommand.CommandText = sql;
            sqlCommand.ExecuteNonQuery();
        }
    }
}