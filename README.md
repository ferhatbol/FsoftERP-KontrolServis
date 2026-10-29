# FsoftERP-KontrolServis (FsoftERPSKWS)

FsoftERP için Depo Stok Tarihçe ve Sipariş Durum Kontrol işlemlerini otomatik olarak yürüten Windows Arka Plan Servisi ve Yönetim Paneli.

## 🚀 Özellikler

1. **📦 Depo Stok Tarihçe (`Sp_Set_DepoStokTarihce`):**
   - Belirlenen saatlerde (Örn: Günde 2 kez saat 12:00 ve 19:00'da veya günde 3 kez 06:00, 12:00, 21:00'de) otomatik çalışır.
   - Kolay saat ekleme/silme ve hazır şablonlar.
   - Aktif / Pasif yapılabilir.
   - Komut zaman aşımı (timeout) ayarlanabilir.
   - Arayüzden tek tıkla test çalıştırılabilir.

2. **📋 Sipariş Durum Kontrol (`Sp_Set_SparisDurumKontrol`):**
   - Belirlenen dakika aralıklarıyla (Örn: Her 3 dakikada bir) periyodik olarak çalışır.
   - İsteğe bağlı `@SiparisNo` parametresi verilebilir (boş bırakıldığında `NULL` olarak tüm siparişler taranır).
   - Aktif / Pasif yapılabilir.
   - Komut zaman aşımı (timeout) ayarlanabilir.
   - Arayüzden tek tıkla test çalıştırılabilir.

3. **🗄️ Veritabanı ve Güvenlik:**
   - MSSQL Server bağlantı ayarları (Sunucu, Veritabanı, Windows Auth veya SQL Auth).
   - Şifreler makineye özel **DPAPI (`ENC(...)`)** ile otomatik şifrelenir; açık metin olarak saklanmaz.
   - Tek tıkla canlı bağlantı testi (gecikme süresi ve SQL versiyonu göstergesi).

4. **⚙️ Windows Servis Yönetimi & Canlı Log Konsolu:**
   - Arayüz üzerinden tek tıkla Servisi Kur, Servisi Kaldır, Başlat ve Durdur.
   - Gerçek zamanlı konsol çıktısı ve günlük dönen dosya loglama (`logs/service_yyyyMMdd.log`).
   - Servis çalışırken ayarlar değiştirilip kaydedildiğinde servisi yeniden başlatmaya gerek kalmadan 30 saniye içinde yeni ayarları otomatik uygular.

## 🛠️ Derleme ve Çalıştırma

### Gereksinimler
- .NET 9.0 SDK veya .NET 8.0 SDK
- Windows 10 / 11 veya Windows Server 2016+

### Derleme
```powershell
dotnet build -c Release
```

### Kullanım Komutları
| Komut | Açıklama |
|---|---|
| `FsoftERPSKWS.exe` | Görsel Yönetim ve Ayar Panelini açar (GUI) |
| `FsoftERPSKWS.exe --service` | Windows Servis modunda arka planda çalıştırır |
| `FsoftERPSKWS.exe --install` | Windows Servisini sisteme otomatik başlangıçlı kurar |
| `FsoftERPSKWS.exe --uninstall` | Windows Servisini sistemden kaldırır |
| `FsoftERPSKWS.exe --start` | Servisi başlatır |
| `FsoftERPSKWS.exe --stop` | Servisi durdurur |
| `FsoftERPSKWS.exe --run-depo` | `Sp_Set_DepoStokTarihce` işlemini hemen çalıştırır |
| `FsoftERPSKWS.exe --run-siparis` | `Sp_Set_SparisDurumKontrol` işlemini hemen çalıştırır |

---
**Versiyon:** `1.0.0.1`  
**Yazar:** Ferhat BOL  
**Telif Hakkı:** © 2026 Fsoft. Tüm hakları saklıdır.
