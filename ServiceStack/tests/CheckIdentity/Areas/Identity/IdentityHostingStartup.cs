// using System;
// using CheckIdentity.Areas.Identity.Data;
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Identity.UI;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
//
// [assembly: HostingStartup(typeof(CheckIdentity.Areas.Identity.IdentityHostingStartup))]
// namespace CheckIdentity.Areas.Identity
// {
//     public class IdentityHostingStartup : IHostingStartup
//     {
//         public void Configure(IWebHostBuilder builder)
//         {
//             builder.ConfigureServices((context, services) => {
//                 services.AddDbContext<CheckIdentityIdentityDbContext>(options =>
//                     options.UseSqlite(
//                         context.Configuration.GetConnectionString("DefaultConnection")));
//
//                 services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//                     .AddEntityFrameworkStores<CheckIdentityIdentityDbContext>();
//             });
//         }
//     }
// }