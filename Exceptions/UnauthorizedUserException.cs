using System;

public class UnauthorizedUserException : Exception
{
    public int ErrorCode { get; set; }

    public UnauthorizedUserException() : base("L'utente non è abilitato ad accedere all'applicazione")
    {
        ErrorCode = 2;
    }
}
