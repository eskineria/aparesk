# Products Domain

Bu domain, urun yonetimi icin uc katmanli akis sunar:

- Domain: `Aparesk.Eskineria.Domain/Entities/Product.cs`
- Application: DTO + validator + service
- Persistence/WebApi: repository + EF config + controller

## Desteklenen Aksiyonlar

- Paged listeleme
- Id ile getirme
- SKU ile getirme
- Olusturma
- Guncelleme
- Stok arttirma/azaltma
- Aktif etme
- Pasif etme
- Arsivleme
- Arsivden geri alma
- Kalici silme (sadece arsivli urun)

## Endpointler

- `GET /api/v1/Products`
- `GET /api/v1/Products/{id}`
- `GET /api/v1/Products/by-sku/{sku}`
- `POST /api/v1/Products`
- `PUT /api/v1/Products/{id}`
- `POST /api/v1/Products/{id}/adjust-stock`
- `POST /api/v1/Products/{id}/activate`
- `POST /api/v1/Products/{id}/deactivate`
- `POST /api/v1/Products/{id}/archive`
- `POST /api/v1/Products/{id}/restore`
- `DELETE /api/v1/Products/{id}`
