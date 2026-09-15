using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public Guid TenantId { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected BaseEntity(Guid tenantId) : this()
    {
        SetTenantId(tenantId);
    }

    public void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException("TenantId é obrigatório.");

        TenantId = tenantId;
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}