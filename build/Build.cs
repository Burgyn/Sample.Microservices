using System;
using System.IO;
using System.Text;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
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

    [Parameter("Which docker registry should be used for publish")]
    readonly string DockerRegistry = "mmlib.azurecr.io";

    string Tag => Configuration == Configuration.Release ? "latest" : "dev";

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath BuildDirectory => RootDirectory / "build";

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
        var target = $"{DockerRegistry}/{name.ToLower()}";
        DockerTasks.DockerImageTag(x => x
            .SetSourceImage(name)
            .SetTargetImage(target));

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
        => $"microservices/{image}:{Tag}";

    private static string GetProjectName(AbsolutePath projectPath)
        => Path.GetFileName(projectPath)
            .Replace("Sample.", string.Empty)
            .Replace("ApiGateway", "Gateway")
            .ToLower();
}
