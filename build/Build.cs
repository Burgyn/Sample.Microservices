using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Data.SqlClient;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using Polly;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.DockerBuild);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [Parameter("Which docker image should be build")]
    readonly string CurrentProject = null;

    string DockerRegistry => (Configuration == Configuration.Release ? "mmlib.azurecr.io/" : string.Empty);

    [Parameter("Services tag")]
    readonly string ServicesTag = null;

    string Tag => ServicesTag ?? (Configuration == Configuration.Release ? "latest" : "dev");

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath BuildDirectory => RootDirectory / "build";
    AbsolutePath TempEnvFile => RootDirectory / "temp.env";
    AbsolutePath PostmanTests => RootDirectory / "tests/postman";
    [PathExecutable] readonly Tool Docker;
    [PathExecutable] readonly Tool Newman;

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
        .Executes(() =>
        {
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
        var target = $"{DockerRegistry}{name.ToLower()}";
        DockerTasks.DockerImagePush(x => x.SetName(target));
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
                .AppendLine($"REGISTRY={DockerRegistry}");

            File.WriteAllText(TempEnvFile, sb.ToString());
        })
        .Before(ComposeUp);

    Target ComposeUp => _ => _
        .DependsOn(CreateTempEnv)
        .Executes(() =>
        {
            Docker($"compose {GetEnvFileOption()} up -d");
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
        var con = new SqlConnection("Server=localhost,1434;Database=Catalog;User Id=SA;Password=str0ngP@ass;Connect Timeout=10");

        var policy = Policy
            .Handle<SqlException>()
            .OrInner<SocketException>()
            .OrInner<SqlException>()
            .WaitAndRetry(20, retryAttempt =>
            {
                Logger.Warn($"==== Retry: {retryAttempt}");
                return TimeSpan.FromSeconds(8);
            });

        policy.Execute(con.Open);
    }

    private string GetEnvFileOption()
        => FileExists(TempEnvFile) ? $"--env-file {TempEnvFile}" : string.Empty;

    Target ComposeDown => _ => _
        .AssuredAfterFailure()
        .Executes(() =>
        {
            Docker($"compose down");
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

    private void PublishProject(AbsolutePath projectPath)
    {
        const string dockerfile = "Dockerfile";
        var path = Path.GetDirectoryName(projectPath);
        var serviceName = Path.GetFileNameWithoutExtension(projectPath);

        Logger.Normal($"=== [{serviceName}]: start publishing.");

        DotNetPublish(proj =>
            proj.SetProject(path)
                .EnableNoBuild()
                .SetOutput(OutputDirectory / serviceName)
                .SetConfiguration(Configuration));
        string dockerFile = File
            .ReadAllText(BuildDirectory / dockerfile)
            .Replace("{PROJECT}", serviceName);

        File.WriteAllText(OutputDirectory / serviceName / dockerfile, dockerFile, Encoding.UTF8);
    }

    private string GetImageName(string image)
        => $"{DockerRegistry}microservices/{image}:{Tag}";

    private static string GetProjectName(AbsolutePath projectPath)
        => Path.GetFileName(projectPath)
            .Replace("Sample.", string.Empty)
            .Replace("ApiGateway", "Gateway")
            .ToLower();
}
