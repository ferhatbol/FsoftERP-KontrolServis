# FsoftERP-KontrolServis (FsoftERPSKWS)

FsoftERP için sınırsız sayıda **Stored Procedure** tanımlayabileceğiniz, her birine bağımsız çalışma periyodu veya saat planı atayabileceğiniz kurumsal Windows Arka Plan Servisi ve Görsel Yönetim Paneli.

## 🚀 Yeni Özellikler (v1.0.1.0)

1. **⚡ Sınırsız Sayıda Stored Procedure Ekleme / Çıkarma:**
   - İster `Sp_Set_DepoStokTarihce`, ister `Sp_Set_SparisDurumKontrol`, ister `Sp_Set_SatınalmaDurumKontrol`, ister kendi yazdığınız özel bir procedure olsun; dilediğiniz kadar SP ekleyebilir, silebilir, kopyalayabilir veya düzenleyebilirsiniz.

2. **🕒 Esnek ve Bağımsız Çalışma Planı:**
   - **Günde Belirtilen Saatlerde Çalıştırma:** Örn. günde 1 kez saat `12:00`'de, günde 2 kez `12:00` ve `19:00`'da, günde 3 kez `06:00, 12:00, 21:00`'de (dakika hassasiyetinde sınırsız saat listesi).
   - **Periyodik Dakika Aralıklarında Çalıştırma:** Örn. her `3` dakikada bir, her `15` dakikada bir veya her `60` dakikada bir.
   - Her görevin çalışma planı ve parametreleri tamamen birbirinden bağımsızdır.

3. **⚙️ Bağımsız Eşzamanlı (Concurrent) Çalışma:**
   - Görevler birbirini kilitlemez. 10 dakika süren ağır bir SP çalışırken, 3 dakikalık hızlı SP arka planda zamanı geldiğinde aksamadan yürütülür.

4. **🗄️ Parametre Desteği & Zaman Aşımı (Timeout):**
   - Her procedure için parametre (`@SiparisNo = NULL`, vb.) ve komut zaman aşımı süresi tanımlanabilir.

5. **🖥️ Windows Form Görsel Paneli (`FsoftERPSKWS.exe`):**
   - Kod veya `appsettings.json` ile uğraşmadan tek ekranda tablo üzerinden yönetim.
   - Kolay Görev Ekle / Düzenle / Sil / Klonla pencereleri.
   - Tek tıkla veritabanı bağlantı testi ve SP'leri beklemeden anında test çalıştırma.
   - Windows Servisi Kurma / Kaldırma / Başlatma / Durdurma butonları.
   - Canlı log konsolu.

---

## 🛠️ Komut Satırı (CLI) Kullanımı

| Komut | Açıklama |
|---|---|
| `FsoftERPSKWS.exe` | Görsel Yönetim ve Ayar Panelini açar (GUI) |
| `FsoftERPSKWS.exe --service` | Windows Servis modunda arka planda çalıştırır |
| `FsoftERPSKWS.exe --install` | Windows Servisini sisteme otomatik başlangıçlı kurar |
| `FsoftERPSKWS.exe --uninstall` | Windows Servisini sistemden kaldırır |
| `FsoftERPSKWS.exe --start` | Servisi başlatır |
| `FsoftERPSKWS.exe --stop` | Servisi durdurur |
| `FsoftERPSKWS.exe --run-all` | Aktif tüm Stored Procedure görevlerini hemen çalıştırır |
| `FsoftERPSKWS.exe --run-sp=<SP_ADI>` | Belirtilen Stored Procedure'ü hemen çalıştırır |

---
**Versiyon:** `1.0.1.0`  
**Yazar:** Ferhat BOL  
**Telif Hakkı:** © 2026 Fsoft. Tüm hakları saklıdır.
