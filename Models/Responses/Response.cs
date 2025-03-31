using System.Collections.Generic;

public class Response<E>
{
    public List<E> items { get; set; }
    public E item { get; set; }
    public int totalPages { get; set; }
    public int totalItems { get; set; }

    public byte[] file { get; set; }
    public string fileName { get; set; }
    public string error { get; set; }

    public bool canRead { get; set; }
    public bool canWrite { get; set; }
}
