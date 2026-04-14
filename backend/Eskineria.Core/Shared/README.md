# Shared Package

`Shared`, diger paketlerin ortak dilini tasir. Response tipleri, ortak exception, localization key'leri, startup helper'lari ve temel config modelleri burada bulunur.

## Neler Saglar

- `Response`, `DataResponse<T>`, `PagedResponse<T>`
- `DomainException`
- `LocalizationKeys`
- `SystemSettingKeys`
- `PagingOptions`
- startup helper extension'lari

## Ic Paket Bagimliliklari

- Diger neredeyse tum paketler bu modulu kullanir

## External Gereksinimler

- Yok

## Kullanim Ornegi

```csharp
return DataResponse<UserDto>.Succeed(userDto);
```

```csharp
return Response.Fail("Operation failed.");
```

## Neden Ayridir

Bu paket "business feature" tasimaz. Diger modullerin ortak kontrat ve primitive'lerini tek yerde toplar.

## Guvenlik ve Performans Notlari

- `Response` mesajlari trimlenir ve kontrol karakterleri temizlenir (log/response kirilmasini azaltir).
- `PagingOptions` icinde merkezi `NormalizePageNumber` ve `NormalizePageSize` bulunur; max/default ayarlari guvenli fallback ile clamp edilir.
- `LocalizedContent` culture degerini normalize eder (`_` -> `-`) ve gecerli culture degilse `en-US` fallback uygular.
- `LocalizedContent` neutral-culture seciminde siralama tabanli LINQ yerine tek gecisli secim yapar (daha az allocation).
- Startup command handler, arguman yoksa DI scope olusturmadan erken doner.
