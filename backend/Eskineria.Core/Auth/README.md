# Auth Package

`Auth`, kimlik dogrulama ve yetkilendirme altyapisini tasir. Identity kurulumu, JWT uretimi, refresh token oturumu, permission policy provider, access control servisleri ve hazir API controller'lari bu pakettedir.

## Neler Saglar

- `AddEskineriaAuth<TContext>(...)`
- `AddEskineriaPermissionAuthorization()`
- `AddEskineriaAuthControllers()`
- `AuthController` ve `AccessControlController`
- `IAuthService`, `IAccountService`, `ITokenService`, `ICurrentUserService`, `IAccessControlService`

## Guvenlik Notlari

- JWT secret bos birakilamaz; startup asamasinda zorunludur.
- Non-development ortamda `JwtSettings:Secret` en az 32 byte olmalidir.
- JWT signing key olusturma ve dogrulama tarafinda UTF-8 kullanilir.
- Refresh tokenlar hashlenmis olarak saklanir; okuma tarafi hash-first, legacy plain fallback seklinde calisir.
- Request baglami (IP/User-Agent) `RequestContextInfoResolver` ile tek yerde normalize edilir.
- Kullanici alanlarinda uygulama ici sifreleme aktiflenebilir:
  - `AuthDataProtection:EncryptionKey` (Base64, 32-byte AES key)
  - `AuthDataProtection:PreviousEncryptionKeys` (opsiyonel key rotation listesi)
  - Bu anahtar tanimlandiginda `FirstName`, `LastName`, `ProfilePicture` DB'de sifreli saklanir.
  - Legacy plaintext kayitlar backward-compatible okunur.

## KVKK Kapsami Notu

- Identity altyapisi nedeniyle `Email/NormalizedEmail` gibi login-lookup alanlari sorgulanabilir kalmak zorundadir.
- Tam email alan sifreleme isteniyorsa deterministic hash + ayri encrypted value modeliyle ayri bir migration/refactor gerekir.

## Ic Paket Bagimliliklari

- `Shared`: response ve ortak key yapilari
- `Validation`: validator'lar assembly taramasi ile kaydolur
- `Localization`: localized auth mesajlari icin onerilir
- `Notifications`: email verification, password reset ve MFA kod gonderimi icin gerekir
- `Settings`: runtime auth ayarlari ve maintenance kararlari icin gerekir
- `Compliance`: register akisinda terms kabul entegrasyonu icin gerekir
- `Auditing`: role switch ve guvenlik aksiyonlarinin loglanmasi icin onerilir

## External Gereksinimler

- `EskineriaIdentityDbContext` uzerinden tureyen bir EF Core context
- `JwtSettings` section'i
- `UseAuthentication()` ve `UseAuthorization()` pipeline'i
- Full API deneyimi icin persistence implementasyonlari ve controller assembly kaydi

## Register

```csharp
services.AddEskineriaAuth<ApplicationDbContext>(
    configuration,
    typeof(Program).Assembly);

services.AddControllers()
    .AddEskineriaAuthControllers();
```

## Host Tarafi

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

## Kullanim Ornegi

```csharp
public class ProfileService
{
    private readonly ICurrentUserService _currentUser;

    public ProfileService(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public Guid? GetActorId() => _currentUser.UserId;
}
```

```csharp
[HasPermission("Settings", "Manage")]
public IActionResult SecureEndpoint() => Ok();
```

## Ne Zaman Tek Basina Yetmez

Sadece `AddEskineriaAuth` cagrisi login akisini ayaga kaldirir, ancak tam paket davranisi icin genelde su zincir de gerekir:

- notification provider
- system settings service
- compliance service
- role selection audit store
- email template ve localization altyapisi
