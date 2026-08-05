using System;

public interface IHealth
{
    event Action<float, float> HealthChanged;

    float CurrentHealth { get; }
    float MaxHealth { get; }
}
