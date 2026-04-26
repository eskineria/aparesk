# Aparesk.Eskineria Storage Module

Bu dokuman, `Aparesk.Eskineria.Core/Storage` modulu icin teknik referans niteligindedir. Amac, depolama katmaninin nasil calistigini, nasil konfigure edildigini ve uygulama katmaninda nasil kullanildigini tek yerde netlestirmektir.

## Paket Bagimliliklari

Ic paket bagimliliklari:

- `Settings` paketi branding upload gibi senaryolarda bu modulu kullanir.
- `Notifications` attachment veya template asset senaryolarinda bu modulle birlikte calisabilir.

External gereksinimler:

- `Local` modunda host dosya sistemi ve static file stratejisi
- `S3` modunda bucket ve credential bilgileri
- `AzureBlob` modunda connection string ve container bilgileri

## Hizli Kurulum

```csharp
services.AddAparesk.EskineriaStorage(options =>
{
    options.ProviderType = StorageProviderType.Local;
    options.Local.RootPath = "wwwroot/uploads";
    options.Local.BaseUrl = "/uploads";
});
```

## Klasor Yapisi

- `Abstractions/IStorageService.cs`: Tum provider'larin uymasi gereken ortak depolama sozlesmesi.
- `Configuration/StorageOptions.cs`: Provider secimi ve provider bazli ayarlar.
- `Extensions/ServiceCollectionExtensions.cs`: DI kaydi (`AddAparesk.EskineriaStorage`).
- `Security/FileSecurityProvider.cs`: Dosya/folder sanitization + boyut/uzanti + magic-byte dogrulamasi.
- `Implementations/EnhancedLocalStorageService.cs`: Varsayilan local provider.
- `Implementations/S3StorageService.cs`: AWS S3 / S3-compatible provider.
- `Implementations/AzureBlobStorageService.cs`: Azure Blob provider.
- `Implementations/LocalStorageService.cs`: Local providerin alternatif implementasyonu (projede DI tarafinda varsayilan degil).

## Mimari Ozet

Storage modulu, `IStorageService` arayuzu uzerinden provider bagimsiz bir API sunar:

- `UploadAsync`
- `DeleteAsync`
- `DownloadAsync`
- `GetFileUrl`
- `CreateFolderAsync`
- `DeleteFolderAsync`

Provider secimi runtime'da `StorageOptions.ProviderType` ile yapilir. Tum provider'lar upload oncesi `FileSecurityProvider` ile guvenlik kontrollerini uygular.

## DI Kaydi ve Provider Secimi

Kayit noktasi:

- `Aparesk.Eskineria.Application/ServiceCollectionExtensions.cs` icinde `services.AddAparesk.EskineriaStorage(...)` cagrilir.
- Konfigurasyon `Storage` section'undan bind edilir.
- Ayrica `Storage:Provider` degeri manuel parse edilerek `ProviderType` belirlenir.

Secim mantigi (`StorageProviderType`):

- `Local` -> `EnhancedLocalStorageService`
- `S3` -> `S3StorageService`
- `AzureBlob` -> `AzureBlobStorageService`

Not:

- `LocalStorageService` kodda mevcut olsa da DI seciminde varsayilan olarak `EnhancedLocalStorageService` kullanilir.

## StorageOptions Referansi

`StorageOptions` alanlari:

- `ProviderType`: `Local | S3 | AzureBlob`
- `Local.RootPath`: Fiziksel dosya yolu (ornek: `wwwroot/uploads`)
- `Local.BaseUrl`: Public URL prefix (ornek: `http://localhost:5285/uploads` veya `/uploads`)
- `S3.BucketName`, `S3.Region`, `S3.AccessKey`, `S3.SecretKey`, `S3.ServiceUrl`
- `AzureBlob.ConnectionString`, `AzureBlob.ContainerName`, `AzureBlob.BaseUrl`
- `Security.AllowedExtensions`, `Security.MaxFileSizeBytes`

Normalize edilen noktalar:

1. `AllowedExtensions` bos degilse basina `.` eklenir (yoksa).
2. Tum extension degerleri kucuk harfe cevrilir.
3. Duplicate degerler temizlenir.
4. `MaxFileSizeBytes <= 0` ise varsayilan `5MB` atanir.
5. `MaxFileSizeBytes` konfigurasyonu `512MB` ustune cikarsa hard-cap ile sinirlanir.
6. `Local.BaseUrl` bos ise `/uploads` olur; relatif verilirse `/` ile normalize edilir.

## Guvenlik Akisi (FileSecurityProvider)

Upload akisinda su adimlar vardir:

1. `ValidateFile(fileName, length)`
2. `ValidateFileContent(stream, fileName)`
3. Dosya adi/folder sanitize
4. Provider'a yazma

### 1) Dosya adi guvenligi

- `NormalizeStoredFileName`: Sadece dosya segmentini alir (`Path.GetFileName`), gecersiz karakterleri `_` yapar, `.` `/` `\` ile baslama durumlarini temizler ve isim bos kalirsa hata firlatir.
- `SanitizeFileName`: `NormalizeStoredFileName` sonucu uzerine `_<GUID>` ekler. Amac overwrite onleme ve tahmin edilmesi zor dosya isimleri uretmektir.

### 2) Folder guvenligi

- `SanitizeFolderName`: Gecersiz path karakterlerini temizler, `..` parcalarini ve path traversal risklerini temizler, `\` -> `/` normalize eder, cift slash'lari sadelestirir.

### 3) Boyut ve uzanti kontrolu

- Dosya boyutu `Security.MaxFileSizeBytes` ile dogrulanir.
- Uzanti `Security.AllowedExtensions` listesinde degilse hata.
- `AllowedExtensions` bos birakilirsa uzanti kisiti devre disi kalir (onerilmez).

### 4) Magic-byte (icerik imzasi) kontrolu

Desteklenen kontroller:

- Resim: `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`
- Diger: `.ico`, `.pdf`, `.zip`, `.7z`, `.rar`
- Office Open XML: `.docx`, `.xlsx`, `.pptx` (ZIP tabanli)
- `.txt`: gecerli kabul edilir (magic-byte zorunlu degil)

Notlar:

- Ilk 12 byte okunur ve stream pozisyonu geri sarilir.
- Uzanti-icerik uyusmazsa upload engellenir.
- Resim uzantilari icin "image-is-image" davranisi var; image uzantisi ile gelen dosya gecerli herhangi bir image imzasina sahipse kabul edilir.

## Provider Davranislari

### 1) EnhancedLocalStorageService (Varsayilan Local)

Temel davranis:

- Fiziksel path: `_env.ContentRootPath + Local.RootPath`
- Upload donus degeri: relative path (`folder/file_guid.ext`)
- URL uretimi: `Local.BaseUrl + encoded relative path`
- Download/Delete tarafinda `folder` bos verilirse `fileName` icindeki path'ten folder ayiklanabilir.

Folder islemleri:

- `CreateFolderAsync`: fiziksel klasor olusturur.
- `DeleteFolderAsync`: klasoru recursive siler.

Operasyon notu:

- Uygulamada `app.UseStaticFiles()` aktif oldugu icin `wwwroot` altindaki local dosyalar HTTP ile servis edilir.
- `Local.RootPath` degerini static file stratejisiyle uyumlu secmek gerekir.
- Upload stream'i seekable degilse guvenlik dogrulamalari nedeniyle islem reddedilir.
- Local path olusturma sirasinda final path'in storage root disina cikmasi engellenir.
- `DeleteFolderAsync` bos klasor adi ile cagrilirsa tum root'u silmeyi engellemek icin hata firlatilir.

### 2) S3StorageService

Temel davranis:

- Obje key formati: `folder/file_guid.ext`
- Upload: `TransferUtility` ile bucket'a yazilir.
- URL: `ServiceUrl` varsa `{ServiceUrl}/{Bucket}/{key}`, region yoksa `https://{Bucket}.s3.amazonaws.com/{key}`, region varsa `https://{Bucket}.s3.{Region}.amazonaws.com/{key}`.

Folder islemleri:

- `CreateFolderAsync`: trailing `/` ile bos object olusturur.
- `DeleteFolderAsync`: prefix ile listeleyip toplu siler (pagination/continuation ile).

Konfigurasyon:

- `BucketName` zorunlu.
- `AccessKey/SecretKey` verilmezse default AWS credential chain kullanilir.
- `ServiceUrl` MinIO gibi S3-compatible altyapilar icin desteklenir (`ForcePathStyle = true`).

### 3) AzureBlobStorageService

Temel davranis:

- Blob path: `folder/file_guid.ext`
- Constructor sirasinda container `CreateIfNotExists(PublicAccessType.None)` ile acilir.
- URL: `AzureBlob.BaseUrl` varsa bu kullanilir, yoksa blob client URI doner.

Connection bilgisi fallback sirasi:

1. `Storage:AzureBlob:ConnectionString`
2. `ConnectionStrings:AzureBlobStorage`

Container fallback sirasi:

1. `Storage:AzureBlob:ContainerName`
2. `Storage:ContainerName`
3. varsayilan: `uploads`

Folder islemleri:

- `CreateFolderAsync`: no-op (Azure'da folder virtual).
- `DeleteFolderAsync`: prefix ile bloblari tek tek siler.

## appsettings Ornegi

Asagidaki ornek, mevcut proje yapisiyla uyumludur:

```json
{
  "Storage": {
    "Provider": "Local",
    "Local": {
      "RootPath": "wwwroot/uploads",
      "BaseUrl": "http://localhost:5285/uploads"
    },
    "Security": {
      "MaxFileSizeBytes": 10485760,
      "AllowedExtensions": [".jpg", ".jpeg", ".png", ".pdf", ".txt", ".docx", ".zip", ".7z", ".rar"]
    },
    "S3": {
      "BucketName": "",
      "Region": "",
      "AccessKey": "",
      "SecretKey": "",
      "ServiceUrl": ""
    },
    "AzureBlob": {
      "ConnectionString": "",
      "ContainerName": "uploads",
      "BaseUrl": ""
    }
  }
}
```

Not:

- Runtime secim `Storage:Provider` uzerinden yapilir (`Local`, `S3`, `AzureBlob`).

## Uygulama Katmaninda Kullanim

Dosya yukleme/indirme akislari icinde `IStorageService` kullanimi:

- Upload: `UploadAsync(stream, seoFileName, physicalFolder)` cagrilir ve donen path DB'ye kaydedilir.
- Download: `DownloadAsync(fileName, physicalFolder)` cagrilir.
- Public URL: `GetFileUrl(fileName, physicalFolder)` cagrilir.

Bu sayede uygulama, provider degisse bile ayni servis koduyla calisir.

## Bilinen Davranislar ve Dikkat Noktalari

- Upload edilen dosya adi her zaman GUID suffix alir; orijinal isim korunmaz.
- Download/Delete/GetFileUrl cagrilarinda `folder` verilmezse ve `fileName` path iceriyorsa folder ayristirilir.
- Azure/S3 tarafinda "folder" kavrami object prefix'tir; fiziksel dizin degildir.
- Local providerda `RootPath` uygulamanin content root'una gore resolve edilir.
- Storage Security limitleri disinda ek olarak uygulama seviyesinde request body ve dosya boyutu limitleri uygulanabilir.

## Hata Giderme

- `Unsupported storage provider`: `Storage:Provider` degeri `Local|S3|AzureBlob` disinda.
- `S3 bucket name is not configured`: `Storage:S3:BucketName` bos.
- `Azure Blob Storage connection string is not configured`: `Storage:AzureBlob:ConnectionString` ve `ConnectionStrings:AzureBlobStorage` bos.
- Local dosyalar URL'den acilmiyor: `RootPath` degeri `wwwroot` altinda degil veya `BaseUrl` yanlis; reverse proxy/host ayarlari nedeniyle URL farkli bir origin'e gidiyor olabilir.

## Gelistirme Icin Onerilen Test Senaryolari

1. Her provider icin upload/download/delete smoke testi.
2. Path traversal denemeleri (`../`, `..\\`, gizli dosya isimleri) ile sanitization testi.
3. Uzanti-icerik uyusmazligi testi (ornek: `.jpg` uzantili exe/pseudo dosya).
4. Buyuk dosyada `MaxFileSizeBytes` ve request-body limit davranisi.
5. URL encode testi (bosluk, Turkce karakter, ozel karakter iceren isimler).
