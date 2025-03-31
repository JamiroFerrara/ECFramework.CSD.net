using System;
using System.Collections.Generic;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;

public static class IServiceExtensions
{
    public static void HandleInjectables(this IServiceCollection services)
    {
        Injectables.Create<IModifiable>((item, context) => item.ModDate = DateTime.Now);
        Injectables.Create<ICreateable>((item, context) => item.CreatedAt = DateTime.Now);

        //TODO: Mabye this should be called n times
        Injectables.GetPage(async (item, context) =>
        {
            IAmazonS3 client = context.s3Client;
            foreach (var x in item)
            {
                if (x is IS3Object s3Object)
                    s3Object.url = await client.GeneratePreSignedURLAsync(s3Object.Id, s3Object.Name);
            }
        });

        Injectables.Delete(async (item, context) =>
        {
            IAmazonS3 client = context.s3Client;
            if (item is IS3Object s3Object)
                await client.DeleteObjectAsync(s3Object.Id, s3Object.Name);
        });

        Injectables.Update<IModifiable>((item, context) => item.ModDate = DateTime.Now);

        Injectables.LogicalDelete<IModifiable>((item, context) => item.ModDate = DateTime.Now);
        Injectables.LogicalDelete<ISoftDeletable>((item, context) => item.DeletedAt = DateTime.Now);
    }
}
