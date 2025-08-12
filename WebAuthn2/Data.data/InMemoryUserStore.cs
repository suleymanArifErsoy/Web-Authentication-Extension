using System.Collections.Concurrent;
using Fido2NetLib.Objects; 
using Fido2NetLib; 

namespace WebAuthn2.Data 
{
    public class StoredCredential
    {
        public byte[] Id { get; set; }
        public byte[] PublicKey { get; set; }
        public byte[] UserHandle { get; set; }
        public uint SignCount { get; set; }
        public string AttestationFormat { get; set; }
        public DateTimeOffset RegDate { get; set; }
        public Guid AaGuid { get; set; }
        public List<AuthenticatorTransport>? Transports { get; set; }
        public bool IsBackupEligible { get; set; }
        public bool IsBackedUp { get; set; }
        public PublicKeyCredentialDescriptor Descriptor { get; set; } 

        
    }


    public class InMemoryUserStore
    {
        private readonly ConcurrentDictionary<string, Fido2User> _users = new ConcurrentDictionary<string, Fido2User>();
        private readonly ConcurrentDictionary<string, List<StoredCredential>> _credentialsByUser = new ConcurrentDictionary<string, List<StoredCredential>>();
        private readonly ConcurrentDictionary<byte[], StoredCredential> _credentialsById = new ConcurrentDictionary<byte[], StoredCredential>(new ByteArrayComparer());

        // Kullanıcıyı alır veya ekler
        public Fido2User GetOrAddUser(string username, Func<Fido2User> createUser)
        {
            return _users.GetOrAdd(username, (_) => createUser());
        }

        // Kullanıcıyı adına göre alır
        public Fido2User GetUser(string username)
        {
            _users.TryGetValue(username, out var user);
            return user;
        }

        // Bir kimlik bilgisi (credential) ID'sine göre kullanıcıları bulur
        public Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId, CancellationToken cancellationToken = default)
        {
            if (_credentialsById.TryGetValue(credentialId, out var cred))
            {
                // UserHandle'ı kullanarak kullanıcıyı bul
                var user = _users.Values.FirstOrDefault(u => u.Id.SequenceEqual(cred.UserHandle));
                if (user != null)
                {
                    return Task.FromResult(new List<Fido2User> { user });
                }
            }
            return Task.FromResult(new List<Fido2User>());
        }

        // Bir kullanıcıya ait tüm kimlik bilgilerini alır
        public List<StoredCredential> GetCredentialsByUser(Fido2User user)
        {
            _credentialsByUser.TryGetValue(user.Name, out var credentials);
            return credentials ?? new List<StoredCredential>();
        }

        // Bir kimlik bilgisi ID'sine göre kimlik bilgisini alır
        public StoredCredential GetCredentialById(byte[] credentialId)
        {
            _credentialsById.TryGetValue(credentialId, out var credential);
            return credential;
        }

        // Bir kullanıcı handle'ına (UserHandle) göre kimlik bilgilerini alır
        public Task<List<StoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle, CancellationToken cancellationToken = default)
        {
           
            var user = _users.Values.FirstOrDefault(u => u.Id.SequenceEqual(userHandle));
            if (user != null)
            {
                return Task.FromResult(GetCredentialsByUser(user));
            }
            return Task.FromResult(new List<StoredCredential>());
        }

        // Yeni bir kimlik bilgisini bir kullanıcıya ekler
        public void AddCredentialToUser(Fido2User user, StoredCredential credential)
        {
            var userCredentials = _credentialsByUser.GetOrAdd(user.Name, (_) => new List<StoredCredential>());
            userCredentials.Add(credential);
            _credentialsById.TryAdd(credential.Id, credential);
        }

        // Bir kimlik bilgisinin imza sayacını günceller
        public void UpdateCounter(byte[] credentialId, uint newCounter)
        {
            if (_credentialsById.TryGetValue(credentialId, out var credential))
            {
                credential.SignCount = newCounter;
            }
        }

        // Tüm kayıtlı kimlik bilgilerini döndürür (passwordless senaryolar için)
        public List<StoredCredential> GetAllCredentials()
        {
            return _credentialsById.Values.ToList();
        }
    }

    // byte dizilerini ConcurrentDictionary anahtarı olarak kullanmak için karşılaştırıcı
    public class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x == null || y == null) return x == y;
            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj == null) return 0;
            return obj.Aggregate(0, (current, b) => current ^ b.GetHashCode());
        }
    }
}
