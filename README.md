
# WebAuthn Tarayıcı Eklentisi / WebAuthn Browser Extension
 Google chrome ortamında çalışan bir kimlik doğrulama eklentisidir. Eksiklikleri var geliştirme aşaması devam ediyor ... 
 
Bu proje, **WebAuthn API** kullanarak web sitelerinde güvenli kimlik doğrulama (FIDO2) ve kayıt işlemlerini kolaylaştırmak amacıyla geliştirilmiş bir tarayıcı eklentisidir.  
Proje, HTML, CSS ve JavaScript ile hazırlanmış olup, hem kayıt (register) hem de giriş (authenticate) akışlarını destekler.

---

## 🇹🇷 Türkçe Açıklama

### Özellikler
- WebAuthn (FIDO2) protokolü ile güvenli kayıt ve giriş.
- HTML form alanına otomatik UI ekleme.
- Mevcut butonları manipüle ederek WebAuthn akışını başlatma.
- API ile entegre çalışma (localhost örneği verilmiştir).
- Base64 ↔ ArrayBuffer dönüşümleri için yardımcı fonksiyonlar.


### Kurulum
1. Projeyi bilgisayarınıza klonlayın:
   ```bash
   git clone https://github.com/kullaniciadi/proje-adi.git
2.Tarayıcınızda geliştirici modu açın ve bu klasörü Unpacked Extension olarak yükleyin.

3.API endpoint’lerini (localhost:7132 örneği) kendi sunucunuza göre güncelleyin.

Kullanım
1.Web sayfasındaki kullanıcı adı alanına giriş yapın.

2."WebAuthn ile Kayıt Ol" butonuna tıklayarak yeni bir anahtar oluşturun.

3."WebAuthn ile Giriş" butonu ile kimlik doğrulaması yapın.
