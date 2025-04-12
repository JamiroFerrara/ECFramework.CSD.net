//NOTE: SignalR Upload Progress hub + Extension -> app.AddUploadHub()
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;

public static class UploadHubExtensions { public static void AddUploadHub(this IApplicationBuilder app) { app.UseEndpoints(endpoints => { endpoints.MapHub<S3UploadProgressHub>("/uploadProgress"); }); } }

public interface IS3UploadProgressHub
{
    Task onProgress(string progress);
    Task onMessage(string message);
}

public class S3UploadProgressHub : Hub<IS3UploadProgressHub>
{
    public async Task SendProgress(string connectionId, string message) => 
        await Clients.Client(connectionId).onProgress(message);

    public async Task SendMessage(string connectionId, string message) => 
        await Clients.Client(connectionId).onMessage(message);
}
