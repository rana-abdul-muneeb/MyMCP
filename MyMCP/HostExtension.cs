using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMCP
{
   public static class HostExtension
    {
        public static async Task RunApplicationandPrintConsoleMessage(this IHost host, string message)
        {
            var lifetime = host.Services.GetService(typeof(IHostApplicationLifetime)) as IHostApplicationLifetime;

            lifetime!.ApplicationStarted.Register(() => {

                Console.WriteLine(message);
            });
            await host.RunAsync();
        }

    }
}
