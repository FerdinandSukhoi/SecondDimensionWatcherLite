using System.Configuration;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using SecondDimensionWatcher;
using SecondDimensionWatcher.Data;
using SecondDimensionWatcher.Models;
using SecondDimensionWatcher.Services;

var builder = WebApplication.CreateBuilder(args);

//builder.Configuration.AddJsonFile("app.json", false, true);

var services = builder.Services;

services.AddRazorPages();
services.AddServerSideBlazor();
services.AddControllers();

services.AddDbContext<AppDataContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});
var provider = new FileExtensionContentTypeProvider();
provider.Mappings.Add(".mkv", "video/webm");
services.AddSingleton(provider);

services.AddHttpClient<FeedService>(http =>
{
    http.BaseAddress = new(builder.Configuration["DownloadSetting:BaseAddress"]);
    http.DefaultRequestHeaders.UserAgent.Add(
        new("SecondDimensionWatcherLite", "1.0"));
});
services.AddMemoryCache();
services.AddScoped<BlazorContext>();
services.AddTransient<FeedService>();
services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    q.AddJob<RssUpdateJob>(o => { o.WithIdentity("rss"); });
    q.AddJob<FetchTorrentInfoJob>(o => { o.WithIdentity("fetch"); });
    q.AddTrigger(o =>
    {
        o.ForJob("rss")
            .WithCronSchedule("0 0/10 * 1/1 * ? *");
    });
    q.AddTrigger(o => { o.ForJob("fetch").StartNow(); });
});


services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

var dataContext = app.Services.GetRequiredService<AppDataContext>();
if (builder.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

dataContext.Database.Migrate();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapBlazorHub();
    endpoints.MapFallbackToPage("/_Host");
    endpoints.MapFallbackToPage("/play/{*param}", "/_Host");
});

await app.RunAsync();
