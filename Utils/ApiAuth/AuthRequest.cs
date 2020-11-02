using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace backend.Utils.ApiAuth {
  public class AuthRequest {
    [Required]
    public string Uid { get; set; }
  }
}
