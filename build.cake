#tool "nuget:?package=NUnit.ConsoleRunner&version=3.9.0"
#addin "nuget:?package=Cake.DoInDirectory&version=3.2.0"
#addin "Cake.FileHelpers&version=3.1.0"
#addin "Cake.XComponent&version=6.0.1"
#addin "Cake.Incubator&version=3.0.0"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var buildConfiguration = Argument("buildConfiguration", "Debug");
var version = Argument("buildVersion", "1.0.0");
var apiKey = Argument("nugetKey", "");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./src/XComponent.Functions/bin") + Directory(buildConfiguration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
    CleanDirectory("nuget");
    CleanDirectory("packaging");
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreRestore("src/XComponent.Functions.sln");
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    DotNetCoreBuild(
    "src/XComponent.Functions.sln",
    new DotNetCoreBuildSettings {
        Configuration = buildConfiguration,
        VersionSuffix = version,
        MSBuildSettings = new DotNetCoreMSBuildSettings{}.SetVersion(version),
    });
});

Task("Test")
  .IsDependentOn("Build")
  .Does(() =>
  {

    var settings = new DotNetCoreTestSettings
    {
        Configuration = buildConfiguration
    };

    var projectFiles = GetFiles("./**/*Test*.csproj");
    foreach(var file in projectFiles)
    {
        DotNetCoreTest(file.FullPath, settings);
    }
  });

Task("CreatePackage")
    .IsDependentOn("Test")
    .Does(() =>
    {
        DotNetCorePack(
            "src/XComponent.Functions/XComponent.Functions.csproj",
            new DotNetCorePackSettings  {
                Configuration = buildConfiguration,
                IncludeSymbols = true,
                NoBuild = true,
                OutputDirectory = @"nuget",
                VersionSuffix = version,
                MSBuildSettings = new DotNetCoreMSBuildSettings{}.SetVersion(version),
            }
        );
    });

//////////////////////////////////////////////////////////////////////
// TASK PUSH
//////////////////////////////////////////////////////////////////////

Task("PushPackage")
    .Does(() =>
    {
        if (!string.IsNullOrEmpty(apiKey))
        {
            DoInDirectory("./nuget", () =>
            {
                var package = "./XComponent.Functions." + version + ".nupkg";
                DotNetCoreNuGetPush(package, new DotNetCoreNuGetPushSettings
                {
                    Source = "https://api.nuget.org/v3/index.json",
                    ApiKey = apiKey
                });
            });
        }
        else
        {
            Error("No Api Key provided. Can't deploy package to Nuget.");
        }
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
		.IsDependentOn("CreatePackage");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
