# Aparesk Site Yonetim Uygulamasi Proje Plani

## 1. Urun Vizyonu

Aparesk, apartman, site, rezidans ve toplu yasam alanlarinin operasyonel, finansal, hukuki ve iletisim sureclerini tek merkezden yoneten uretim seviyesi bir site yonetim platformu olacaktir. Ilk surum web uygulamasi olarak gelistirilecek, web surumu olgunlastiktan sonra mobil uygulama kapsami yeniden degerlendirilecektir.

Hedef, sadece aidat ve daire takip eden basit bir uygulama degil; yonetici, denetci, malik, kiraci, gorevli, muhasebe ve tedarikci taraflarinin gunluk ihtiyaclarini karsilayan, denetlenebilir, guvenli, raporlanabilir ve buyuyebilir bir platform kurmaktir.

## 2. Temel Ilkeler

- MVP yaklasimi uygulanmayacak; ilk canli surum uretimde kullanilabilecek kalite ve kapsama sahip olacak.
- Web uygulamasi once tamamlanacak; mobil uygulama webde dogrulanan is akislarina gore planlanacak.
- Her kritik islem denetlenebilir olacak: kim, ne zaman, hangi veriyi degistirdi bilgisi tutulacak.
- Finansal islerde tutarlilik, izlenebilirlik ve geri alinabilirlik ana kriter olacak.
- Yetkilendirme rol bazli ve site bazli olacak; bir kullanici birden fazla sitede farkli rollere sahip olabilecek.
- Veri modeli coklu site/mulk yapisini destekleyecek.
- Uygulama ileride e-fatura, banka, sanal POS, muhasebe ve bildirim entegrasyonlarina acik tasarlanacak.

## 3. Mevcut Altyapiyi Yeniden Yapmama Karari

Repo icinde kimlik, rol/izin, profil, e-posta, bildirim, audit, localization, cache, rate limit, exception, validation, compliance, storage ve genel sistem ayarlari icin hazir altyapi bulunuyor. Bu proje planinda bu parcalar sifirdan yeniden gelistirilecek isler olarak ele alinmayacak.

Bu alanlarda hedef:

- Mevcut auth, rol ve izin altyapisini site yonetimi domainine baglamak
- Site, blok, daire ve gorev baglaminda ek yetki kurallari tanimlamak
- Mevcut profil ve kullanici yonetimini malik, kiraci, sakin ve personel kayitlariyla iliskilendirmek
- Mevcut e-posta/bildirim altyapisini aidat, duyuru, toplanti ve talep senaryolarinda kullanmak
- Mevcut audit altyapisini finans, denetim, karar ve belge islemlerinde dogru sekilde tetiklemek
- Mevcut localization ve compliance kabiliyetlerini yeni ekran ve is akislari icin genisletmek

Yeni gelistirme odagi platform altyapisi degil; site yonetimi domain modeli, is kurallari, ekranlar, raporlar ve entegrasyon noktalaridir.

## 4. Hedef Kullanici Rolleri

- Sistem sahibi / platform yoneticisi
- Site yoneticisi
- Yonetim kurulu uyesi
- Denetci / denetim kurulu uyesi
- Blok temsilcisi
- Malik
- Kiraci
- Daire sakini / hane uyesi
- Muhasebe sorumlusu
- Guvenlik / danisma personeli
- Teknik personel / gorevli
- Tedarikci / hizmet firmasi
- Misafir veya gecici erisim kullanicisi

## 5. Ana Moduller

### 5.1 Kimlik ve Yetki Entegrasyonu

- Mevcut kullanici, rol, izin, profil ve oturum altyapisi kullanilacak.
- Site, blok, daire ve gorev bazli ek erisim kurallari domain seviyesinde tanimlanacak.
- Bir kullanicinin birden fazla sitede farkli rol ve yetkilere sahip olmasi desteklenecek.
- Malik, kiraci, hane uyesi, personel ve tedarikci kayitlari mevcut kullanici profiliyle iliskilendirilecek.
- Kritik site yonetimi islemleri mevcut audit ve yetkilendirme mekanizmasina baglanacak.
- Davet akisi varsa mevcut sistemle kullanilacak; eksik kalan site/dairenin davete baglanmasi eklenecek.
- Yetki gecmisi ve rol degisiklikleri mevcut log altyapisiyla raporlanabilir hale getirilecek.

### 5.2 Site, Blok ve Daire Yonetimi

- Site tanimi
- Blok tanimlari
- Kat, daire, depo, otopark, ticari alan gibi bagimsiz bolum tipleri
- Daire numarasi, arsa payi, metrekare, kullanim tipi
- Malik, kiraci ve hane sakini iliskileri
- Gecmis sakin ve malik kayitlari
- Tasinma giris/cikis surecleri
- Daire bazli borc, odeme, bildirim ve talep gecmisi
- Otopark, depo ve ortak alan kullanim haklari

### 5.3 Site Sakini ve Iletisim Yonetimi

- Malik, kiraci, aile bireyi ve yetkili kisi kayitlari
- Acil durum kisi bilgileri
- KVKK acik riza ve iletisim izinleri
- Telefon, e-posta, adres ve meslek bilgileri
- Kisi-daire iliski tarihcesi
- Toplu kisi ice aktarma
- Etiketleme ve segmentasyon
- Kara liste / kisitli erisim bilgileri

### 5.4 Gelir, Gider ve Muhasebe

- Gelir kalemleri
- Gider kalemleri
- Kasa ve banka hesaplari
- Cari hesap yapisi
- Tedarikci hesaplari
- Fatura, fis, makbuz ve dekont kayitlari
- Odeme planlari
- Gelir/gider kategori agaci
- Donemsel muhasebe kapama
- Butce planlama ve gerceklesen karsilastirmasi
- Borc/alacak mutabakatlari
- Finansal belge yukleme
- Kasa-banka virman islemleri
- Muhasebe fisleri
- Mali raporlar ve denetim ciktisi

### 5.5 Aidat ve Tahakkuk Yonetimi

- Aidat tarifeleri
- Daire, metrekare, arsa payi veya ozel kural bazli aidat hesaplama
- Donemsel tahakkuk olusturma
- Gecikme faizi ve ceza kurallari
- Tek seferlik ek odeme kalemleri
- Daire bazli indirim, muafiyet veya ozel durum
- Toplu borclandirma
- Tahakkuk onay akisi
- Geri alma ve duzeltme fisleri
- Borc bildirimleri
- Odeme takibi
- Makbuz olusturma
- Banka ekstresi ile odeme eslestirme icin hazir mimari

### 5.6 Demirbas ve Varlik Yonetimi

- Demirbas kayitlari
- Kategori, marka, model, seri numarasi
- Satin alma tarihi, bedeli ve garanti bilgileri
- Zimmet ve lokasyon takibi
- Bakim planlari
- Ariza gecmisi
- Fotograf ve belge ekleri
- Amortisman bilgileri icin hazir alanlar
- Hurdaya ayirma ve pasife alma
- Demirbas sayim listeleri

### 5.7 Bakim, Ariza ve Talep Yonetimi

- Site sakini talep olusturma
- Ariza, sikayet, oneriler ve hizmet talepleri
- Oncelik, durum, kategori ve atanan kisi
- SLA / hedef cozum sureleri
- Fotograf ve dosya ekleme
- Yorum ve islem gecmisi
- Gorevli veya tedarikci atama
- Talep kapanis onayi
- Tekrarlayan bakim gorevleri
- Periyodik bakim takvimi
- Talep performans raporlari

### 5.8 Yonetici Secimi ve Genel Kurul

- Genel kurul toplanti tanimi
- Gunden maddeleri
- Hazirun listesi
- Vekaletname takibi
- Katilim yeter sayisi kontrolu
- Oylama ve karar kayitlari
- Yonetici ve denetci secimi
- Karar defteri kayitlari
- Toplanti tutanaklari
- Imza listesi ciktisi
- Karar ekleri ve dosya arsivi
- Gelecek toplantilar icin karar takip maddeleri

### 5.9 Hazirun Listesi

- Malik ve temsilci listesi
- Daire ve arsa payi bilgisi
- Katilimci imza alani
- Vekaleten katilim bilgisi
- Katilim durumu
- Toplam pay ve kisi sayisi hesaplari
- PDF/Excel cikti
- Toplanti bazli arsivleme

### 5.10 Denetim Islemleri

- Denetim donemi tanimi
- Denetim checklistleri
- Finansal belge inceleme
- Gelir/gider mutabakatlari
- Karar uygulama kontrolleri
- Eksiklik ve bulgu kayitlari
- Denetim raporu olusturma
- Denetci yorumlari
- Bulgular icin aksiyon planlari
- Denetim kapanis onayi
- Degistirilemez denetim arsivi

### 5.11 Duyuru, Bildirim ve Iletisim

- Site geneli, blok bazli veya daire bazli duyurular
- E-posta bildirimi
- SMS/push bildirimi icin hazir kanal mimarisi
- Bildirim okundu bilgisi
- Zorunlu okuma/onay gerektiren duyurular
- Anketler
- Sakinlerden geri bildirim toplama
- Sablonlu mesajlar
- Iletisim gecmisi

### 5.12 Belge ve Arsiv Yonetimi

- Site belgeleri
- Sozlesmeler
- Faturalar
- Karar defteri ekleri
- Denetim raporlari
- Demirbas belgeleri
- Daireye ozel belgeler
- Dosya versiyonlama
- Yetkiye gore belge erisimi
- Belge son kullanma / yenileme hatirlatmalari

### 5.13 Personel, Tedarikci ve Sozlesme Yonetimi

- Personel kayitlari
- Gorev ve vardiya bilgisi
- Tedarikci firma kayitlari
- Hizmet sozlesmeleri
- Sozlesme bitis hatirlatmalari
- Tedarikci performans notlari
- Hak edis ve odeme takibi
- Personel gorev atamalari

### 5.14 Ortak Alan ve Rezervasyon Yonetimi

- Ortak alan tanimlari
- Rezervasyon takvimi
- Kullanim kurallari
- Ucretli/ucretsiz rezervasyon
- Onay sureci
- Iptal kurallari
- Kullanim gecmisi

### 5.15 Guvenlik ve Ziyaretci Yonetimi

- Ziyaretci kaydi
- Daireye ziyaretci bildirme
- Arac plaka kaydi
- Gecici giris izni
- Guvenlik notlari
- Teslimat kayitlari
- Kara liste / riskli ziyaretci uyarilari

### 5.16 Raporlama ve Analitik

- Finansal ozet paneli
- Borclu daireler raporu
- Tahsilat performansi
- Gelir/gider dagilimi
- Aidat tahakkuk/tahsilat karsilastirmasi
- Gider kategori analizi
- Demirbas durumu
- Talep ve ariza performansi
- Genel kurul ve karar takip raporlari
- Denetim bulgu raporlari
- Excel/PDF disari aktarma

### 5.17 Sistem Yonetimi

- Parametre yonetimi
- Dil ve yerellestirme
- E-posta sablonlari
- Bildirim sablonlari
- Denetim loglari
- Sistem saglik kontrolleri
- Yedekleme ve geri yukleme stratejisi
- Entegrasyon ayarlari

## 6. Web Uygulamasi Ekran Haritasi

### 6.1 Ortak Ekranlar

- Giriş
- Sifre yenileme
- Davet kabul
- Profil
- Bildirim merkezi
- Yardim ve destek
- Yetkisiz erisim
- Bakim modu

### 6.2 Yonetim Paneli

- Ana dashboard
- Finans ozeti
- Geciken odemeler
- Son talepler
- Yaklasan toplantilar
- Kritik bildirimler
- Hızlı islemler

### 6.3 Tanimlar

- Siteler
- Bloklar
- Daireler
- Kisiler
- Roller
- Kategoriler
- Banka/kasa hesaplari
- Ortak alanlar
- Demirbas kategorileri

### 6.4 Finans

- Aidat tarifeleri
- Tahakkuklar
- Borclar
- Tahsilatlar
- Gelirler
- Giderler
- Kasa/banka hareketleri
- Tedarikci carileri
- Donem kapama
- Finansal raporlar

### 6.5 Operasyon

- Talepler
- Bakim planlari
- Demirbaslar
- Personel
- Tedarikciler
- Ortak alan rezervasyonlari
- Ziyaretci kayitlari

### 6.6 Kurumsal Yonetim

- Genel kurul
- Hazirun listeleri
- Vekaletnameler
- Oylamalar
- Kararlar
- Yonetici secimi
- Denetim planlari
- Denetim raporlari

### 6.7 Iletisim

- Duyurular
- Bildirimler
- Anketler
- Mesaj sablonlari
- Okundu/onay raporlari

### 6.8 Arsiv

- Belgeler
- Faturalar
- Sozlesmeler
- Tutanaklar
- Raporlar

## 7. Onerilen Teknik Mimari

Mevcut repo yapisi dikkate alinarak hedef mimari:

- Backend: ASP.NET Core Web API
- Domain: temiz domain katmani, aggregate ve entity odakli model
- Application: use case, servis, DTO, validasyon ve is kurallari
- Persistence: Entity Framework Core, iliskisel veritabani
- Core: mevcut audit, localization, notification, cache, exception, validation, auth, compliance, storage ve rate limit kabiliyetleri
- Frontend: React, TypeScript, Vite
- UI: mevcut React/Bootstrap tabani uzerine operasyon odakli, yogun ama okunabilir arayuz
- API dokumantasyonu: OpenAPI/Scalar
- Kimlik: mevcut auth altyapisi korunacak, site yonetimi yetki baglamlariyla genisletilecek
- Cache: mevcut cache servisleri kullanilacak
- Loglama: mevcut loglama ve audit mekanizmalari domain olaylarina baglanacak
- Bildirim: mevcut e-posta/bildirim yapisi duyuru, aidat, toplanti ve talep senaryolarina baglanacak

## 8. Veri Modeli Ana Basliklari

### 8.1 Organizasyon ve Mulk

- Site
- Block
- Floor
- Unit
- UnitType
- ParkingSpace
- StorageArea
- CommonArea

### 8.2 Kisi ve Yetki

Bu basliktaki User, Role ve Permission gibi cekirdek kimlik varliklari mevcut altyapidan gelecektir. Yeni model bu varliklari tekrar olusturmak yerine site, daire ve sakin baglamlariyla iliskilendirecektir.

- User
- Person
- Resident
- Owner
- Tenant
- HouseholdMember
- Role
- Permission
- SiteMembership
- UnitOccupancy
- ContactInformation

### 8.3 Finans

- AccountingPeriod
- Account
- CashAccount
- BankAccount
- LedgerEntry
- Income
- Expense
- Supplier
- SupplierInvoice
- AssessmentPlan
- AssessmentCharge
- Debt
- Payment
- Receipt
- PenaltyRule
- Budget

### 8.4 Operasyon

- Asset
- AssetCategory
- MaintenancePlan
- MaintenanceTask
- ServiceRequest
- RequestComment
- Staff
- Vendor
- Contract
- Reservation
- Visitor

### 8.5 Kurumsal Yonetim

- GeneralAssembly
- AgendaItem
- AttendanceList
- AttendanceRecord
- ProxyAuthorization
- Vote
- Decision
- Election
- BoardTerm
- AuditPeriod
- AuditChecklist
- AuditFinding
- AuditReport

### 8.6 Iletisim ve Belge

- Announcement
- Notification
- NotificationDelivery
- Survey
- SurveyResponse
- Document
- DocumentVersion
- FileAttachment
- MessageTemplate

### 8.7 Sistem

- AuditLog
- AuditLogIntegrity
- LanguageResource
- EmailTemplate
- TermsAndConditions
- UserTermsAcceptance
- IntegrationSettings

## 9. Kritik Is Kurallari

- Silme islemleri varsayilan olarak soft delete olmali; finansal kayitlarda fiziksel silme olmamali.
- Finansal kayitlar onaylandiktan sonra dogrudan degistirilmemeli; duzeltme kaydi ile ters islem yapilmali.
- Tahakkuk ve tahsilat islemleri transaction icinde calismali.
- Donem kapandiktan sonra ilgili doneme yeni kayit ancak yetkili rol ve gerekce ile acilabilmeli.
- Bir dairede ayni anda birden fazla aktif malik olabilir; pay oranlari tutulmali.
- Kiraci ve malik ayni dairede farkli bildirim/yetki kurallarina sahip olabilir.
- Genel kurul karar, hazirun ve vekalet kayitlari sonradan degistirilirse revizyon gecmisi tutulmali.
- Denetim raporu kapandiktan sonra degistirilemez olmali; ek aciklama revizyon olarak eklenmeli.
- Tum kritik belge erisimleri loglanmali.
- Site bazli veri izolasyonu zorunlu olmali.

## 10. API Tasarim Prensipleri

- REST odakli, kaynak bazli endpointler
- Sayfalama, filtreleme, siralama ve arama standartlari
- Tutarlı hata cevap formati
- Validasyon hatalarinda alan bazli hata listesi
- Idempotent tahsilat ve entegrasyon endpointleri
- Optimistic concurrency icin row version kullanimi
- Kritik komutlarda reason/note alani
- Dosya yuklemeleri icin ayri endpoint ve virus tarama entegrasyonuna hazir tasarim
- OpenAPI dokumantasyonunun CI icinde dogrulanmasi

## 11. Frontend Tasarim Prensipleri

- Dashboard ve liste ekranlari operasyonel kullanima uygun, hizli taranabilir olacak.
- Finans ekranlari yogun veri gosterecek; tablo filtreleri, kolon secimi ve disari aktarma desteklenecek.
- Kritik islemler onay modali, gerekce alani ve islem ozeti ile tamamlanacak.
- Formlarda adim adim giris, otomatik kaydetme veya taslak mantigi degerlendirilecek.
- Responsive web desteklenecek; ancak mobil native uygulama ayri fazda ele alinacak.
- Yetkiye gore menuler, aksiyonlar ve alanlar gizlenecek.
- Tum para, tarih, telefon ve adres formatlari yerel kullanimlara uygun olacak.
- Bos durum, hata durumu, yukleniyor durumu ve yetkisiz durum ekranlari standartlasacak.

## 12. Guvenlik, KVKK ve Uyumluluk

- KVKK aydinlatma metni ve kullanici kabul kayitlari
- Iletisim izinleri
- Hassas kisi verileri icin maskeleme
- Mevcut rol ve izin altyapisinin site yonetimi kurallariyla genisletilmesi
- Site bazli veri izolasyonu
- Guvenli parola politikasi
- Rate limit
- Audit log
- Dosya erisim yetkilendirmesi
- Loglarda hassas veri maskeleme
- Yedekleme ve veri saklama politikasi
- Kullanici veri ihraci ve silme/anonomize etme surecleri
- Finansal kayitlar icin degisiklik gecmisi

## 13. Entegrasyon Yol Haritasi

Ilk web surumunde entegrasyon altyapisi hazir olacak; gercek entegrasyonlar oncelige gore aktive edilecek.

- E-posta: ilk surumde aktif
- SMS: altyapi hazir, servis saglayici secimi sonraki karar
- Push notification: mobil faz ile birlikte
- Banka ekstresi ice aktarma: CSV/Excel ile baslangic, API entegrasyonu sonraki faz
- Sanal POS: sonraki faz
- E-fatura/e-arsiv: sonraki faz
- Muhasebe programlari: sonraki faz
- Harita/adres servisleri: ihtiyaca gore

## 14. Uretim Kalitesi Gereksinimleri

- CI pipeline
- Backend unit testleri
- Application servis testleri
- Persistence entegrasyon testleri
- Frontend component ve sayfa testleri
- Kritik akislarda end-to-end test
- Kod kalite kontrolleri
- Migration stratejisi
- Seed data stratejisi
- Ortam bazli konfigürasyon
- Merkezi hata izleme
- Performans olcumleri
- Backup/restore proseduru
- Rollback plani
- API versiyonlama
- Teknik dokumantasyon
- Kullanici dokumantasyonu

## 15. Test Stratejisi

### 15.1 Backend

- Domain is kurallari unit testleri
- Aidat hesaplama testleri
- Gecikme faizi testleri
- Tahakkuk ve tahsilat transaction testleri
- Yetkilendirme testleri
- Audit log testleri
- Donem kapama testleri
- Genel kurul ve hazirun hesaplama testleri
- Denetim kapanis testleri

### 15.2 Frontend

- Form validasyon testleri
- Tablo filtreleme ve siralama testleri
- Rol bazli ekran davranisi testleri
- Finans ekrani hesaplama gorunum testleri
- Kritik modal/onay akislari
- Responsive ekran kontrolleri

### 15.3 E2E

- Site kurulumu
- Blok ve daire tanimlama
- Malik/kiraci ekleme
- Aidat tahakkuku
- Odeme alma
- Gider girisi
- Demirbas ekleme
- Talep acma ve kapatma
- Genel kurul hazirun olusturma
- Denetim raporu kapatma

## 16. Fazlara Bolunmus Gelistirme Plani

### Faz 0 - Urun ve Teknik Hazirlik

- Detayli domain sozlugu
- Rol ve yetki matrisi
- Ana veri modeli karari
- Finansal hesaplama kurallarinin netlestirilmesi
- UI bilgi mimarisi
- API standartlari
- Test ve CI standartlari
- Ortam konfigurasyonlari

### Faz 1 - Domain Temeli ve Mevcut Altyapi Entegrasyonu

- Site, blok, daire ve kisi cekirdek modeli
- Mevcut auth, rol/izin ve profil altyapisinin site/domain iliskileriyle baglanmasi
- Mevcut audit, localization, notification ve dosya altyapisinin yeni domain olaylarinda kullanilmasi
- Temel dashboard
- Ortak tablo, form, modal, filtre ve export bilesenleri
- Varsa mevcut davet akisina site, blok ve daire baglami eklenmesi

### Faz 2 - Mulk ve Sakin Yonetimi

- Site/blok/daire ekranlari
- Malik, kiraci, hane sakini iliskileri
- Tasinma giris/cikis sureci
- Daire profili
- Kisi profili
- Toplu ice aktarma
- Temel belge ekleri

### Faz 3 - Finans ve Aidat

- Muhasebe donemleri
- Gelir/gider kategorileri
- Kasa/banka hesaplari
- Aidat tarifeleri
- Tahakkuk olusturma
- Borc ve tahsilat takibi
- Makbuz
- Gecikme faizi
- Finansal raporlar
- Donem kapama

### Faz 4 - Operasyon ve Demirbas

- Demirbas yonetimi
- Bakim planlari
- Ariza/talep yonetimi
- Tedarikci ve sozlesme yonetimi
- Personel kayitlari
- Ortak alan rezervasyonlari
- Ziyaretci kayitlari

### Faz 5 - Genel Kurul, Hazirun ve Denetim

- Genel kurul tanimlari
- Gunden ve toplanti akisi
- Hazirun listesi
- Vekaletname
- Oylama ve kararlar
- Yonetici/denetci secimi
- Denetim checklistleri
- Denetim bulgulari
- Denetim raporlari

### Faz 6 - Iletisim, Raporlama ve Arsiv

- Duyurular
- Bildirim merkezi
- Anketler
- Belge arsivi
- Gelişmiş raporlar
- PDF/Excel ciktilar
- Okundu/onay takipleri

### Faz 7 - Uretime Hazirlik

- Guvenlik testi
- Performans testi
- Veri yedekleme ve geri yukleme testi
- Kullanici kabul testi
- Pilot site kurulumu
- Canli gecis plani
- Destek sureci
- Monitoring ve alarm kurallari

## 17. Kabul Kriterleri

Uygulama ilk web surumu icin asagidaki kriterleri saglamadan uretime alinmayacak:

- Birden fazla siteyi izole sekilde yonetebiliyor.
- Site, blok, daire, malik, kiraci ve sakin kayitlari tam calisiyor.
- Aidat tahakkuku, borclandirma, odeme ve makbuz sureci tutarli calisiyor.
- Gelir/gider ve kasa/banka kayitlari raporlanabiliyor.
- Demirbas ve talep surecleri kullanilabilir durumda.
- Genel kurul, hazirun ve denetim modulleri temel degil, gercek kullanim senaryosunu kapsayacak seviyede.
- Tum kritik site yonetimi islemleri mevcut audit log altyapisina dusuyor.
- Mevcut rol/izin altyapisi site, blok, daire ve gorev baglaminda ekran ve API seviyesinde uygulanmis.
- PDF/Excel ciktilari temel raporlarda calisiyor.
- E-posta bildirimleri calisiyor.
- Testler CI icinde calisiyor.
- Uygulama en az bir pilot site senaryosunda bastan sona dogrulanmis.

## 18. Mobile Gecis Icin Saklanacak Kararlar

Web surumu tamamlandiktan sonra mobil kapsam yeniden ele alinacak. Mobilde muhtemel oncelikler:

- Sakinler icin borc ve odeme goruntuleme
- Bildirimler
- Duyurular
- Talep/ariza olusturma
- Fotograf yukleme
- Ortak alan rezervasyonu
- Ziyaretci bildirme
- Yonetici icin kritik finans ve talep ozetleri
- Push notification

Mobil uygulamaya gecmeden once webde hangi is akislarinin gercek kullanicida en cok kullanildigi olculecek.

## 19. Acik Kararlar

- Ilk canli surumde online odeme olacak mi?
- SMS saglayici secilecek mi, yoksa e-posta ile mi baslanacak?
- Banka ekstresi icin ilk hedef CSV/Excel mi, banka API mi?
- Muhasebe entegrasyonu hangi programlarla hedeflenecek?
- Daire bazli arsa payi hesaplama detaylari nasil standartlasacak?
- Genel kurul ve vekaletname sureclerinde hukuki belge formatlari kim tarafindan onaylanacak?
- Pilot site verisi gercek veri mi, anonimlestirilmis veri mi olacak?
- Dosya saklama icin yerel disk, S3 uyumlu storage veya baska servis mi kullanilacak?

## 20. Ilk Yapilacak Somut Isler

1. Mevcut altyapi envanteri kesinlestirilecek; yeniden yazilmayacak auth, profil, bildirim, audit, localization ve storage parcalari isaretlenecek.
2. Domain sozlugu ve site yonetimi rol-yetki matrisi hazirlanacak.
3. Site/blok/daire/kisi/veri modeli kesinlestirilecek.
4. Finansal hesaplama kurallari dokumante edilecek.
5. Backend entity ve migration planlari cikarilacak.
6. Frontend navigasyon ve ekran haritasi netlestirilecek.
7. Ortak UI bilesenleri belirlenecek.
8. CI, test ve kod kalite kapilari tanimlanacak.
9. Faz 1 icin teknik issue listesi olusturulacak.
