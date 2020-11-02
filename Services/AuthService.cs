using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using backend.Models;
using backend.Utils;
using backend.Utils.ApiAuth;

namespace backend.Services {
  public class AuthService : IAuthService {
    private rapidesqlContext _context;
    private readonly AppSettingsDTO _appSettings;

    public AuthService(rapidesqlContext context, IOptions<AppSettingsDTO> appSettings) {
      _context = context;
      _appSettings = appSettings.Value;
    }

    public AuthResponse Authenticate(AuthRequest model, string ipAddress) {
      // le model.uid est encrypté 
      int uid = Convert.ToInt32(Crypto.DecryptString(model.Uid, _appSettings.RouteKey));

      AuthUser authUser = LoadAuthUser(uid).FirstOrDefault();

      // return null if user not found
      if (authUser == null) return null;

      LoadRefreshTokens(authUser);

      // authentication successful so generate jwt and refresh tokens
      var jwtToken = generateJwtToken(authUser);
      var refreshToken = generateRefreshToken(ipAddress);

      // save refresh token
      authUser.RefreshTokens.Add(refreshToken);
      AddRefreshToken(authUser, refreshToken);

      return new AuthResponse(authUser, jwtToken, refreshToken.Token);
    }

    private IQueryable<AuthUser> LoadAuthUser(int uid) {
      IQueryable<AuthUser> rv =  from User in _context.Users
                                 join Agent in _context.Agents on User.IdAgent equals Agent.IdAgent
                                 where User.IdUser == uid

                                 select new AuthUser {
                                   Id = User.IdUser,
                                   FirstName = Agent.Prenom,
                                   LastName = Agent.Nom,
                                   Username = User.Login,
                                   RefreshTokens = new List<RefreshToken>()
                                 };

      return rv;
    }

    private void AddRefreshToken(AuthUser authUser, RefreshToken refreshToken) {
      UserRefreshTokens urt = new UserRefreshTokens() {
        IdUser = authUser.Id,
        Token = refreshToken.Token,
        Expires = refreshToken.Expires,
        Created = refreshToken.Created,
        CreatedByIp = refreshToken.CreatedByIp,
        Revoked = refreshToken.Revoked,
        RevokedByIp = refreshToken.RevokedByIp,
        ReplacedByToken = refreshToken.ReplacedByToken
      };

      _context.UserRefreshTokens.Add(urt);
      _context.SaveChanges();

      return;
    }

    private void LoadRefreshTokens(AuthUser authUser) {
      IQueryable<UserRefreshTokens> urts = from x in _context.UserRefreshTokens where x.IdUser == authUser.Id select x;
      foreach (UserRefreshTokens urt in urts) {
        RefreshToken rt = new RefreshToken() {
          Id = urt.IdUser,
          Token = urt.Token,
          Expires = urt.Expires,
          Created = urt.Created,
          CreatedByIp = urt.CreatedByIp,
          Revoked = urt.Revoked,
          RevokedByIp = urt.RevokedByIp,
          ReplacedByToken = urt.ReplacedByToken
        };
        authUser.RefreshTokens.Add(rt);
      }

      return;
    }

    public AuthResponse RefreshToken(string token, string ipAddress) {
      UserRefreshTokens urt = _context.UserRefreshTokens.SingleOrDefault(x => x.Token == token);
      if (urt == null) return null;
      
      AuthUser authUser = LoadAuthUser(urt.IdUser).FirstOrDefault();
      if (authUser == null) return null;

      LoadRefreshTokens(authUser);
      var refreshToken = authUser.RefreshTokens.Single(x => x.Token == token);

      // return null if token is no longer active
      if (!refreshToken.IsActive) return null;

      // replace old refresh token with a new one and save
      var newRefreshToken = generateRefreshToken(ipAddress);
      refreshToken.Revoked = DateTime.UtcNow;
      refreshToken.RevokedByIp = ipAddress;
      refreshToken.ReplacedByToken = newRefreshToken.Token;
      authUser.RefreshTokens.Add(newRefreshToken);

      AddRefreshToken(authUser, newRefreshToken);

      // generate new jwt
      var jwtToken = generateJwtToken(authUser);

      return new AuthResponse(authUser, jwtToken, newRefreshToken.Token);
    }

    public bool RevokeToken(string token, string ipAddress) {
      UserRefreshTokens urt = _context.UserRefreshTokens.SingleOrDefault(x => x.Token == token);
      if (urt == null) return false;

      AuthUser authUser = LoadAuthUser(urt.IdUser).FirstOrDefault();
      if (authUser == null) return false;

      LoadRefreshTokens(authUser);
      var refreshToken = authUser.RefreshTokens.Single(x => x.Token == token);

      // return false if token is not active
      if (!refreshToken.IsActive) return false;

      // revoke token and save
      refreshToken.Revoked = DateTime.UtcNow;
      refreshToken.RevokedByIp = ipAddress;
      
      urt.Revoked = DateTime.UtcNow;
      urt.RevokedByIp = ipAddress;
      _context.Update<UserRefreshTokens>(urt);

      return true;
    }

    private string generateJwtToken(AuthUser user) {
      JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
      byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
      SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.Name, user.Id.ToString()) }),
        Expires = DateTime.UtcNow.AddMinutes(15),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
      };

      SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

      return tokenHandler.WriteToken(token);
    }

    private RefreshToken generateRefreshToken(string ipAddress) {
      using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider()) {
        byte[] randomBytes = new byte[64];
        rngCryptoServiceProvider.GetBytes(randomBytes);
        return new RefreshToken {
          Token = Convert.ToBase64String(randomBytes),
          Expires = DateTime.UtcNow.AddDays(7),
          Created = DateTime.UtcNow,
          CreatedByIp = ipAddress
        };
      }
    }

  } // class
}
