using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebAuthn2.core.DTos;
using WebAuthn2.Data;

namespace WebAuthn2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {

        private readonly Fido2 _fido2;
        private readonly InMemoryUserStore _store; 

        public UserController(Fido2 fido2, InMemoryUserStore store) 
        {
            _fido2 = fido2;
            _store = store;
        }

        private string FormatException(Exception e)
        {
            return string.Format("{0}{1}", e.Message, e.InnerException != null ? " (" + e.InnerException.Message + ")" : "");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {

            try
            {
                if (string.IsNullOrEmpty(registerDto.Username))
                {
                    return BadRequest("Kayıt olmak için bir kullanıcı adı girin !!!!");
                }

                Console.WriteLine($"Apiye gelen Kullanıcı Adi : {registerDto.Username}");

    
                var user = _store.GetOrAddUser(registerDto.Username, () => new Fido2User
                {
                    DisplayName = registerDto.Username,
                    Name = registerDto.Username,
                    Id = Encoding.UTF8.GetBytes(registerDto.Username) 
                });

   
                var existingKeys = _store.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();

                var authenticatorSelection = new AuthenticatorSelection
                {
                   
                    ResidentKey = ResidentKeyRequirement.Required,
                    UserVerification = UserVerificationRequirement.Preferred,
                    
                    AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform 
                };

                var exts = new AuthenticationExtensionsClientInputs()
                {
                    Extensions = true,
                    UserVerificationMethod = true,
                    CredProps = true
                };

                var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
                {
                    User = user,
                    ExcludeCredentials = existingKeys,
                    AuthenticatorSelection = authenticatorSelection,
                    AttestationPreference = AttestationConveyancePreference.None,
                    Extensions = exts
                });

              
           
                HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

            
                return Ok(options); 
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Status = "error", ErrorMessage = FormatException(e) });
            }

        }
        [HttpPost("register/response")]
        public async Task<IActionResult> MakeCredential([FromBody] AuthenticatorAttestationRawResponse attestationResponse, CancellationToken cancellationToken)
        {
            try
            {

                var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
                if (string.IsNullOrEmpty(jsonOptions))
                {
                    return BadRequest("Kayıt seçenekleri bulunamadı veya süresi doldu.");
                }
                var options = CredentialCreateOptions.FromJson(jsonOptions);

           
                IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, token) =>
                {
                    var users = await _store.GetUsersByCredentialIdAsync(args.CredentialId, token);
                    if (users.Count > 0)
                        return false; 

                    return true;
                };

             
                var credential = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
                {
                    AttestationResponse = attestationResponse,
                    OriginalOptions = options,
                    IsCredentialIdUniqueToUserCallback = callback
                }, cancellationToken: cancellationToken);

               
                _store.AddCredentialToUser(options.User, new StoredCredential
                {
                   
                    Descriptor = new PublicKeyCredentialDescriptor(credential.Id),
                    PublicKey = credential.PublicKey,
                    UserHandle = credential.User.Id,
                    SignCount = credential.SignCount,
                    AttestationFormat = credential.AttestationFormat,
                    RegDate = DateTimeOffset.UtcNow,
                    AaGuid = credential.AaGuid,
                 
                    Transports = credential.Transports?.ToList(), 
                    IsBackupEligible = credential.IsBackupEligible,
                    IsBackedUp = credential.IsBackedUp,
                    
                });

               
                HttpContext.Session.Remove("fido2.attestationOptions");

               
                return Ok(new { Message = "Kayıt başarıyla tamamlandı.", Username = options.User.Name });
            }
            catch (Fido2VerificationException ex)
            {
                return BadRequest($"Fido2 doğrulama hatası: {ex.Message}");
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Status = "error", ErrorMessage = FormatException(e) });
            }
        }




        [HttpGet("authenticate")]
        public async Task<IActionResult> Authenticate([FromQuery] string username)
        {
            try
            {
                var existingCredentials = new List<PublicKeyCredentialDescriptor>();
                Fido2User user = null;

                if (!string.IsNullOrEmpty(username))
                {
                 
                    user = _store.GetUser(username);
                    if (user == null)
                    {
                        return NotFound("Kullanıcı bulunamadı.");
                    }
                  
                    existingCredentials = _store.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
                }
                else 
                {
                   
                    existingCredentials = _store.GetAllCredentials().Select(c => c.Descriptor).ToList();
                }

                if (!existingCredentials.Any())
                {
                    return BadRequest("Giriş yapmak için kayıtlı bir kimlik bilgisi bulunmuyor.");
                }

                var exts = new AuthenticationExtensionsClientInputs()
                {
                    Extensions = true,
                    UserVerificationMethod = true
                };

              
                var uv = UserVerificationRequirement.Preferred;

                var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams()
                {
                   
                    AllowedCredentials = existingCredentials,
                    UserVerification = uv,
                    Extensions = exts
                });

               
                HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());
              
                HttpContext.Session.SetString("fido2.assertionUsername", user?.Name ?? "");


              
                return Ok(options);
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Status = "error", ErrorMessage = FormatException(e) });
            }
        }
        [HttpPost("authenticate/response")]
        public async Task<IActionResult> MakeAssertion([FromBody] AuthenticatorAssertionRawResponse clientResponse, CancellationToken cancellationToken)
        {
            try
            {
               
                var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
                if (string.IsNullOrEmpty(jsonOptions))
                {
                    return BadRequest("Kimlik doğrulama seçenekleri bulunamadı veya süresi doldu.");
                }
                var options = AssertionOptions.FromJson(jsonOptions);

              
                var username = HttpContext.Session.GetString("fido2.assertionUsername");
                Fido2User storedUser = null;
                if (!string.IsNullOrEmpty(username))
                {
                    storedUser = _store.GetUser(username);
                    if (storedUser == null)
                    {
                        return BadRequest("Kimlik doğrulama için kullanıcı bulunamadı.");
                    }
                }
                else 
                {
                    if (clientResponse.Response?.UserHandle != null)
                    {
                        var userHandleStr = Encoding.UTF8.GetString(clientResponse.Response.UserHandle);
                       
                        storedUser = _store.GetUser(userHandleStr);
                        if (storedUser == null)
                        {
                            
                            var usersWithCred = await _store.GetUsersByCredentialIdAsync(clientResponse.RawId, cancellationToken);
                            storedUser = usersWithCred.FirstOrDefault();
                        }
                    }
                }

                if (storedUser == null)
                {
                    return BadRequest("Kimlik doğrulamak için uygun kullanıcı bulunamadı.");
                }


                var creds = _store.GetCredentialById(clientResponse.RawId) ?? throw new Exception("Unknown credentials");

                
                var storedCounter = creds.SignCount;

                IsUserHandleOwnerOfCredentialIdAsync callback = async (args, token) =>
                {
                    var storedCreds = await _store.GetCredentialsByUserHandleAsync(args.UserHandle, token);
                    return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
                };

               
                var res = await _fido2.MakeAssertionAsync(new MakeAssertionParams
                {
                    AssertionResponse = clientResponse,
                    OriginalOptions = options,
                    StoredPublicKey = creds.PublicKey,
                    StoredSignatureCounter = storedCounter,
                    IsUserHandleOwnerOfCredentialIdCallback = callback
                }, cancellationToken: cancellationToken);

             
                _store.UpdateCounter(res.CredentialId, res.SignCount);

                // Oturumdaki kimlik doğrulama seçeneklerini temizle
                HttpContext.Session.Remove("fido2.assertionOptions");
                HttpContext.Session.Remove("fido2.assertionUsername");


               
                
                return Ok(new { Message = $"Giriş başarıyla tamamlandı, hoş geldiniz {storedUser.DisplayName}!", RedirectUrl = "https://webauthn.io/profile" });
            }
            catch (Fido2VerificationException ex)
            {
                return BadRequest($"Fido2 doğrulama hatası: {ex.Message}");
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Status = "error", ErrorMessage = FormatException(e) });
            }
        }
    }
}
