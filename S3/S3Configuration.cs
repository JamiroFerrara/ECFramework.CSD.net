using Microsoft.Extensions.Configuration;

public static class S3Configuration
{
    public static string AccessKey { get; private set; }
    public static string SecretKey { get; private set; }
    public static string BucketName { get; private set; }
    public static string Region { get; private set; }
    public static string Endpoint { get; private set; }
    public static void Initialize(IConfiguration configuration)
    {
        var awsOptions = configuration.GetSection("AWS");
        AccessKey = awsOptions["AccessKey"];
        SecretKey = awsOptions["SecretKey"];
        BucketName = awsOptions["BucketName"];
        Region = awsOptions["Region"];
        Endpoint = awsOptions["Endpoint"];
    }
}
