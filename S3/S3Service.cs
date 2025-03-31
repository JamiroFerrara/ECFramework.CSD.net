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
        // Initialize the S3Configuration with the provided configuration
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

    public static async Task<string> GeneratePreSignedURLAsync(this IAmazonS3 client, Guid objectId, string objectName)
    {
        // Construct the key for the S3 object
        string key = $"{objectId}/{objectName}";
        // Set the expiration time for the pre-signed URL
        var expiration = DateTime.UtcNow.AddMinutes(15); // URL valid for 15 minutes TODO: Put in config
                                                         // Generate the pre-signed URL
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
        // Construct the key for the S3 object
        string key = $"{objectId}/{objectName}";
        // Create the delete request
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = S3Configuration.BucketName,
            Key = key
        };
        
        // Delete the object
        await client.DeleteObjectAsync(deleteRequest);
        Console.WriteLine($"Successfully deleted object: {key}");
    }

}

