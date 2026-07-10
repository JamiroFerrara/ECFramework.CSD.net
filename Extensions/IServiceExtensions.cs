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

        Injectables.GetPage(async (item, context) =>
        {
            IAmazonS3 client = context.s3Client;
            foreach (var x in item)
            {
                if (x is IS3Object obj)
                    obj.url = await client.GeneratePreSignedURLAsync(obj.Id, obj.Name);

                // Use reflection to iterate over all properties of the SAMPLE_PACK object
                var properties = x.GetType().GetProperties();
                foreach (var property in properties)
                {
                    // Check if the property is of type IS3Object
                    if (typeof(IS3Object).IsAssignableFrom(property.PropertyType))
                    {
                        // Handle single IS3Object
                        var s3Object = property.GetValue(x) as IS3Object;
                        if (s3Object != null)
                            s3Object.url = await client.GeneratePreSignedURLAsync(s3Object.Id, s3Object.Name);
                    }
                    // Check if the property is a collection of IS3Object
                    else if (property.PropertyType.IsGenericType &&
                             typeof(IS3Object).IsAssignableFrom(property.PropertyType.GenericTypeArguments[0]))
                    {
                        // Handle collection of IS3Object
                        var collection = property.GetValue(x) as System.Collections.IEnumerable;
                        if (collection != null)
                        {
                            foreach (var itemInCollection in collection)
                            {
                                var s3Object = itemInCollection as IS3Object;
                                if (s3Object != null)
                                    s3Object.url = await client.GeneratePreSignedURLAsync(s3Object.Id, s3Object.Name);
                            }
                        }
                    }
                }
            }
        });

        Injectables.GetItem(async (item, context) =>
        {
            IAmazonS3 client = context.s3Client;

            if (item is IS3Object obj)
                obj.url = await client.GeneratePreSignedURLAsync(obj.Id, obj.Name);

            var properties = item.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (typeof(IS3Object).IsAssignableFrom(property.PropertyType))
                {
                    var s3Object = property.GetValue(item) as IS3Object;
                    if (s3Object != null)
                        s3Object.url = await client.GeneratePreSignedURLAsync(s3Object.Id, s3Object.Name);
                }
                else if (property.PropertyType.IsGenericType &&
                         typeof(IS3Object).IsAssignableFrom(property.PropertyType.GenericTypeArguments[0]))
                {
                    var collection = property.GetValue(item) as System.Collections.IEnumerable;
                    if (collection != null)
                    {
                        foreach (var itemInCollection in collection)
                        {
                            var s3Object = itemInCollection as IS3Object;
                            if (s3Object != null)
                                s3Object.url = await client.GeneratePreSignedURLAsync(s3Object.Id, s3Object.Name);
                        }
                    }
                }
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
