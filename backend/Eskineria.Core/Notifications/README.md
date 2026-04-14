# Notifications Package

`Notifications`, kanal bazli bildirim gonderimi, e-posta template yonetimi ve delivery log takibini tek bir modulde toplar.

## Neler Saglar

- `AddEskineriaNotifications()`
- `AddEmailChannel(configuration)`
- `INotificationService`
- `IEmailTemplateService`
- `IEmailDeliveryLogService`
- `EmailTemplatesController`
- `EmailDeliveryLogsController`

## Ic Paket Bagimliliklari

- `Shared`: response ve ortak modeller
- `Localization`: template ve servis mesajlari icin onerilir
- `Repository`: template ve delivery log repository'leri icin gerekir
- `Storage`: template tarafinda zorunlu degil ama attachment veya branding linkleri ile birlikte kullanilabilir
- `Auth`: controller permission korumalari icin gerekir

## External Gereksinimler

- SMTP bilgileri veya ozel bir `IEmailSender`
- Template persistence
- Delivery log persistence

## Register

```csharp
services.AddEskineriaNotifications();
services.AddEmailChannel(configuration);

services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
services.AddScoped<IEmailTemplateRevisionRepository, EmailTemplateRevisionRepository>();
services.AddScoped<IEmailDeliveryLogRepository, EmailDeliveryLogRepository>();
services.AddScoped<IEmailTemplateService, EmailTemplateService>();
services.AddScoped<IEmailDeliveryLogService, EmailDeliveryLogService>();
```

## Kullanim Ornegi

```csharp
await _notificationService.SendAsync(new NotificationMessage
{
    Recipient = "user@example.com",
    Title = "Welcome",
    Body = "Hello from Eskineria",
    Channel = NotificationChannel.Email
});
```

## Alt Paketler

- `Email`: sender, renderer, provider zinciri
- `Templates`: template CRUD, publish, rollback, validation
- `DeliveryLogs`: gonderim kayitlari ve izleme

## Guvenlik ve Performans Notlari

- E-posta adresleri gonderim oncesi format ve uzunluk kontrolunden gecirilir.
- Loglarda recipient bilgisi maskelenir (PII azaltimi).
- Subject/Error alanlari normalize ve sinirlandirilir (log injection / tasma riski azaltimi).
- Scriban template parse sonuclari cache'lenir (tekrarli render performansi).
