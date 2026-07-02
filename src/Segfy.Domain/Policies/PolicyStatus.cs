namespace Segfy.Domain.Policies;

public enum PolicyStatus
{
    Ativa,
    Cancelada,
    Expirada
}

public static class PolicyStatusExtensions
{
    public static bool CanTransitionTo(this PolicyStatus current, PolicyStatus next) =>
        (current, next) switch
        {
            (PolicyStatus.Ativa, PolicyStatus.Cancelada) => true,
            (PolicyStatus.Ativa, PolicyStatus.Expirada) => true,
            _ => false
        };
}
