using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using backend.Utils.ApiAuth;

namespace backend.Services {
  public interface IAuthService {
      AuthResponse Authenticate(AuthRequest model, string ipAddress);
      AuthResponse RefreshToken(string token, string ipAddress);
      bool RevokeToken(string token, string ipAddress);
  }
}
