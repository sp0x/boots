using System;

public class DonutFunctionNotImplementedException : Exception
{
    public DonutFunctionNotImplementedException(string message) : base($"Donut fn not implemented: ${message}")
    {
    }
}