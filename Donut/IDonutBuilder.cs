using System;

namespace Donut
{
    public interface IDonutBuilder
    {
        Type DonutType { get; }
        Type DonutContextType { get; }
        IDonutfile Generate();

        void SetEmitterType(Type featureEmitterType);
        Type GetEmitterType();
    }
}