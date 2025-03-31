using System;


/// <summary>
/// By implementing this you get automatic ModDate updates from framework
/// </summary>
public interface IModifiable
{
    public DateTime? ModDate { get; set; }
}


public interface ICreateable
{
    public DateTime CreatedAt { get; set; }
}
