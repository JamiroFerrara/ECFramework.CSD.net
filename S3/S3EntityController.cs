using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public partial class EntityController<E> : Controller where E : class, new()
{
    public IAmazonS3 s3Client;

    public EntityController(IAmazonS3 s3Client, DbContext ctx, IConfiguration configuration)
    {
        this.s3Client = s3Client;
        this.ctx = ctx;
    }

    [HttpPost("Upload")]
    public virtual async Task<Response<E>> _Upload([FromForm] E item) => await Upload(item, query => query);

    [NonAction]
    public virtual async Task<Response<E>> Upload(E item, Func<DbSet<E>, DbSet<E>> action)
    {
        //BUG: Context disappears in Try(() => ) for some fucking reason

        if (item is null)
            throw new Exception("Entity must be not null");

        if (item is not S3Object s3Item)
            throw new Exception("Entity must be a S3Object");

        if (s3Item.file == null || s3Item.file.Length == 0)
            throw new Exception("One of the files is empty");

        var res = new Response<E>();

        this.CheckReflectiveId(item);
        DbSet<E> query = ctx.Set<E>();
        query = action(query);

        s3Item.Id = Guid.NewGuid();
        string key = $"{s3Item.Id}/{s3Item.Name}";

        using (var stream = s3Item.file.OpenReadStream())
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = S3Configuration.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = s3Item.MimeType, // Ensure proper content type
                AutoResetStreamPosition = true, // Important to prevent SHA-256 mismatch
                UseChunkEncoding = false, // Disable chunked transfer encoding for S3-compatible providers
            };

            await s3Client.PutObjectAsync(putRequest);
        }
        query.Add(item);

        if (item is IModifiable modifiable)
            modifiable.ModDate = DateTime.Now;

        await ctx.SaveChangesAsync();
        res.item = item;

        return res;
    }
}
