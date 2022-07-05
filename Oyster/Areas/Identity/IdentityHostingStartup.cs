using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oyster.Areas.Identity.Data;
using Oyster.Data;

[assembly: HostingStartup(typeof(Oyster.Areas.Identity.IdentityHostingStartup))]

namespace Oyster.Areas.Identity {
public class IdentityHostingStartup : IHostingStartup {
  public void Configure(IWebHostBuilder builder) {
    builder.ConfigureServices((context, services) => {
      services.AddDbContext<OysterContext>(options =>
        options
          // .ConfigureWarnings(w=>w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))

          // .LogTo(Console.WriteLine)
          .UseSqlServer(
            context.Configuration.GetConnectionString("OysterContextConnection")));

      services.AddDefaultIdentity<OysterUser>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<OysterContext>();
    });
  }
}
}