# Logging Package

`Logging`, correlation id, request-response logging ve opsiyonel Kafka sink entegrasyonunu saglar.

## Neler Saglar

- `AddEskineriaLogging(...)`
- `UseEskineriaLogging()`
- `CorrelationIdMiddleware`
- `RequestResponseLoggingMiddleware`
- Serilog ve opsiyonel Kafka sink konfigurasyonu

## Ic Paket Bagimliliklari

- `Security`: PII maskleme stratejileri ile birlikte kullanilabilir
- `Localization`: log mesajlari icin zorunlu degil

## External Gereksinimler

- Serilog konfigurasyonu
- Kafka sink kullanilacaksa broker bilgileri

## Register

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddEskineriaLogging();
```

```csharp
app.UseEskineriaLogging();
```

## Kullanim Notlari

- Hassas header'lar maske edilir
- `auth` path'lerinde request-response body loglamasi bilincli olarak daraltilmistir
- `Aparesk.EskineriaLogging` section'i ile body size, masked fields ve sink davranisi ayarlanir
- Header/query log uzunluklari sinirlanir (log sismesi ve log-forging riskini azaltir)
- Body loglari yalnizca text/json/xml tiplerinde tutulur; parse edilemeyen body ham haliyle yazilmaz
