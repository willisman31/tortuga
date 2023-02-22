using System;
using System.IO;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace tortuga
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WhitelistLocalServerIp();
            HttpServer.RunServer();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) => 
                {
                    services.AddHostedService<Worker>();
                });
        
        public static void WhitelistLocalServerIp() {
            var netsh = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "netsh.exe");
            var startInfo = new ProcessStartInfo(netsh);
            startInfo.Arguments = $"{HttpServer.protocol} add urlacl url={HttpServer.url} user=Users";
            startInfo.UseShellExecute = true;
            startInfo.Verb = "runas";
            try {
                var process = Process.Start(startInfo);
                process.WaitForExit();
            } catch(FileNotFoundException) {
                Console.WriteLine(".netsh not found or other FileNotFoundException thrown while whitelisting IP");
            } catch(Win32Exception) {
                Console.WriteLine("Generic error thrown while whitelisting IP");
            }
        }
    }

    public class HttpServer {
        public static HttpListener listener;
        public static int port = 8000;
        public static string protocol = "http";
        public static string url = SetUrl();
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData = 
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "    <p>Page Views: {0}</p>" +
            "    <form method=\"post\" action=\"shutdown\">" +
            "      <input type=\"submit\" value=\"Shutdown\" {1}>" +
            "    </form>" +
            "  </body>" +
            "</html>";

        public static string GetIp() {
            string ip = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .ToString();
            return ip;
        }

        public static string SetUrl() {
            string prefix = protocol + "://";
            string HostAddress = prefix + "*" + ":" + port.ToString() + "/";
            Console.WriteLine(HostAddress);
            return HostAddress;
        }

        public static void RunServer()
        {
            HttpServer.listener = new HttpListener();
            try {
                HttpServer.listener.Prefixes.Add(HttpServer.url);
            } catch {
                HttpServer.listener.Prefixes.Add("http://localhost:" + port.ToString() + "/");
            } 
            HttpServer.listener.Start();
            Console.WriteLine(value: $"Listening for connections on {HttpServer.url}");

            // Handle requests
            Task listenTask = HttpServer.HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            HttpServer.listener.Close();
        }

        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;
            while (runServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("Request #: {0}", ++requestCount);
                Console.WriteLine(req.Url.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);
                Console.WriteLine();

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown")) {
                    Console.WriteLine("Shutdown requested");
                    runServer = false;
                }

                // Make sure we don't increment the page views counter if `favicon.ico` is requested
                if (req.Url.AbsolutePath != "/favicon.ico") {
                    pageViews += 1;
                }
                // Write the response info
                string disableSubmit = !runServer ? "disabled" : "";
                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, disableSubmit));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }
    }
}
