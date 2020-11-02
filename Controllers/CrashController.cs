﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using backend.Utils;

namespace backend.Controllers {

  [ApiController]
  [ApiExplorerSettings(IgnoreApi = true)]
  public class CrashController : ControllerBase {

    [Route("error")]
    public GenericErrorResponse Error() {
      var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
      var exception = context?.Error; // Your exception
      var code = 500; // Internal Server Error by default

      //if (exception is MyNotFoundException) code = 404; // Not Found
      //else if (exception is MyUnauthorizedException) code = 401; // Unauthorized
      //else if (exception is MyException) code = 400; // Bad Request

      Response.StatusCode = code; // You can use HttpStatusCode enum instead

      return new GenericErrorResponse(exception); // Your error model
    }

  } // class
}
