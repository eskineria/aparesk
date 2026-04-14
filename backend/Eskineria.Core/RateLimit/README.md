# RateLimit Package

`RateLimit`, ASP.NET Core rate limiting altyapisini global limiter ve named policy ile birlestirir.

## Neler Saglar

- `AddEskineriaRateLimit(configuration)`
- `AddEskineriaRateLimit(options => ...)`
- global limiter
- policy bazli limiter
- reject response handler

## External Gereksinimler

- ASP.NET Core rate limiting middleware
- policy isimlerini endpoint'lerde kullanan controller veya minimal API

## Register

```csharp
services.AddEskineriaRateLimit(configuration);
```

```csharp
app.UseRateLimiter();
```

## Kullanim Ornegi

```csharp
[EnableRateLimiting("AuthLogin")]
public async Task<IActionResult> Login(LoginRequest request)
{
    return Ok();
}
```

## Ne Zaman Faydalidir

- auth endpoint'leri
- anonymous public endpoint'ler
- brute force ve flood senaryolari

## Guvenlik ve Performans Notlari

- Client identifier uretiminde ham `X-Forwarded-For` header'i kullanılmaz; spoof riski azaltilir.
- Identifier degerleri hash'lenir (partition key boyutu sabitlenir, PII azalir).
- Policy/global limit degerleri normalize edilir ve ust sinirlarla clamp edilir.
- 429 yanitlari `no-store` cache basliklari ve `traceId` ile doner.
