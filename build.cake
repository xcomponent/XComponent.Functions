#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
#tool "nuget:?package=ILRepack"
//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("buildConfiguration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var buildDir = Directory("./src/XComponent.Functions/bin") + Directory(configuration);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./src/XComponent.Functions.sln");
});

Task("Test")
  .IsDependentOn("Build")
  .IsDependentOn("Merge")
  .Does(() =>
  {
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./src/XComponent.Functions.Test/XComponent.Functions.Test.csproj", settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      MSBuild("./src/XComponent.Functions.Test/XComponent.Functions.Test.csproj", settings =>
        settings.SetConfiguration(configuration));
    }
    NUnit3(GetFiles("./**/bin/" + configuration + "/*Test*.dll"));	
  });

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./src/XComponent.Functions/XComponent.Functions.csproj", settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      MSBuild("./src/XComponent.Functions/XComponent.Functions.sln", settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Merge")
  .IsDependentOn("Build")
  .Does(() =>
{

	var binariesDir = "./src/XComponent.Functions/bin/" + configuration + "/";
	var assemblyPaths = GetFiles(binariesDir + "*.dll");

    var ilRepackSettings = new ILRepackSettings { Parallel = false, ArgumentCustomization = args => args.Append(@"/target:library /allowDup /internalize") };

	ILRepack(
		"./generated/XComponent.Functions.Core.dll",
		binariesDir + "XComponent.Functions.dll",
		assemblyPaths,
    ilRepackSettings
		);
});

Task("CreatePackage")
    .IsDependentOn("Merge")
    .Does(() =>
    {
		var assemblyInfo = ParseAssemblyInfo("src/XComponent.Functions/Properties/AssemblyInfo.cs");
        var nugetPackSettings = new NuGetPackSettings()
        { 
            OutputDirectory = @"./generated",
			Version = assemblyInfo.AssemblyVersion,
        };

        NuGetPack("src/XComponent.Functions/XComponent.Functions.nuspec", nugetPackSettings);
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
