namespace Gbfs.Quest.Auth
{
    internal interface IAuthProvider
    {
        bool Login(string user, string password);
        bool Register(string user, string password);
    }
}