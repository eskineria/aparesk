# Aparesk.Eskineria Caching Module

Bu dokuman, `Aparesk.Eskineria.Core/Caching` modulu icin teknik referans niteligindedir. Amac, cache katmaninin nasil kaydedildigini, hangi modlarda nasil davrandigini ve uygulama tarafinda nasil kullanildigini tek yerde netlestirmektir.

## Paket Bagimliliklari

Ic paket bagimliliklari:

- Zorunlu bir `Core` modulu bagimliligi yoktur.
- `Security` ile birlikte kullanildiginda encryption ve hassas veri korumasi daha anlamli hale gelir.

External gereksinimler:

- `Memory` modu icin ek altyapi gerekmez.
- `Redis` ve `Hybrid` modlari icin Redis erisimi gerekir.
- Uygulama tarafinda `IConfiguration` veya `Action<CacheOptions>` ile konfigurasyon verilmelidir.

## Hizli Kurulum

```csharp
services.AddEskineriaCaching(configuration);
```

veya

```csharp
services.AddEskineriaCaching(options =>
{
    options.CacheType = CacheType.Hybrid;
    options.RedisConnectionString = "localhost:6379";
    options.KeyPrefix = "Aparesk.Eskineria:";
});
```

## Klasor Yapisi

- `Abstractions/ICacheService.cs`: Uygulamanin kullandigi ortak cache sozlesmesi.
- `Configuration/CacheOptions.cs`: Cache tipi ve davranis ayarlari.
- `Extensions/ServiceCollectionExtensions.cs`: DI kaydi (`AddEskineriaCaching`).
- `Implementations/MemoryCacheService.cs`: `IMemoryCache` tabanli in-memory cache.
- `Implementations/RedisCacheService.cs`: Redis tabanli distributed cache.
- `Implementations/HybridCacheService.cs`: Redis-first, in-memory fallback cache.
- `Security/CacheEncryptionProvider.cs`: Icerik icin opsiyonel AES-256 encryption destegi.

## Mimari Ozet

Caching modulu uygulamaya tek bir abstraction sunar:

- `GetAsync<T>`
- `SetAsync<T>`
- `RemoveAsync`
- `RemoveByPrefixAsync`
- `ExistsAsync`

Uygulama kodu dogrudan `IMemoryCache` veya Redis kullanmaz; bunun yerine `ICacheService` kullanir. Hangi implementasyonun aktif olacagi runtime'da `Caching:CacheType` ile belirlenir.

## Register Bilgisi

Kayit noktasi:

- `Aparesk.Eskineria.Application/ServiceCollectionExtensions.cs` icinde `services.AddEskineriaCaching(configuration)` cagrilir.
- Bu cagri, varsayilan olarak `Caching` section'ini bind eder.

DI secimi:

- `CacheType = Memory` ise `ICacheService -> MemoryCacheService`
- `CacheType = Redis` ise `ICacheService -> RedisCacheService`
- `CacheType = Hybrid` ise `ICacheService -> HybridCacheService`

Not:

- `Memory` ve `Hybrid` modlarinda `AddMemoryCache()` ile framework `IMemoryCache` servisi kaydedilir.
- `Redis` ve `Hybrid` modlarinda `IConnectionMultiplexer` kaydedilir.

## Cache Modlari

### 1) Memory

Davranis:

- Cache sadece uygulama process memory'sinde tutulur.
- `MemoryCacheService`, ASP.NET Core `IMemoryCache` kullanir.
- Single instance ve hizli local cache senaryolari icin uygundur.

Avantaj:

- Redis bagimliligi yoktur.
- En dusuk latency bu moddadir.

Sinir:

- Multi-instance ortamda node'lar arasi veri paylasimi yoktur.
- Uygulama restart olunca cache temizlenir.

### 2) Redis

Davranis:

- Tum `get/set/remove` operasyonlari Redis uzerinden calisir.
- Distributed deployment icin uygundur.

Avantaj:

- Node'lar arasi ortak cache saglanir.

Sinir:

- Redis erisimi yoksa cache operasyonlari hata verir.
- Bu modda fallback mantigi yoktur; saf Redis davranisi vardir.

### 3) Hybrid

Davranis:

- Primary cache Redis'tir.
- `GetAsync` oncelikle Redis'e gider.
- Redis'ten basarili veri gelirse sonuc kisa sureli olarak in-memory katmana da yazilir.
- Redis hata verirse is kurali tarafini dusurmemek icin in-memory fallback devreye girer.
- Redis tekrar ulasilabilir oldugunda once bekleyen Redis operasyonlari replay edilir, sonra tekrar Redis primary olarak devam edilir.

Bu modun amaci:

- Normal durumda distributed cache davranisini korumak
- Redis outage durumunda business akislarini tamamen durdurmamak

Onemli not:

- Redis kapaliyken yapilan bekleyen `set/remove/remove-prefix` operasyonlari process memory'sinde kuyruklanir.
- Uygulama bu sirada restart olursa bu bekleyen operasyonlar kaybolur.
- Yani bu mekanizma "availability first" bir failover'dur; tam durability garantisi vermez.

## CacheOptions Referansi

`CacheOptions` alanlari:

- `CacheType`: `Memory | Redis | Hybrid`
- `RedisConnectionString`: Redis baglanti cumlesi
- `HybridL1TtlSeconds`: Hybrid modda memory katmaninin TTL suresi
- `HybridRedisRetryDelaySeconds`: Redis hata aldiktan sonra tekrar deneme araligi
- `HybridMaxPendingOperations`: Redis down iken memory'de tutulacak maksimum bekleyen operasyon sayisi
- `MaxKeyLength`: Tek bir cache key/prefix icin izin verilen maksimum karakter sayisi
- `RedisRemoveByPrefixBatchSize`: Redis prefix silme operasyonunda toplu silme batch boyutu
- `KeyPrefix`: Tum key'lerin basina eklenecek zorunlu prefix
- `EncryptionKey`: Opsiyonel AES-256 base64 key
- `PreviousEncryptionKeys`: Key rotation icin eski key listesi

Validation kurallari:

- `KeyPrefix` bos olamaz.
- `RedisConnectionString`, `Redis` ve `Hybrid` modlarinda zorunludur.
- `HybridL1TtlSeconds` `1..3600` araligina normalize edilir.
- `HybridRedisRetryDelaySeconds` `1..300` araligina normalize edilir.
- `HybridMaxPendingOperations` `100..50000` araligina normalize edilir.
- `MaxKeyLength` `32..1024` araligina normalize edilir.
- `RedisRemoveByPrefixBatchSize` `100..5000` araligina normalize edilir.
- Encryption key varsa base64 olmali ve 32 byte decode edilmelidir.

## appsettings Ornegi

```json
{
  "Caching": {
    "CacheType": "Hybrid",
    "RedisConnectionString": "localhost:6379",
    "HybridL1TtlSeconds": 60,
    "HybridRedisRetryDelaySeconds": 5,
    "HybridMaxPendingOperations": 5000,
    "MaxKeyLength": 256,
    "RedisRemoveByPrefixBatchSize": 1000,
    "KeyPrefix": "Aparesk.Eskineria:",
    "EncryptionKey": null,
    "PreviousEncryptionKeys": []
  }
}
```

Ornek mod secimleri:

- Sadece memory icin `CacheType: "Memory"`
- Sadece redis icin `CacheType: "Redis"`
- Redis-first fallback icin `CacheType: "Hybrid"`

## Kullanimi

Servis icine `ICacheService` inject edilir:

```csharp
public class ExampleService
{
    private readonly ICacheService _cacheService;

    public ExampleService(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }
}
```

Temel ornek:

```csharp
var cacheKey = "users:42:profile";

var cached = await _cacheService.GetAsync<UserDto>(cacheKey);
if (cached is not null)
{
    return cached;
}

var user = await LoadUserAsync();
await _cacheService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(10));
return user;
```

Silme ornekleri:

```csharp
await _cacheService.RemoveAsync("users:42:profile");
await _cacheService.RemoveByPrefixAsync("users:42:");
```

## Key Tasarimi

Onerilen yaklasim:

- Domain bazli prefix kullanin: `users:`, `files:`, `settings:`
- Entity id'lerini acik yazin: `users:42:profile`
- Prefix temizligi icin hiyerarsik key tasarlayin

Ornekler:

- `users:42:profile`
- `files:tenant-3:folder-10:list`
- `system-settings:tenant-1`

Not:

- `CacheOptions.KeyPrefix` uygulama seviyesinde otomatik eklenir.
- Uygulama kodunda tekrar global prefix eklemeyin.
- Key ve prefix parametreleri bos olamaz, `MaxKeyLength` limitini asamaz.
- Prefix temizliginde wildcard karakterleri (`*`, `?`, `[`, `]`) kabul edilmez.

## Encryption Davranisi

`EncryptionKey` verilirse:

- Cache'e yazilan JSON icerigi encrypt edilir.
- Read sirasinda decrypt edilip deserialize edilir.

Amac:

- Redis veya memory dump gibi ortamlarda plain-text veri riskini azaltmak

Not:

- Encryption key rotation icin `PreviousEncryptionKeys` kullanilabilir.
- Yanlis veya bilinmeyen key ile okunamayan degerler `default` doner.

## Bilinen Davranislar ve Dikkat Noktalari

- `SetAsync` null degerleri cache'e yazmaz.
- `RemoveByPrefixAsync`, prefix eslesen tum key'leri silmeye calisir.
- `Redis.RemoveByPrefixAsync` key'leri memory'ye toplu yuklemez; stream + batch delete kullanir.
- `Memory` mod process-local calisir; instance'lar arasi ortak degildir.
- `Hybrid` modda read yolu Redis-first'tur, memory sadece hizlandirici ve fallback katmanidir.
- `Redis` modda fallback yoktur; Redis yoksa cache operasyonu da yoktur.
- `Hybrid` modda Redis down iken bekleyen operasyon kuyrugu limit dolarsa en eski operasyon dusurulur ve warning log basilir.
- `Hybrid` fallback `SetAsync` yolunda L1 memory TTL, `HybridL1TtlSeconds` kuraliyla sinirlanir.

## Hata Giderme

- `CacheOptions.KeyPrefix is required`: `Caching:KeyPrefix` bos birakilmistir.
- `RedisConnectionString is required`: `CacheType` `Redis` veya `Hybrid` iken connection string yoktur.
- Redis timeout/connection hatalari: `Hybrid` modda fallback devreye girer, `Redis` modda exception beklenir.
- Beklenmedik stale veri: Hybrid modda recovery sonrasi replay edilen operasyonlara ve TTL davranisina bakilmalidir.

## Gelistirme Icin Onerilen Test Senaryolari

1. `Memory` modda basic `get/set/remove` smoke testi.
2. `Redis` modda distributed cache smoke testi.
3. `Hybrid` modda Redis up iken Redis-first davranisinin testi.
4. Redis down iken `Hybrid` fallback davranisinin testi.
5. Redis geri geldiginde replay ve recovery davranisinin testi.
6. `RemoveByPrefixAsync` ile toplu invalidation testi.
7. Encryption acik iken write/read uyumlulugu testi.
