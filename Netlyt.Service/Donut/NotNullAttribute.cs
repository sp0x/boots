using System;

namespace Netlyt.Service.Donut
{
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Parameter |
        AttributeTargets.Property | AttributeTargets.Delegate |
        AttributeTargets.Field)]
    internal sealed class NotNullAttribute : Attribute
    {
    }
}