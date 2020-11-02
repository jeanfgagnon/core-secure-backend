using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

using System;
using System.Text;

using backend.Models;
using backend.Services;
using backend.Utils;

namespace backend {
  public class Startup {
    public Startup(IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
      var connectionString = Configuration.GetConnectionString("RapideSQL");
      services.AddDbContext<rapidesqlContext>(options => options.UseSqlServer(connectionString));

      // configure strongly typed settings objects
      var appSettingsSection = Configuration.GetSection("AppSettings");
      services.Configure<AppSettingsDTO>(appSettingsSection);

      services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.IgnoreNullValues = true);

      // configure jwt authentication
      var appSettings = appSettingsSection.Get<AppSettingsDTO>();
      var key = Encoding.ASCII.GetBytes(appSettings.Secret);
      services.AddAuthentication(x =>
      {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(x => {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ValidateIssuer = false,
          ValidateAudience = false,
                // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                ClockSkew = TimeSpan.Zero
        };
      });

      // configure DI for application services
      services.AddScoped<IAuthService, AuthService>();
      //services.AddControllers();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }

      app.UseHttpsRedirection();

      app.UseRouting();

      // global cors policy
      app.UseCors(x => x
          .SetIsOriginAllowed(origin => true)
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials());

      app.UseAuthentication();
      app.UseAuthorization();

      // envoi toutes erreur de l'api sur cette route
      app.UseExceptionHandler("/error");
      app.UseEndpoints((IEndpointRouteBuilder endpoints) => endpoints.MapControllers());
    }
  }
}
