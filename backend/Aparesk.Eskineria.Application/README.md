# Aparesk.Eskineria.Application

Bu katman, is akislarini domain-bazli moduller halinde toplar.

## Yapilanma

- `ServiceCollectionExtensions.cs`
  - Uygulama katmani DI kayitlarinin tek giris noktasi.
- `Features/Products/*`
  - Product domain aksiyonlari (DTO, validator, service, abstraction).
- `Utilities/StringExtensions.cs`
  - Ortak string yardimcilari.

## Calisma Sekli

`ServiceCollectionExtensions.AddApplicationServices(...)` icerisinde
platform, notification, storage ve product kayitlari acik sekilde yapilir.
