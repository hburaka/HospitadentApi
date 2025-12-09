# Dokploy Deployment Rehberi

## 🚀 Hızlı Deployment Adımları

### 1. Git Repository Ayarları
Dokploy'da "Provider" bölümünde:
- **Repository URL**: Git repository URL'nizi girin
- **Branch**: `main` veya `master` (production branch'iniz)
- **Build Path**: `/` (kök dizin)
- **Enable Submodules**: Kapalı (eğer submodule yoksa)

### 2. Build Type Seçimi
**"Build Type"** bölümünde:
- ✅ **Dockerfile** seçin (önerilen - daha kontrollü)
  - Dockerfile proje kök dizininde mevcut
- VEYA **Nixpacks** seçin (otomatik algılama)

### 3. Environment Variables Ayarları ⚠️ ÖNEMLİ
**"Environment"** sekmesine gidin ve şu değişkenleri ekleyin:

```env
CONNECTION_STRING=Server=152.89.36.234;Port=3306;Database=ota19dds_hsptdnt181921;Uid=ota19dds_reportuser;Pwd=KRqnSM{$~tj-OY#7;Allow Zero Datetime=true;Convert Zero Datetime=true;
JWT_SECRET_KEY=bfe9Sul1gxQnHppgpRtWrNWpfAGg1aNVeEMy5YUMwrr
JWT_ISSUER=HospitadentApi
JWT_AUDIENCE=HospitadentApi_Users
JWT_EXPIRATION_MINUTES=1440
ASPNETCORE_ENVIRONMENT=Production
```

**⚠️ ÖNEMLİ:** Dokploy'da Environment Variables eklerken:
- Her satır bir KEY=VALUE formatında olmalı
- Tırnak işareti kullanmayın
- Özel karakterler için escape yapmayın (Dokploy otomatik handle eder)

### 4. Port Ayarları
- Dokploy otomatik port atar
- Veya manuel olarak **8080** portunu kullanabilirsiniz
- Dockerfile'da `EXPOSE 8080` tanımlı

### 5. Domain Ayarları (Opsiyonel)
**"Domains"** sekmesinden:
- Custom domain ekleyebilirsiniz
- SSL sertifikası otomatik olarak Let's Encrypt ile sağlanır

### 6. Deploy İşlemi
1. **"Save"** butonuna tıklayın
2. **"Deploy"** butonuna tıklayın
3. Build loglarını takip edin
4. Deployment tamamlandığında test edin

## 🔍 Test Adımları

Deployment sonrası:
1. **Health Check**: `https://your-domain/api/auth/validate` (Bearer token ile)
2. **Login Test**: `POST https://your-domain/api/auth/login`
3. **Swagger**: Production'da kapalı olmalı (şu an açık - sonra kapatacağız)

## ⚠️ Şu An Eksik Olanlar (Deployment Sonrası Yapılacak)

1. ✅ Environment Variables eklendi
2. ❌ Swagger UI production'da kapatılmalı
3. ❌ CORS politikası eklenmeli
4. ❌ Security headers eklenmeli
5. ❌ Rate limiting eklenmeli

## 🐛 Sorun Giderme

### Build Hatası
- Dockerfile'ın proje kök dizininde olduğundan emin olun
- `.dockerignore` dosyası oluşturun (opsiyonel)

### Connection String Hatası
- Environment Variables'ın doğru eklendiğinden emin olun
- Dokploy'da "Environment" sekmesini kontrol edin

### Port Hatası
- Dokploy otomatik port atar, genelde sorun olmaz
- Manuel port belirlemek isterseniz Dockerfile'ı düzenleyin

