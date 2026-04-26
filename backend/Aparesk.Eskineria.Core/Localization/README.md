# Localization Package

`Localization`, hem JSON dosyasi tabanli hem database tabanli localizer akisini tasir. Runtime string localizer factory, localization management API ve sync servisi bu pakettedir.

## Neler Saglar

- `AddEskineriaLocalization(...)`
- `AddJsonLocalization(...)`
- `AddDatabaseLocalization(...)`
- `LocalizationController`
- `ILocalizationService`
- `LocalizationSyncService`
- runtime okumada sadece `WorkflowStatus = Published` kaynaklarin donmesi
- sync sirasinda farkli `ResourceSet` degerine sahip mevcut kayitlarin ezilmesini engelleyen koruma
- API seviyesinde bos/whitespace culture alanlari icin erken validasyon ve kontrollu `400 BadRequest` donusu

## Ic Paket Bagimliliklari

- `Shared`: ortak localization key ve response tipleri
- `Repository`: database mode'da repository bazli sorgular icin gerekir
- `Settings`: aktif culture/fallback culture ile birlikte kullanildiginda deger kazanir

## External Gereksinimler

- JSON mode icin localization dosyalari
- Database mode icin `LanguageResource` persistence'i
- `IStringLocalizer<T>` kullanan servisler

## Register

JSON mode:

```csharp
services.AddJsonLocalization(configuration);
```

Database mode:

```csharp
services.AddDatabaseLocalization(configuration);
services.AddScoped<ILanguageResourceRepository, LanguageResourceRepository>();
services.AddScoped<ILocalizationService, LocalizationService>();
```

## Kullanim Ornegi

```csharp
public class UserService
{
    private readonly IStringLocalizer<UserService> _localizer;

    public UserService(IStringLocalizer<UserService> localizer)
    {
        _localizer = localizer;
    }
}
```

```csharp
var resources = await _localizationService.GetResourcesAsync("tr-TR");
```

Not:

- Runtime localizer (`IStringLocalizer`) ve `GetResourcesAsync` draft kayitlari servis etmez.
- Draft kayitlar sadece yonetim akisinda gorunur ve `Publish` sonrasi runtime'a yansir.

## Ne Zaman Database Mode Secilir

- runtime localization duzenleme gerekiyorsa
- yonetim panelinden translation publish etmek isteniyorsa
- frontend ve backend resource'lari tek merkezden yonetilecekse
