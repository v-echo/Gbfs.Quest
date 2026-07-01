using System.Security.Cryptography;
using System.Text;

namespace Gbfs.Quest.Auth
{
    /// <summary>
    /// A very simple in-memory basic authentication
    /// </summary>
    internal class BasicConsoleAuthProvider : IAuthProvider
    {
        Dictionary<string, string> Users { get; } = [];

        public bool Login(string user, string password) => Users.TryGetValue(user, out var hash) && hash == QuickHash(password);

        // Logout is handled UI-side

        public bool Register(string user, string password) => Users.TryAdd(user, QuickHash(password));

        static string QuickHash(string input)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(input);
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
