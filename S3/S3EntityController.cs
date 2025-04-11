using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Builder;

//NOTE: SignalR Upload Progress hub + Extension -> app.AddUploadHub()
public class UploadProgressHub : Hub { public async Task SendProgress(string connectionId, string message) => await Clients.Client(connectionId).SendAsync("ReceiveProgress", message); }
public static class UploadHubExtensions { public static void AddUploadHub(this IApplicationBuilder app) { app.UseEndpoints(endpoints => { endpoints.MapHub<UploadProgressHub>("/uploadProgress"); }); } }

public partial class EntityController<E> : Controller where E : class, new()
{
    public IAmazonS3 s3Client;
    private readonly IHubContext<UploadProgressHub> _hubContext;
    private IHubContext<UploadProgressHub> hubContext;

    public EntityController(IHubContext<UploadProgressHub> hubContext, IAmazonS3 s3Client, DbContext ctx, IConfiguration configuration)
    {
        this.s3Client = s3Client;
        this.ctx = ctx;
        this.hubContext = hubContext;
    }

    [HttpPost("Upload")]
    public virtual async Task<Response<E>> _Upload([FromForm] E item, string connectionId) => await Upload(item, connectionId, query => query);

    [NonAction]
    public virtual async Task<Response<E>> Upload(E item, string connectionId, Func<DbSet<E>, DbSet<E>> action)
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
            stream.Position = 0; // Ensure correct read position

            var fileTransferUtility = new TransferUtility(s3Client);
            var putRequest = new TransferUtilityUploadRequest
            {
                BucketName = S3Configuration.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = s3Item.MimeType,
                DisablePayloadSigning = true,
                DisableDefaultChecksumValidation = true //NOTE: This is disabled, not needed for putRequest
            };

            putRequest.UploadProgressEvent += async (s, e) =>
            {
                if (connectionId != null)
                    await this.hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", (int)((e.TransferredBytes / (double)e.TotalBytes) * 100));
            };

            await fileTransferUtility.UploadAsync(putRequest);
        }

        query.Add(item);

        if (item is IModifiable modifiable)
            modifiable.ModDate = DateTime.Now;

        await ctx.SaveChangesAsync();
        res.item = item;

        return res;
    }
}
