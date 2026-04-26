# Mapping Package

`Mapping`, assembly bazli register toplama ile Mapster kurulumunu sadeleştirir.

## Neler Saglar

- `AddAparesk.EskineriaMapping(params Assembly[] assemblies)`
- `TypeAdapterConfig`
- `IMapper`
- assembly bazli `MappingProfile` taramasi (`IMapFrom<T>`)

## Ic Paket Bagimliliklari

- Zorunlu bir ic paket bagimliligi yoktur

## External Gereksinimler

- Mapping attribute veya profile metadata'si bulunan assembly'ler

## Register

```csharp
services.AddAparesk.EskineriaMapping(
    typeof(Program).Assembly,
    typeof(SomeCoreType).Assembly);
```

## Kullanim Ornegi

```csharp
public class UserAppService
{
    private readonly IMapper _mapper;

    public UserAppService(IMapper mapper)
    {
        _mapper = mapper;
    }
}
```

## Not

Bu paket Mapster'i explicit `MappingProfile(assembly)` modeliyle kurar. Bu sayede implicit scan kaynakli construction hatalari daha az olur.

- Mapping tipleri fail-fast sekilde dogrulanir; ctor veya `Mapping(TypeAdapterConfig)` eksikse startup'ta net hata verir.
- `IMapper` `scoped` register edilir; boylece scoped resolver bagimliliklari guvenli sekilde cozulur.
- `TypeAdapterConfig.Compile()` ile hatali map konfigurasyonlari erken yakalanir.
