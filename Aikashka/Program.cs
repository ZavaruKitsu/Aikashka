#region

using System.Globalization;
using Aikashka;
using Microsoft.Extensions.Hosting;

#endregion

var host = Host.CreateApplicationBuilder();
host.Logging.AddAikashkaLogging();
host.Services.AddAikashkaServices();

// set locale
var culture = CultureInfo.GetCultureInfo(host.Configuration["Bot:Locale"]!);

Thread.CurrentThread.CurrentCulture = culture;
Thread.CurrentThread.CurrentUICulture = culture;

await host
      .Build()
      .RunAsync();
