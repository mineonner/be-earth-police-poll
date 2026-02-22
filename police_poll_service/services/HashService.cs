using Microsoft.IdentityModel.Tokens;
using police_poll_service.models.respone;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace report_meesuanruam_service.services
{
    public class HashService
    {
        private const int SaltSize = 24;
        private const int HashSize = 32;
        private const int Interrations = 10000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

        private IConfiguration _config;

        public HashService(IConfiguration config)
        {
            _config = config;
        }

        public string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Interrations, Algorithm, HashSize);
            return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}";
        }

        public bool Verify(string password, string passwordHash)
        {
            string[] parts = passwordHash.Split('-');
            byte[] hash = Convert.FromHexString(parts[0]);
            byte[] salt = Convert.FromHexString(parts[1]);

            byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Interrations, Algorithm, HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, inputHash);
        }

        public string createJwtToken(UserResModel userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("user", userInfo.user),
                new Claim("role_code", userInfo.role_code),
                new Claim("org_unit_code", userInfo.org_unit_code), 
                //new Claim(JwtRegisteredClaimNames.Sub, userInfo.user),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(480),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public UserResModel DecodingJwtToken(string token)
        {
            var payload = new JwtSecurityTokenHandler().ReadJwtToken(token).Payload;
            UserResModel result = new UserResModel()
            {
                user = (string)payload["user"],
                role_code = (string)payload["role_code"],
                org_unit_code = (string)payload["org_unit_code"],
            };

            return result;
        }
    }
}
