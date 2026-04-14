# ExceptionHandler Package

`ExceptionHandler`, uygulama genelinde exception to problem-details cevirisini standartlastirir.

## Neler Saglar

- `AddEskineriaExceptionHandler(...)`
- exception type to status code map'i
- host pipeline'da kullanilan centralized exception middleware destegi
- status code bazli log seviyesi (4xx warning, 5xx error)
- `traceId` ve `timestampUtc` extension alanlariyla problem-details cevabi
- istemci tarafindan iptal edilen isteklerde (`OperationCanceledException`) `499` cevabi

## Ic Paket Bagimliliklari

- Zorunlu ic paket bagimliligi yoktur
- `Localization` ile birlikte kullanilirsa hata mesajlari daha zengin hale gelir

## External Gereksinimler

- Host pipeline'da exception middleware cagrisi
- Exception mapping politikasini host tarafinda belirlemek

## Register

```csharp
services.AddEskineriaExceptionHandler(options =>
{
    options.Map<ArgumentException>(400, "Bad Request", "BAD_REQUEST");
    options.Map<KeyNotFoundException>(404, "Not Found", "NOT_FOUND");
    options.IncludeExceptionDetails(); // sadece gerekli ortamlarda acin
});
```

## Kullanim Notu

Bu paket servis kaydini yapar. Middleware'in pipeline'a eklenmesi host tarafinda yapilmalidir.
