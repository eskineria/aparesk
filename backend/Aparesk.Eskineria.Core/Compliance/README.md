# Compliance Package

`Compliance`, terms of service, privacy policy ve benzeri metinlerin versiyonlanmasi ve kullanici kabul kayitlarinin tutulmasi icin kullanilir.

## Neler Saglar

- `IComplianceService`
- `ComplianceController`
- `TermsAndConditions` ve kullanici acceptance entity'leri
- `ComplianceSeedService`
- aktif/yururlukte olmayan metinlerin kabul edilmesini engelleyen acceptance kurali
- acceptance tarafinda istemciden gelen IP/User-Agent degerlerine guvenmek yerine sunucu request context kaynakli kayit
- pending required terms sorgusunda toplu sorgu ile daha dusuk DB round-trip

## Ic Paket Bagimliliklari

- `Shared`: response modelleri
- `Auth`: kullanici kimligi ve register/login akislariyla baglanmak icin gerekir
- `Localization`: compliance metinlerinin lokalize sunumu icin onerilir
- `Repository`: repository abstraction ve specification kullanimi icin gerekir

## External Gereksinimler

- `ITermsRepository`
- `IUserTermsAcceptanceRepository`
- EF Core veya baska bir persistence katmani

## Register

Bu paket icin su an tek bir `AddAparesk.EskineriaCompliance()` extension'i yok. Servis ve repository'ler explicit kaydedilir:

```csharp
services.AddScoped<ITermsRepository, TermsRepository>();
services.AddScoped<IUserTermsAcceptanceRepository, UserTermsAcceptanceRepository>();
services.AddScoped<IComplianceService, ComplianceService>();
services.AddScoped<ComplianceSeedService>();
```

## Kullanim Ornegi

```csharp
var activeTerms = await _complianceService.GetActiveTermsByTypeAsync("PrivacyPolicy");
```

```csharp
var accepted = await _complianceService.AcceptTermsAsync(
    userId,
    termsId,
    requestIp,
    userAgent);
```

## Ne Zaman Gerekir

- register sirasinda zorunlu kabul toplamak
- kullanicinin guncel terms'i kabul edip etmedigini kontrol etmek
- degisen yasal metinleri versiyonlayarak saklamak
