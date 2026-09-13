namespace SinalVortex.Domain.Exceptions;

using System;

// Indica falhas temporárias (HTTP 503, Timeout, Circuit Breaker). O Worker fará RETRY.
public class TransientChannelException : Exception
{
    public TransientChannelException(string message) : base(message) { }
    public TransientChannelException(string message, Exception innerException) : base(message, innerException) { }
}

// Indica falhas definitivas (E-mail/Telefone inválido, HTTP 400/404). O Worker enviará direto para a DLQ.
public class PermanentChannelException : Exception
{
    public PermanentChannelException(string message) : base(message) { }
    public PermanentChannelException(string message, Exception innerException) : base(message, innerException) { }
}