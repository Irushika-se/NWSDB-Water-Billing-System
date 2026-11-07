using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NWSDB.API.Services
{
    /// <summary>
    /// Issues JWTs for the three "Authenticate ..." use cases in the use-case diagram:
    /// Authenticate User (Customer), Authenticate Staff, Authenticate API Client (Partner).
    /// The role claim is what [Authorize(Roles = "...")] checks on every protected endpoint.
    /// </summary>
    public class JwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(string subjectId, string role, string displayName, IEnumerable<Claim>? extraClaims = null)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, subjectId),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.Name, displayName)
            };

            // e.g. the NWSDB Customer ID(s) a QuickPay login is authorized for - baked into
            // the signed token at login time so every later request re-checks them from the
            // token itself, never from a lookup the browser could influence.
            if (extraClaims != null)
                claims.AddRange(extraClaims);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
