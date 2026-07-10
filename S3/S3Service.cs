using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class S3Service
{
    public static void AddS3(this IServiceCollection services, IConfiguration configuration)
    {
        S3Configuration.Initialize(configuration);

        var s3Config = new AmazonS3Config
        {
            ServiceURL = S3Configuration.Endpoint,
            ForcePathStyle = true,
            SignatureVersion = "4",
            AuthenticationRegion = S3Configuration.Region
        };

        services.AddSingleton<IAmazonS3>(sp =>
            new AmazonS3Client(S3Configuration.AccessKey, S3Configuration.SecretKey, s3Config));
    }

    public static Task<string> GeneratePreSignedURLAsync(this IAmazonS3 client, Guid objectId, string objectName)
    {
        // Fast path: construct URL from CDN domain — no R2 S3 API call
        if (!string.IsNullOrEmpty(S3Configuration.PublicUrl))
            return Task.FromResult(GetPublicUrl(objectId, objectName));

        return GeneratePreSignedURLInternalAsync(client, objectId, objectName);
    }

    public static string GetPublicUrl(Guid objectId, string objectName)
    {
        return $"{S3Configuration.PublicUrl.TrimEnd('/')}/{objectId}/{objectName}";
    }

    private static async Task<string> GeneratePreSignedURLInternalAsync(IAmazonS3 client, Guid objectId, string objectName)
    {
        string key = $"{objectId}/{objectName}";
        var expiration = DateTime.UtcNow.AddMinutes(15);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = S3Configuration.BucketName,
            Key = key,
            Expires = expiration,
            Protocol = Protocol.HTTPS,
        };

        return await client.GetPreSignedURLAsync(request);
    }

    public static async Task DeleteObjectAsync(this IAmazonS3 client, Guid objectId, string objectName)
    {
        string key = $"{objectId}/{objectName}";
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = S3Configuration.BucketName,
            Key = key
        };

        await client.DeleteObjectAsync(deleteRequest);
        Console.WriteLine($"Successfully deleted object: {key}");
    }
}
