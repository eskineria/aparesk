# Auditing Package

`Auditing`, runtime action loglama ve audit log sorgulama akislarini tek yerde toplar.
Bu paket; action filter, store abstraction, persistence bridge, policy provider, repository katmani ve API endpointlerini kapsar.

## Neler Saglar

- `AddAppAuditing<TStore>()`
- `AuditActionFilter`
- `IAuditingStore`, `IAuditingPersistence`
- `IAuditLogService`, `IAuditLogRepository`, `IAuditLogIntegrityRepository`
- `AuditLogsController`

## Guvenlik Davranisi

- Action argument loglarinda hassas alan isimleri (`password`, `token`, `secret`, `authorization`, vb.) `***REDACTED***` olarak yazilir.
- Query string degerleri loglanmaz; sadece query key listesi loglanir.
- Audit hardening chain'i icin secret zorunludur:
  - `Auditing:Hardening:HmacSecret` (onerilen)
  - fallback: `JwtSettings:Secret`
- Secret yoksa veya 32 karakterden kisa ise:
  - normal audit log kaydi devam eder (best-effort),
  - integrity chain yazimi devre disi kalir.

## Performans Notlari

- Audit read/query akislarinda `AsNoTracking` kullanilir.
- Ayar okuma tarafinda reflection yerine dogrudan EF sorgusu kullanilir (`Settings` tablosu).
- Filter payload'i minimal tutulur: tam query yerine yol + query keyleri yazilir.

## Ic Paket Bagimliliklari

- `Shared`: response ve paging yapilari
- `Auth`: actor claim ve kullanici baglami ile daha zengin log cikarimi icin onerilir
- `Settings`: audit policy cache invalidation ve feature flag entegrasyonu icin kullanilir

## External Gereksinimler

- Bir `IAuditingStore` implementasyonu
- Audit kaydini kalici hale getirecek persistence sinifi
- MVC filter kaydi
- Full query API icin audit repository implementasyonlari

## Register

```csharp
services.AddScoped<IAuditingPersistence, EfAuditingPersistence>();
services.AddAppAuditing<DbAuditingStore>();
services.AddScoped<IAuditLogRepository, AuditLogRepository>();
services.AddScoped<IAuditLogIntegrityRepository, AuditLogIntegrityRepository>();
services.AddScoped<IAuditLogService, AuditLogService>();

services.AddControllers(options =>
{
    options.Filters.AddService<AuditActionFilter>();
});
```

## Kullanim Ornegi

```csharp
public class OrdersController : ControllerBase
{
    [HttpPost]
    public IActionResult Create() => Ok();
}
```

Controller MVC pipeline'ina girdiginde `AuditActionFilter`, request metadata'sini toplayip store'a yazar.

## Paket Siniri

Bu paket kural bazli uyari orkestrasyonu tasimaz.
Odak alani: loglama, sorgulama, diff ve integrity ozetidir.
