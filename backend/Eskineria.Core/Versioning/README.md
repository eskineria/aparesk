# Versioning Package

`Versioning`, API versioning stratejilerini tek yerde toplar. URL segment, header ve query string varyantlarini destekler.

## Neler Saglar

- `AddEskineriaVersioning(...)`
- `AddEskineriaUrlSegmentVersioning()`
- `AddEskineriaHeaderVersioning()`
- `AddEskineriaQueryStringVersioning()`
- Header/query version token adlari icin guvenli karakter dogrulamasi

## Ic Paket Bagimliliklari

- Zorunlu ic paket bagimliligi yoktur

## External Gereksinimler

- ASP.NET API versioning paketleri
- controller route'larinda version placeholder kullanimi

## Register

```csharp
services.AddEskineriaUrlSegmentVersioning();
```

## Controller Ornegi

```csharp
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class UsersController : ControllerBase
{
}
```

## Hangi Mod Ne Zaman Kullanilir

- URL segment: public API ve dokumantasyon icin en okunabilir yol
- Header: temiz URL isteyen client'lar icin uygun
- Query string: geriye donuk uyumluluk gereken durumlarda faydali

## Guvenlik Notlari

- Header veya query parameter adlari bos, whitespace veya gecersiz karakter iceriyorsa startup asamasinda fail-fast olur.
- Version token adlari sadece `A-Z`, `a-z`, `0-9`, `.`, `_`, `-` karakterlerini kabul eder.
