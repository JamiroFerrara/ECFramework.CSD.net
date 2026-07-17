using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

public interface IS3Object
{
    Guid Id { get; set; }
    string Name { get; set; }
    string Path { get; set; }
    string Bucket { get; set; }
    string MimeType { get; set; }
    
    string url { get; set; } // Note: Consider using PascalCase for property names
    IFormFile file { get; set; } // Note: This property is for upload only
}
public class S3Object : IS3Object
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Path { get; set; }
    public string Bucket { get; set; }
    public string MimeType { get; set; }

    [NotMapped]
    public string url { get; set; }

    [NotMapped] //NOTE: upload only
    public IFormFile file { get; set; }

    public IEnumerable<DownloadableFile> GetFiles()
    {
        yield return new DownloadableFile(Id, Name);
    }
}
