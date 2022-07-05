using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oyster.EmailService;

namespace Oyster {
public class Startup {
  public Startup(IConfiguration configuration) {
    Configuration = configuration;
  }

  public IConfiguration Configuration { get; }


  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services) {
    services.AddRazorPages().AddRazorRuntimeCompilation();
    // Use login path as a home page for unauthenticated users
    services.ConfigureApplicationCookie(options =>
      options.LoginPath = "/"
    );

    services.Configure<IdentityOptions>(options => {
      options.Password.RequireNonAlphanumeric = false;
      options.Password.RequireUppercase = false;
      options.SignIn.RequireConfirmedEmail = true;
      options.SignIn.RequireConfirmedAccount = true;
      options.User.RequireUniqueEmail = true;
    });

    services.AddTransient<IEmailSender, SmtpEmailSender>();
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
    var contentRoot = env.ContentRootPath;
    if (env.IsDevelopment()) {
      app.UseDeveloperExceptionPage();
    } else {
      // Set the general server error page
      app.UseExceptionHandler("/Errors/500");
      // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
      app.UseHsts();
    }
    // Set pages for specific http error codes
    app.UseStatusCodePagesWithReExecute("/Errors/{0}");
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
  }
}
}