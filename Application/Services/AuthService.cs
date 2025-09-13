using Supabase.Gotrue;

namespace datopus.Application.Services
{
    public class AuthService(Supabase.Client sbClient)
    {
        public async Task<Session?> VerifyTokenHash(string tokenHash)
        {
            return await sbClient.Auth.VerifyTokenHash(tokenHash);
        }
    }
}
