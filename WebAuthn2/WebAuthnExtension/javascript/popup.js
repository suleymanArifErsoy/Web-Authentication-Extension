document.addEventListener("DOMContentLoaded",()=>{


    const registerButton = document.getElementById("kaydet");
    const loginButton = document.getElementById("giris");

    if(registerButton){

        registerButton.addEventListener("click",(event)=>{

            alert("Kayit isteği gönderiliyor...");

            chrome.tabs.query({active : true , currentWindow : true},(tabs)=>{

                chrome.tabs.sendMessage(tabs[0].id,{action: "performRegistration"});

            });

        });
    }
    if(loginButton){

        loginButton.addEventListener("click",(event)=>{

            alert("giriş isteği gönderiliyor ...");

            chrome.tabs.query({active:true,currentWindow:true}, (tabs) =>{


                chrome.tabs.sendMessage(tabs[0].id,{action: "performAuthentication"})
            });
            



        });

    }

});