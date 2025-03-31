using System;
namespace ECFramework;

public class UnauthorizedException : Exception { public UnauthorizedException() : 
    base("L'utente non è abilitato ad accedere all'applicazione") { } }
