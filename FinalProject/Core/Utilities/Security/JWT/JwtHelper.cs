using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Core.Entities.Concrete;
using Core.Utilities.Security.Encryption;
using Core.Utilities.Security.JWT;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Core.Extensions;
namespace Core.Utilities.Security.Jwt
{
    public class JwtHelper : ITokenHelper
    {
        public IConfiguration Configuration { get; }
        private TokenOptions _tokenOptions;
        private DateTime _accessTokenExpiration; //Access token ne zaman geçerliliğini bitirecek
        public JwtHelper(IConfiguration configuration)
        {
            Configuration = configuration; //appsettings.json classını okumaya yarar
            _tokenOptions = Configuration.GetSection("TokenOptions").Get<TokenOptions>();//appsettings.json deki sectionları al ve tokenoptionsa göre uygula

        }
        public AccessToken CreateToken(User user, List<OperationClaim> operationClaims)
        {
            _accessTokenExpiration = DateTime.Now.AddMinutes(_tokenOptions.AccessTokenExpiration);//Token bitiş süresi
            var securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);//appsettings.json'ı okumaya yarıyor
            var signingCredentials = SigningCredentialsHelper.CreateSigningCredentials(securityKey);//Hangi anahtar veya algoritmanın kullanılacağı
            var jwt = CreateJwtSecurityToken(_tokenOptions, user, signingCredentials, operationClaims);//Token oluşturmak için gerekenler   
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var token = jwtSecurityTokenHandler.WriteToken(jwt);

            return new AccessToken
            {
                Token = token,
                Expiration = _accessTokenExpiration
            };

        }

        public JwtSecurityToken CreateJwtSecurityToken(TokenOptions tokenOptions, User user,
            SigningCredentials signingCredentials, List<OperationClaim> operationClaims)
        {
            var jwt = new JwtSecurityToken( //parametredeki bilgilerle token oluşturuluyor.
                issuer: tokenOptions.Issuer,
                audience: tokenOptions.Audience,
                expires: _accessTokenExpiration,
                notBefore: DateTime.Now,
                claims: SetClaims(user, operationClaims),
                signingCredentials: signingCredentials
            );
            return jwt;
        }

        private IEnumerable<Claim> SetClaims(User user, List<OperationClaim> operationClaims)
        {
            var claims = new List<Claim>();
            claims.AddNameIdentifier(user.Id.ToString());
            claims.AddEmail(user.Email);
            claims.AddName($"{user.FirstName} {user.LastName}");
            claims.AddRoles(operationClaims.Select(c => c.Name).ToArray());

            return claims;
        }
    }
}