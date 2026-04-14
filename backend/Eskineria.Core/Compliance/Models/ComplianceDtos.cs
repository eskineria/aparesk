using Eskineria.Core.Mapping.Abstractions;
using Eskineria.Core.Compliance.Entities;
using TypeAdapterConfig = Mapster.TypeAdapterConfig;

namespace Eskineria.Core.Compliance.Models;

public class TermsDto : IMapFrom<TermsAndConditions>
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    public void Mapping(TypeAdapterConfig config)
    {
        config.NewConfig<TermsAndConditions, TermsDto>();
    }
}

public class CreateTermsDto
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class UpdateTermsDto
{
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public bool IsActive { get; set; }
}

public class UserTermsAcceptanceDto : IMapFrom<UserTermsAcceptance>
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TermsAndConditionsId { get; set; }
    public DateTime AcceptedAt { get; set; }
    public string? IpAddress { get; set; }

    public void Mapping(TypeAdapterConfig config)
    {
        config.NewConfig<UserTermsAcceptance, UserTermsAcceptanceDto>();
    }
}

public class AcceptTermsDto
{
    public Guid TermsAndConditionsId { get; set; }
}
