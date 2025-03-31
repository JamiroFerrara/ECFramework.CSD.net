using System.Collections.Generic;
using CSD.Framework.NetCore.Service.Classes;

namespace ECFramework;

public class Response<E> : CSDResponse
{
    public List<E> items { get; set; }
    public E item { get; set; }
    public int totalPages { get; set; }
    public int totalItems { get; set; }

    public byte[] file { get; set; }
    public string fileName { get; set; }

    public bool canRead { get; set; }
    public bool canWrite { get; set; }
}
