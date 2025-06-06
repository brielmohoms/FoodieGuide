using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Web.Common.DependencyInjection;
using Umbraco.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1) Register Umbraco (back-office + website)
builder.Services
    .AddUmbraco(builder.Environment, builder.Configuration)
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()   // only if you have IComposer implementations
    .Build();         // <-- returns void, so no var here

// Program.cs  (or a composer if you prefer)

// 2) Build the WebApplication
var app = builder.Build();

// 3) Boot Umbraco
await app.BootUmbracoAsync();

// 4) Standard middleware & routing
app.UseStaticFiles();
app.UseRouting();

app.UseUmbraco()                        
   .WithMiddleware(m => {
       m.UseBackOffice();
       m.UseWebsite();
   })
   .WithEndpoints(e => {
       e.UseBackOfficeEndpoints();
       e.UseWebsiteEndpoints();
   });

// 5) Run
await app.RunAsync();
