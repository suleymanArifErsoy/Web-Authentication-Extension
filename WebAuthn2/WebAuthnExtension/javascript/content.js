window.addEventListener("load", ()=> {

    console.log("WebAuthn Sayfası yüklendi.");

    // -------------------------------------------------------------------------------------------------------------------------------------
    //                                                         SİTE İÇERİSİNDE YAZMIŞ OLDUĞUMUZ HTML SAYFASININ EKLENMESİ
    // HTML inner işlemleri 

    const newDiv = document.createElement("div");
    newDiv.className = "Extension-container"; // classname doğru yazım

    // Eklenecek Html div
    newDiv.innerHTML= `
        <header class="Extension-container">
            <span class="custom-extension-icon">🔒</span>
            <h3>WebAuthn Eklentisi</h3>
        </header>
        <div class="actions">
            <button class="btn-custom btn-success-custom" id="my-kaydet-button">
                <span class="btn-icon">📥</span>
                WebAuthn ile Kayıt Ol
            </button>
            <button class="btn-custom btn-primary-custom" id="my-giris-button">
                <span class="btn-icon">🔑</span>
                WebAuthn ile Giriş
            </button>
        </div>
    `;

    // Form alanının altına eklenecek. 
    const webAuthnForm = document.querySelector('form');

    if (webAuthnForm) {
        webAuthnForm.parentNode.insertBefore(newDiv, webAuthnForm.nextSibling);
        console.log("Html sayfası başarı ile yüklendi.");

        const kaydetButton = document.getElementById("my-kaydet-button");
        const girisButton = document.getElementById("my-giris-button");

        if(kaydetButton){
            kaydetButton.addEventListener("click",(event)=>{
                console.log("Html üzerinden Kaydet butonuna basıldı.");
                registerApiButton();
            });
        }
        if(girisButton){
            girisButton.addEventListener("click",(event)=>{
                console.log("Html üzerinden Login butonuna basıldı.");
                authenticateApiButton();
            });
        }
    } else {
        console.log("HTML form içeriği bulunamadı. Html eklenti dosyası yüklenemdi.");
    }

    //---------------------------------------------------------------------------------------------------------------------------------
    //                                           SİTENİN HTML TAG'INDA TANIMLANMIŞ BUTONLARI MANİPÜLE ETME 

    // register ve Authenticate buttonlarını manipüle edecez.
    if (webAuthnForm) { // webAuthnForm'un null olup olmadığını kontrol edin
        webAuthnForm.addEventListener("submit",(event)=>{
            event.preventDefault();
            event.stopImmediatePropagation();
            console.log("Submit engellendi.");
        },true);
    }

    const registerButton = document.getElementById("register-button");
    const authenticateButton = document.getElementById("login-button");

    // Register Button 
    if(registerButton) {
        registerButton.addEventListener("click", (event)=> {
            event.preventDefault();
            event.stopImmediatePropagation(); 
            console.log("Register Butonuna tıklandı!!!");
            registerApiButton();
            console.log("Register Butonu Manipüle edildi.");
        },true);
    } else {
        console.log("Register Butonu bulunamadı !!!!!!!");
    }

    // Authenticate Button 
    if(authenticateButton){
        authenticateButton.addEventListener("click",(event)=>{
            event.preventDefault();
            event.stopImmediatePropagation();
            console.log("Authenticate Butonuna Tıklandı.");
            authenticateApiButton();
        },true);
    }

});

// -------------------------------------------------------------------------------------------------------------
//                                                 BUTON İŞLEVLERİ 
async function registerApiButton() {
    const username = getUsername();

    if (username) {
        alert(`API'ye kayıt isteği gönderiliyor: ${username}`);
        console.log(`API'ye kayıt isteği gönderiliyor: ${username}`);

        const apiEndpoint = 'https://localhost:7132/api/User/register'; 

        try {
            const response = await fetch(apiEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ Username: username })
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({ message: 'Yanıt JSON formatında değil veya boş.' }));
                throw new Error(`API Hatası (${response.status}): ${errorData.message || JSON.stringify(errorData)}`);
            }

            const credentialCreationOptions = await response.json();
            console.log('API\'den gelen kayıt seçenekleri:', credentialCreationOptions);

            // *** DÜZELTME: credentialCreationOptions.publicKey'nin varlığını kontrol edin ***
            if (!credentialCreationOptions || !credentialCreationOptions.publicKey) {
                throw new Error("Sunucudan gelen kayıt seçenekleri veya publicKey alanı eksik.");
            }
            
            // `challenge` ve `user.id`'ye güvenli erişim
            if (credentialCreationOptions.publicKey.challenge) {
                credentialCreationOptions.publicKey.challenge = base64ToArrayBuffer(credentialCreationOptions.publicKey.challenge);
            } else {
                console.warn("Kayıt seçeneklerinde challenge bulunamadı.");
                // Burada bir hata fırlatmayı düşünebilirsiniz, challenge olmazsa WebAuthn çalışmaz.
                throw new Error("Sunucudan challenge değeri alınamadı.");
            }
            
            if (credentialCreationOptions.publicKey.user && credentialCreationOptions.publicKey.user.id) {
                credentialCreationOptions.publicKey.user.id = base64ToArrayBuffer(credentialCreationOptions.publicKey.user.id);
            } else {
                console.warn("Kayıt seçeneklerinde user.id bulunamadı.");
                // `user.id` olmazsa yine WebAuthn düzgün çalışmayabilir.
                throw new Error("Sunucudan user.id değeri alınamadı.");
            }

            if (credentialCreationOptions.publicKey.excludeCredentials) {
                credentialCreationOptions.publicKey.excludeCredentials.forEach(cred => {
                    if (cred.id) { // cred.id'nin varlığını kontrol edin
                        cred.id = base64ToArrayBuffer(cred.id);
                    }
                });
            }

            const credential = await navigator.credentials.create({
                publicKey: credentialCreationOptions.publicKey
            });

            const attestationResponse = {
                id: credential.id,
                rawId: ArrayBufferToBase64(credential.rawId), 
                response: {
                    attestationObject: ArrayBufferToBase64(credential.response.attestationObject),
                    clientDataJSON: ArrayBufferToBase64(credential.response.clientDataJSON),
                },
                type: credential.type,
                // authenticatorAttachment, eğer varsa eklenmeli
                authenticatorAttachment: (credential).authenticatorAttachment || undefined 
            };

            const registerResponseEndpoint = 'https://localhost:7132/api/User/register/response';
            const registerResponse = await fetch(registerResponseEndpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(attestationResponse)
            });

            if (!registerResponse.ok) {
                const errorData = await registerResponse.json().catch(() => ({ message: 'Yanıt JSON formatında değil veya boş.' }));
                throw new Error(`API Doğrulama Hatası (${registerResponse.status}): ${errorData.message || JSON.stringify(errorData)}`);
            }

            const result = await registerResponse.json();
            alert(`Kayıt Başarılı: ${result.Message}`);
            console.log('Kayıt başarılı:', result);

        } catch (error) {
            console.error('Kayıt işlemi sırasında hata oluştu:', error);
            alert(`Kayıt işlemi sırasında hata oluştu: ${error.message || 'Bilinmeyen bir hata.'}`);
        }

    } else {
        alert("Kayıt işlemi için bir kullanıcı adı girmeniz gerekmektedir.");
        console.warn("Kayıt işlemi için kullanıcı adı alınamadı veya boş bırakıldı.");
    }
}

async function authenticateApiButton() {
    let loginUsername = getUsername(); 

    if (!loginUsername) {
        alert("Giriş yapmak için bir kullanıcı adı girin.");
        console.warn("Giriş işlemi için kullanıcı adı alınamadı veya boş bırakıldı.");
        return;
    }

    const apiEndpoint = `https://localhost:7132/api/User/authenticate?username=${encodeURIComponent(loginUsername)}`;

    try {
        // Adım 1: API'den kimlik doğrulama seçeneklerini al
        const response = await fetch(apiEndpoint, {
            method: 'GET', // GET metodu ile kullanıcı adını query string olarak gönder
            headers: {
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: 'Yanıt JSON formatında değil veya boş.' }));
            throw new Error(`API Hatası (${response.status}): ${errorData.message || JSON.stringify(errorData)}`);
        }

        const assertionOptions = await response.json();
        console.log('API\'den gelen kimlik doğrulama seçenekleri:', assertionOptions);

        // *** DÜZELTME: assertionOptions.publicKey'nin varlığını kontrol edin ***
        if (!assertionOptions || !assertionOptions.publicKey) {
            throw new Error("Sunucudan gelen kimlik doğrulama seçenekleri veya publicKey alanı eksik.");
        }

        // Seçenekleri WebAuthn API'sine uygun hale getir
        if (assertionOptions.publicKey.challenge) {
            assertionOptions.publicKey.challenge = base64ToArrayBuffer(assertionOptions.publicKey.challenge);
        } else {
            console.warn("Kimlik doğrulama seçeneklerinde challenge bulunamadı.");
            throw new Error("Sunucudan challenge değeri alınamadı.");
        }

        if (assertionOptions.publicKey.allowCredentials) {
            assertionOptions.publicKey.allowCredentials.forEach(cred => {
                if (cred.id) { // cred.id'nin varlığını kontrol edin
                    cred.id = base64ToArrayBuffer(cred.id);
                }
            });
        }

        // Adım 2: Tarayıcıda kimlik doğrulaması yap
        const credential = await navigator.credentials.get({
            publicKey: assertionOptions.publicKey
        });

        // Adım 3: Doğrulanan kimlik bilgisini API'ye gönder
        const assertionResponse = {
            id: credential.id,
            rawId: ArrayBufferToBase64(credential.rawId),
            response: {
                authenticatorData: ArrayBufferToBase64(credential.response.authenticatorData),
                clientDataJSON: ArrayBufferToBase64(credential.response.clientDataJSON),
                signature: ArrayBufferToBase64(credential.response.signature),
                // userHandle null olabileceği için kontrol ekleyin
                userHandle: credential.response.userHandle ? ArrayBufferToBase64(credential.response.userHandle) : null,
            },
            type: credential.type,
        };

        const authenticateResponseEndpoint = 'https://localhost:7132/api/User/authenticate/response';
        const authenticateResponse = await fetch(authenticateResponseEndpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(assertionResponse)
        });

        if (!authenticateResponse.ok) {
            const errorData = await authenticateResponse.json().catch(() => ({ message: 'Yanıt JSON formatında değil veya boş.' }));
            throw new Error(`API Kimlik Doğrulama Hatası (${authenticateResponse.status}): ${errorData.message || JSON.stringify(errorData)}`);
        }

        const result = await authenticateResponse.json();
        alert(`Giriş Başarılı: ${result.Message}`);
        console.log('Giriş başarılı:', result);

        // Başarılı giriş sonrası profile sayfasına yönlendirme
        if (result.RedirectUrl) {
            window.location.href = result.RedirectUrl;
        }

    } catch (error) {
        console.error('Kimlik doğrulama işlemi sırasında hata oluştu:', error);
        alert(`Kimlik doğrulama işlemi sırasında hata oluştu: ${error.message || 'Bilinmeyen bir hata.'}`);
    }
}

// ArrayBuffer'ı Base64 string'e dönüştürür (Base64 URL Safe değildir)
function ArrayBufferToBase64(buffer) {
    // FIDO2 genellikle Base64Url kullanır. Bu fonksiyon standart Base64 kullanır.
    // Eğer sunucu Base64Url bekliyorsa, bu fonksiyonda ayarlama yapmanız gerekebilir.
    return btoa(String.fromCharCode(...new Uint8Array(buffer)));
}

// Base64 string'i ArrayBuffer'a dönüştürür (Base64 URL Safe değildir)
function base64ToArrayBuffer(base64) {
    // FIDO2 genellikle Base64Url kullanır. Bu fonksiyon standart Base64 kullanır.
    // Eğer sunucudan Base64Url geliyorsa, atob öncesi düzeltme yapmanız gerekebilir (örn: -'i + ya, _'yi /'ye çevirme).
    const binary_string = window.atob(base64.replace(/-/g, '+').replace(/_/g, '/')); // Base64Url safe dönüşümü için ekleme
    const len = binary_string.length;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
        bytes[i] = binary_string.charCodeAt(i);
    }
    return bytes.buffer;
}

// Input alanına giren Username'i çalıştır. 
function getUsername(){
    const inputUsername = document.getElementById("input-email");
    let username = "";
    if(inputUsername){
        username = inputUsername.value.trim();
        if(username){
            console.log("Kullanici adi :",username);
            return username;
        } else {
            alert("Kullanıcı adını giriniz.");
            return null;
        }
    } else {
        console.log("Input alanı alınamadı.");
        return null;
    }
}

// --------------------------------------------------------------------------------------------------------------------------
//                                                POPUP TAG'INDAN GELEN MESAJLARI YÖNETME 

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.action === "performRegistration") {
        console.log("Popup'tan kayit isteği alindi!");
        registerApiButton(); 
    } else if (message.action === "performAuthentication") {
        console.log("Popup'tan giriş isteği alindi !");
        authenticateApiButton(); 
    }
});