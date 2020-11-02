using System;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using backend.Services;
using backend.Utils.ApiAuth;

namespace backend.Controllers {

  [Authorize]
  [ApiController]
  [Route("[controller]")]
  public class DoormanController : ControllerBase {
    private IAuthService _authService;

    public DoormanController(IAuthService authService) {
      _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet("authenticate/{id}")]
    [HttpGet("authenticate")]
    public IActionResult Authenticate(string id) {
      string uid = id;
      if (string.IsNullOrEmpty(uid)) {
        uid = Request.Query["uid"].ToString().Replace(' ', '+');
      }

      AuthRequest model = new AuthRequest() {
        Uid = uid
      };
      var response = _authService.Authenticate(model, ipAddress());

      if (response == null)
        return BadRequest(new { message = "Username or password is incorrect" });

      setTokenCookie(response.RefreshToken);

      return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken() {
      var refreshToken = Request.Cookies["refreshToken"];
      var response = _authService.RefreshToken(refreshToken, ipAddress());

      if (response == null)
        return Unauthorized(new { message = "Invalid token" });

      setTokenCookie(response.RefreshToken);

      return Ok(response);
    }

    [HttpPost("revoke-token")]
    public IActionResult RevokeToken([FromBody] RevokeTokenRequest model) {
      // accept token from request body or cookie
      var token = model.Token ?? Request.Cookies["refreshToken"];

      if (string.IsNullOrEmpty(token))
        return BadRequest(new { message = "Token is required" });

      var response = _authService.RevokeToken(token, ipAddress());

      if (!response) 
        return NotFound(new { message = "Token not found" });

      return Ok(new { message = "Token revoked" });
    }


    // private code (helpers)

    private void setTokenCookie(string token) {
      var cookieOptions = new CookieOptions {
        HttpOnly = true,
        Expires = DateTime.UtcNow.AddDays(7)
      };

      Response.Cookies.Append("refreshToken", token, cookieOptions);
    }

    private string ipAddress() {
      if (Request.Headers.ContainsKey("X-Forwarded-For")) {
        return Request.Headers["X-Forwarded-For"];
      }
      else {
        return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
      }
    }

  } // class
}
