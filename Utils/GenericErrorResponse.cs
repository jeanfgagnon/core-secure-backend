using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Utils {
  public class GenericErrorResponse {
    public string Type { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }

    public GenericErrorResponse(Exception ex) {
      Type = ex.GetType().Name;
      Message = ex.Message;
      StackTrace = ex.StackTrace;
    }
  }
}
