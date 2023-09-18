#addin nuget:?package=AWSSDK.Core&version=3.7.0.44
#addin nuget:?package=AWSSDK.SecurityToken&version=3.7.1.32
#addin nuget:?package=AWSSDK.Lambda&version=3.7.1.5
#addin nuget:?package=AWSSDK.KeyManagementService&version=3.7.1.11
#addin nuget:?package=AWSSDK.CloudFormation&version=3.7.3.9
#addin nuget:?package=AWSSDK.S3&version=3.7.1.14
#addin nuget:?package=YamlDotNet&version=6.0.0
#addin nuget:?package=Cake.Http&version=0.6.1
#addin nuget:?package=Cake.FileHelpers&version=3.2.0
#addin nuget:?package=SharpZipLib&version=1.1.0
#addin nuget:?package=Firefly.CrossPlatformZip&version=0.5.0
#addin nuget:?package=Polly&version=7.2.0

#addin nuget:?package=ReedExpo.Cake.Base&version=1.0.7
#addin nuget:?package=ReedExpo.Cake.AWS.Base&version=1.0.25
#addin nuget:?package=ReedExpo.Cake.AWS.CloudFormation&version=2.0.109
#addin nuget:?package=ReedExpo.Cake.AWS.BuildAuthentication&version=1.0.18
#addin nuget:?package=ReedExpo.Cake.AWS.Lambda&version=1.1.25
#addin nuget:?package=ReedExpo.Cake.CycloneDX&version=1.0.7
#addin nuget:?package=ReedExpo.Cake.CrossPlatformZip&version=1.0.18

#addin nuget:?package=ReedExpo.Cake.ServiceNow&version=1.0.27
#addin nuget:?package=Firefly.EmbeddedResourceLoader&version=0.1.3

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Cake.FileHelpers;

// Arguments
var appName = "PPSLambda";
var lambdaFunctionArn = Argument<string>("lambdaFunctionArn");
var buildNumber = Argument<string>("buildNumber");
var deployVersion = Argument<string>("deployVersion");
var target = Argument("target", "Default");
var splunkToken = Argument("splunkToken", "");
var bomFile = File("./bom.xml");
var s3BucketName = Argument<string>("s3BucketName");
var ringBoxURL = Argument<string>("ringBoxURL");
var royalBoxURL = Argument<string>("royalBoxURL");

var lambdaEnvironment = new Dictionary<string, string>();

// Config
AWSCredentials awsCredentials;
var environmentName = EnvironmentVariableOrDefault("bamboo_deploy_environment", "local");
var deploymentPackageName = appName + "-" + buildNumber + "-Bundle";
var deploymentPackageZipFileName = deploymentPackageName + ".zip";
var temp = "./temp";
var packageTempDir = temp + "/" + deploymentPackageName;
var repackagedDeploymentPackageZipFileName = temp + "/" + Directory(deploymentPackageZipFileName);
var cloudFormationTemplatePath = File("PPSLambdaCloudFormation.json");
var configFileName  = "config.json";
var targetConfigFileName = packageTempDir + "/" + configFileName;

string cloudFormationStackName;
string changeRequestNumber;
var isRunningInBamboo = Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(k => k.StartsWith("bamboo_", StringComparison.OrdinalIgnoreCase));
string featureToggles;

//////////////////////////////////////////////////////////////////////
// ENTRIES
//////////////////////////////////////////////////////////////////////
Task("Initialise")
    .Does(() => {

        //////////////////////////////////////////////////////////////////////
        // Authenticate with AWS.
        // See
        // https://bitbucket.org/coreshowservices/reedexpo.cake.aws.buildauthentication
        //////////////////////////////////////////////////////////////////////
        awsCredentials = GetBuildAwsCredentials();
        featureToggles = EnvironmentVariableOrDefault("bamboo_FEATURE_TOGGLES", "");
    });


Task("CleanTemp")
    .Does(() =>
    {
        CleanDirectory(temp);
        CreateDirectory(packageTempDir);
    });

Task("UnpackageSourceCode")
    .Does(() =>
    {
        Information("unzipping  " + deploymentPackageZipFileName + " to " + packageTempDir);
        Unzip(deploymentPackageZipFileName, packageTempDir);
    });

Task("AppendConfig")
    .Does(() =>
    {
        var config = DeserializeJsonFromFile<JObject>(targetConfigFileName);

        config["deployVersion"] = deployVersion;
        config["buildNumber"] = buildNumber;
        config["splunkKey"] = splunkToken;
        config["s3BucketName"] = s3BucketName;
        config["ringBoxURL"] = ringBoxURL;
        config["royalBoxURL"] = royalBoxURL;
        config["featureToggles"] = featureToggles;

        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject (config, Newtonsoft.Json.Formatting.Indented);
        FileWriteText(targetConfigFileName, jsonString);
    });

Task("RepackageSource")
    .Does(() =>
    {
        Information("repackaging  " + packageTempDir + " to " + repackagedDeploymentPackageZipFileName);

        ZipX(new ZipSettings{
            Artifacts = packageTempDir,
            ZipFile = repackagedDeploymentPackageZipFileName,
            TargetPlatform = ZipPlatform.Unix
        });
    });

Task("Publish-Version")
    .IsDependentOn("UploadBillOfMaterials")
    .Description("Publishes a version of your function")
    .Does(() =>
{
    DeployLambda(new DeployLambdaSettings {
        Credentials = awsCredentials,
        DeploymentPackage = repackagedDeploymentPackageZipFileName,
        FunctionName = lambdaFunctionArn,
        EnvironmentVariables = lambdaEnvironment
    });
});

Task("CloudFormationDeployment")
    .IsDependentOn ("RaiseChangeRequest")
    .WithCriteria(environmentName != "local")
    .Does(() => {

        // Sort out the CF stack name, as it does not follow convention!
        var awsAccountId = GetAwsAccountId(awsCredentials);

        switch (awsAccountId)
        {
            case "915203318988":

                // rxplatformrefresh
                cloudFormationStackName = $"PPSLambda{environmentName.Substring(0,1).ToUpper() + environmentName.Substring(1).ToLower()}";
                break;

            case "324811521787":
            case "612155760304":

                // preprod, prod
                cloudFormationStackName = $"PPSLambda-{environmentName}".ToLower();
                break;

            default:

                throw new CakeException($"Running in unsupported AWS account {awsAccountId}");
        }
        
        var cfParams = new Dictionary<string, string>();
        cfParams.Add("BillingEnvironmentName", environmentName);

        var result = RunCloudFormation(
            new RunCloudFormationSettings
            {
                Credentials = awsCredentials,
                Region = RegionEndpoint.EUWest1,
                StackName = cloudFormationStackName,
                TemplatePath = cloudFormationTemplatePath.Path.FullPath,
                Parameters = cfParams
            }
        );

        Information($"Stack update result was {result}");
    });

Task("UploadBillOfMaterials")
    .WithCriteria(environmentName == "prod")
    .Does(() => {

        UploadBillOfMaterials(
            new UploadBillOfMaterialsSettings {
                ApplicationName = appName,
                BomFile = File(bomFile),
                EnvironmentName = environmentName
            }
        );
    });

Task("RaiseChangeRequest")
    .WithCriteria(environmentName == "prod") // Have added and condition to raise for Prod Only.
	.Does(() => {
        changeRequestNumber = RaiseChangeRequest(new ChangeRequestSettings());
    });

//////////////////////////////////////////////////////////////////////
// POST DEPLOYMENT
//////////////////////////////////////////////////////////////////////

Teardown(context => {

        // Teardown is EXECUTED by -WhatIf. Only permit execution when running in Bamboo
        if (isRunningInBamboo && !string.IsNullOrWhiteSpace(changeRequestNumber))
        {
            Information("Inside teardown");
            CloseChangeRequest(new ChangeRequestSettings
            {
                SystemId = changeRequestNumber,
                Success  = context.Successful,
                EnvironmentName = environmentName
            });
        }
 });

Task("Default")
    .IsDependentOn("Initialise")
    .IsDependentOn("CleanTemp")
    .IsDependentOn("UploadBillOfMaterials")
    .IsDependentOn("CloudFormationDeployment")
    .IsDependentOn("UnpackageSourceCode")
    .IsDependentOn("AppendConfig")
    .IsDependentOn("RepackageSource")
    .IsDependentOn("Publish-Version");

RunTarget(target);

T DeserializeJsonFromFile<T> (FilePath filename)
{
    var json = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(filename.ToString()));
    return JsonConvert.DeserializeObject<T> (json);
}

string EnvironmentVariableOrDefault(string key, string defaultValue)
{
    var value = EnvironmentVariable(key);
    return value ?? defaultValue;
}

Uri GetHostUri(string url)
{
    var uri = new Uri(url);

    return new Uri($"{uri.Scheme}://{uri.Host}");
}

string SetUriPath(Uri host, string path)
{
    var ub = new UriBuilder(host);

    ub.Path = path;
    return ub.ToString();
}