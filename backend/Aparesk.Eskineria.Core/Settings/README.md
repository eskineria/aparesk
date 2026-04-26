# Settings Package

`Settings`, sistem genel ayarlari, auth runtime policy'leri, branding upload ve maintenance mode davranisini yoneten pakettir.

## Neler Saglar

- `ISystemSettingsService`
- `SystemSettingsController`
- `MaintenanceModeMiddleware`
- `SystemSettingsSeedService`
- auth, maintenance, notification, localization ve audit feature flag'leri

## Ic Paket Bagimliliklari

- `Shared`: `SystemSettingKeys` ve response modelleri
- `Storage`: branding logo ve favicon upload icin gerekir
- `Auditing`: audit logging policy cache invalidation icin kullanilir
- `Auth`: maintenance middleware'de kullanici ve role kararlarini almak icin gerekir
- `Localization`: settings ekran mesajlari ve public culture bilgileri icin onerilir
- `Repository`: settings persistence kontratlari icin gerekir

## External Gereksinimler

- `ISettingRepository`
- `IStorageService`
- host pipeline'da middleware kaydi

## Register

Bu paket su anda tek extension method yerine explicit DI ile baglanir:

```csharp
services.AddScoped<ISettingRepository, SettingRepository>();
services.AddScoped<ISystemSettingsService, SystemSettingsService>();
services.AddScoped<SystemSettingsSeedService>();
```

```csharp
app.UseMiddleware<MaintenanceModeMiddleware>();
```

## Kullanim Ornegi

```csharp
var authSettings = await _systemSettingsService.GetAuthSettingsAsync();
var isLoginEnabled = await _systemSettingsService.IsLoginEnabledAsync();
```

## Ozellikle Gerekli Oldugu Yerler

- login, register ve MFA runtime kararlarini DB'den okumak
- maintenance mode acip kapatmak
- public auth settings endpoint'i saglamak
- branding asset yuklemek

## Guvenlik ve Performans Notlari

- Culture ve email gibi kritik alanlar normalize/validate edilir.
- Text tabanli setting alanlarinda kontrol karakterleri temizlenir.
- Maintenance middleware kisa TTL cache ile gereksiz DB okuma yukunu azaltir.
