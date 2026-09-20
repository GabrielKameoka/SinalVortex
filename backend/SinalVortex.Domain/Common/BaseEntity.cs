using SinalVortex.Domain.Exceptions;

namespace SinalVortex.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public Guid TenantId { get; protected set; }
    public DateTime CriadoEm { get; protected set; }
    public DateTime? AtualizadoEm { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CriadoEm = DateTime.UtcNow;
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
        AtualizadoEm = DateTime.UtcNow;
    }
}
