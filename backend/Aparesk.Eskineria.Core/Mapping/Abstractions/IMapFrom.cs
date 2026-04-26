using Mapster;

namespace Aparesk.Eskineria.Core.Mapping.Abstractions;

public interface IMapFrom<T>
{
    void Mapping(TypeAdapterConfig config) => config.NewConfig(typeof(T), GetType());
}
