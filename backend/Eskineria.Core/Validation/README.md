# Validation Package

`Validation`, FluentValidation validator'larini merkezi kaydeder ve global MVC validation filter uygular.

## Neler Saglar

- `AddEskineriaValidation(assemblies)`
- `ValidationFilter`
- automatic validator discovery
- suppressed default model state filter
- DataAnnotations duplicate validation kapatma (`DisableDataAnnotationsValidation = true`)

## Ic Paket Bagimliliklari

- `Shared`: response formatlari ile birlikte kullanildiginda standart hata cikti verir
- `Localization`: validator mesajlari lokalize edilmek istendiginde onerilir

## External Gereksinimler

- FluentValidation validator assembly'leri
- MVC

## Register

```csharp
services.AddEskineriaValidation(new[]
{
    typeof(Program).Assembly,
    typeof(SomeCoreMarker).Assembly
});
```

## Kullanim Ornegi

Yeni bir validator sinifi eklemek yeterlidir. Paket assembly taramasi ile otomatik kaydeder.

```csharp
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
    }
}
```

## Guvenlik ve Performans Notlari

- `ValidationFilter` cikti boyutunu sinirlar (`max field` / `max error-per-field`) ve response'a `traceId` ekler.
- Validation hata mesaji listesinde duplicate metinler tekilleştirilir.
- `LocalizedContentValidator` culture key formatini dogrular ve kontrol karakteri iceren metinleri reddeder.
- `LocalizedContentValidator` icin culture key / metin uzunluk limitleri vardir.
