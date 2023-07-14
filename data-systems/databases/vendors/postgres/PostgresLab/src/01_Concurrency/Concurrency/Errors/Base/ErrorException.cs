﻿namespace Transactions.Errors.Base;

public class ErrorException : Exception
{
    protected ErrorException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}
