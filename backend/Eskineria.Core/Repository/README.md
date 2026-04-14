# Repository Package

`Repository`, generic EF Core repository, read repository, specification ve paging destegini sunar.

## Neler Saglar

- `AddEskineriaRepository()`
- `AddEskineriaRepository<TContext>()`
- `IRepository<TContext, TEntity>`
- `IReadRepository<TContext, TEntity>`
- specification ve paging altyapisi

## Ic Paket Bagimliliklari

- `Shared`: paging response modelleri ile birlikte kullanildiginda daha degerlidir

## External Gereksinimler

- EF Core `DbContext`
- Entity ve specification tanimlari

## Register

```csharp
services.AddDbContext<ApplicationDbContext>(...);
services.AddEskineriaRepository<ApplicationDbContext>(options =>
{
    options.AutoSave = false;
});
```

## Kullanim Ornegi

```csharp
public class UserReadService
{
    private readonly IReadRepository<ApplicationDbContext, User> _users;

    public UserReadService(IReadRepository<ApplicationDbContext, User> users)
    {
        _users = users;
    }
}
```

## Mimari Not

Bu paket generic repository verir. Is kurali tasiyan feature'larda dogrudan generic read repository yerine feature-specific repository kontrati uzerinden gitmek daha temizdir.

## Guvenlik ve Performans Notlari

- Specification paging degerleri normalize edilir (`skip/take` negatif olamaz).
- Repository option tarafinda `MaxPageSize` ust sinirla clamp edilir.
- Generic repository metotlari null-guard ile fail-fast davranir.
- Paged query tarafinda count/query akisi ayrik tutulur; include/tracking adimlari gerektigi noktada uygulanir.
