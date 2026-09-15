namespace SinalVortex.Application.Common.Interfaces;

public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
}