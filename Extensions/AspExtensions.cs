using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace ECFramework;

public static class AspExtensions
{
    public async static Task<string> GenerateSwaggerFile(this IApplicationBuilder app)
    {
        Thread.Sleep(500); // Wait for the server to start
        var test = "pwd".Run();

        var res = "";
        using (HttpClient client = new HttpClient())
        {
            string url = "http://localhost:8080/swagger/v1/swagger.json"; //FIX: Create global env variables for ports ()
            HttpResponseMessage response = await client.GetAsync(url);
            res = await response.Content.ReadAsStringAsync();
            File.WriteAllText("./swagger.json", res);
        }

        var out1 = "npx swagger-typescript-api@13.0.23 --axios --module-name-index 1 --unwrap-response-data -t ./client/templates -p swagger.json -o ./client -n client.ts".Run();
        return res;
    }
}
