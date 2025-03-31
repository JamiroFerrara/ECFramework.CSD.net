using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ECFramework;

public interface ISoftDeletable
{
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; }
}
