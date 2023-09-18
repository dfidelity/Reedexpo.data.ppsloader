#module nuget:?package=Cake.DotNetTool.Module&version=0.3.0         // dotnet tool nuget package loader - needs bootstrap - see build.ps1 at the end
#addin nuget:?package=Newtonsoft.Json&version=11.0.2
#addin nuget:?package=Cake.Http&version=0.6.1
#addin nuget:?package=Cake.FileHelpers&version=3.2.0
#addin nuget:?package=Cake.Sonar&version=1.1.22
#tool nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.6.0
#tool nuget:?package=JetBrains.dotCover.CommandLineTools&version=2018.3.5
#tool dotnet:?package=CycloneDX&version=0.3.3                      // will be installed at .\tools\dotnet-CycloneDX.exe

#addin nuget:?package=Cake.Coverlet&version=2.3.4
#tool dotnet:?package=coverlet.console&version=1.7.2

using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var rebuild = Argument ("rebuild", true);
var buildNumber = Argument ("buildNumber", "buildNumber");
var createArtifact = Argument ("createArtifact", false);
var target = Argument ("target", "Default");
var configuration = Argument ("configuration", "Release");
var buildmode = Argument ("buildmode", "default");
var isCI = buildmode == "CI";

var nugetConfigFile = "../PPSLambda/NuGet.config";
var nugetConfigTemplateFile = "../PPSLambda/NuGet.config.template";
var buildDir = Directory (".");
var deployDir = Directory ("../Deploy");
var environmentDir = Directory("../Environment");
var sourceRoot = Directory ("../src");
var sonarQubeAppName = "ReedExpo.Data.PPSLoader";
var buildOutputDir = Directory ("../output");
var artifactDir = buildOutputDir + Directory ("artifact");
var appBundleZip = File ("PPSLambda" + "-" + buildNumber + "-Bundle.zip");

var tempServiceDir = buildOutputDir + Directory ("temp-service");
var cyclonePath = GetFiles("./**/dotnet-CycloneDX.exe").FirstOrDefault();
var isBamboo = Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(k => k.StartsWith("bamboo_", StringComparison.OrdinalIgnoreCase));

if (cyclonePath == null)
{
    throw new CakeException("Can't find CycloneDX tool");
}
else
{
    Information($"Found CycloneDX: {cyclonePath}");
}

Verbose ("Creating Artifact: " + createArtifact);

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task ("Clean")
    .WithCriteria (rebuild)
    .Does (() => {
        CleanDirectories ("../PPSLambda/bin/" + configuration);
        CleanDirectory (buildOutputDir);
    });

Task ("RestoreNuGetPackages")
    .WithCriteria (rebuild)
    .IsDependentOn ("NuGetConfig")
    .Does (() => {
        DotNetCoreRestore ("../PPSLambda");
    });

Task ("Build")
    .IsDependentOn ("RestoreNuGetPackages")
    .Does (() => {
        DotNetCoreBuild("../PPSLambda");
    });



Task ("UnitTests")
    .Does (() => {
        // Argument customisation may not work with future versions of dotnet.exe as the -xml
        // is an XUnit specific switch.
        // See https://github.com/dotnet/cli/issues/4921
        // Using XUnit2 command failed because it cannot find xunit.dll alongside the test dll.
        if (DirectoryExists("../PPSLambda.Tests")){
            var testDir = Directory ("../PPSLambda.Tests");
            var testResultsDir = MakeAbsolute (testDir + Directory ("TestResults"));
            var coverageResultsDir = MakeAbsolute (testDir + Directory ("CoverageResults"));
            
            EnsureDirectoryExists (testResultsDir);
            EnsureDirectoryExists (coverageResultsDir);

            var coverletSettings = new CoverletSettings {
                CollectCoverage = true,
                CoverletOutputFormat = CoverletOutputFormat.opencover,
                CoverletOutputDirectory = coverageResultsDir,
                CoverletOutputName = "coverage.opencover.xml"
            };
            
            DotNetCoreTest ("../PPSLambda.Tests/PPSLambda.Tests.csproj", new DotNetCoreTestSettings {
                Configuration = "Debug",
                WorkingDirectory = testDir,
                ArgumentCustomization = args => args.Append ("--logger trx")
            });

            Information($"Testing:              {"../PPSLambda.Tests/PPSLambda.Tests.csproj"}");
            Information($"Coverage Result Dir:  {coverletSettings.CoverletOutputDirectory}");
            Information($"Coverage Output Name: {coverletSettings.CoverletOutputName}");
            Coverlet(new FilePath("../PPSLambda.Tests/PPSLambda.Tests.csproj"), coverletSettings);
        }
    });

Task ("NuGetConfig")
    .WithCriteria (isBamboo)
    .Does (() => {
        var progetUsername = EnvironmentVariableStrict("bamboo_ATLAS_PROGET_USERNAME");
        var progetPassword = EnvironmentVariableStrict("bamboo_ATLAS_PROGET_PASSWORD");
        var progetUb = new UriBuilder(EnvironmentVariableStrict("bamboo_PROGET_URL"));

        progetUb.Path = "/nuget/Default/";
        CopyFile (nugetConfigTemplateFile, nugetConfigFile);
        ReplaceTextInFiles (nugetConfigFile, "{PROGET_USERNAME}", progetUsername);
        ReplaceTextInFiles (nugetConfigFile, "{PROGET_PASSWORD}", progetPassword);
        ReplaceTextInFiles(nugetConfigFile, "{PROGET_URL}", progetUb.Uri.AbsoluteUri);
    });
Task ("CleanArtifacts")
    .Does (() => { });

Task ("AddServiceToArtifact")
    .Does (() => {
        DotNetCorePublish ("../PPSLambda", new DotNetCorePublishSettings {
            Configuration = configuration,
                OutputDirectory = tempServiceDir
        });
        EnsureDirectoryExists (artifactDir);
        Zip (tempServiceDir, artifactDir + appBundleZip);
    });

Task ("AddDeploymentScriptsToArtifact")
    .Does (() => {
        CopyDirectory (deployDir, artifactDir);
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task ("CreateArtifact")
    .IsDependentOn ("AddServiceToArtifact")
    .IsDependentOn ("AddDeploymentScriptsToArtifact");

Task ("Default")
    .IsDependentOn ("Clean")
    .IsDependentOn ("SonarBegin")
    .IsDependentOn ("Build")
    .IsDependentOn ("UnitTests")
    .IsDependentOn ("CreateBillOfMaterials")
    .IsDependentOn ("CreateArtifact")
    .IsDependentOn ("SonarEnd");

Task("SonarBegin")
  .WithCriteria(isCI)
  .Does(() => {
    var sonarQubeUsername = EnvironmentVariableStrict("bamboo_ATLAS_SONARQUBE_USERNAME");
    var sonarQubePassword = EnvironmentVariableStrict("bamboo_ATLAS_SONARQUBE_PASSWORD");


     SonarBegin(new SonarBeginSettings{
        Url = EnvironmentVariableStrict("bamboo_SONARQUBE_URL"),
        Login = sonarQubeUsername,
        Password = sonarQubePassword,
        Verbose = false,
		Key = sonarQubeAppName,
		Name  = sonarQubeAppName,
		Version ="1.0",
        VsTestReportsPath = @"..\*.Tests\TestResults\*.trx",
		OpenCoverReportsPath =  @"../*.Tests/CoverageResults/*.xml"
     });
 });

Task("SonarEnd")
  .WithCriteria(isCI)
  .Does(() => {
    var sonarQubeUsername = EnvironmentVariableStrict("bamboo_ATLAS_SONARQUBE_USERNAME");
    var sonarQubePassword = EnvironmentVariableStrict("bamboo_ATLAS_SONARQUBE_PASSWORD");

     SonarEnd(new SonarEndSettings{
        Login = sonarQubeUsername,
        Password = sonarQubePassword
     });
 });

Task("CreateBillOfMaterials")
    .WithCriteria(isCI)
    .Does(() => {
        EnsureDirectoryExists(artifactDir);

        DotNetCoreTool("../ReedExpo.Data.PPSLoader.sln", new DotNetCoreToolSettings {
            ToolPath = cyclonePath,
            ArgumentCustomization = args => args.Append($" -o {artifactDir}")
            }
        );
    });
//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget (target);

string EnvironmentVariableStrict (string key) {
    var value = EnvironmentVariable (key);
    if (value == null) {
        throw new Exception ("Environment Variable not found: " + key);
    }
    return value;
}
