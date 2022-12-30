using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HealthCheckSample.Web.Extensions
{
    public class CustomTokenValidator : JwtSecurityTokenHandler
    {

        public override ClaimsPrincipal ValidateToken(string token, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
        {
            try
            {

                var claimsPrincipal = base.ValidateToken(token, validationParameters, out validatedToken);

                return claimsPrincipal;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        protected override JwtSecurityToken ValidateSignature(string token, TokenValidationParameters validationParameters)
        {
            var jwt = new JwtSecurityToken(token);

            return jwt;
        }
    }
}
