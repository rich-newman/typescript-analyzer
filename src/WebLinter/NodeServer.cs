// Modifications Copyright Rich Newman 2017
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EdgeJs;

namespace WebLinter
{
    public class NodeServer
    {
        private const string BASE_URL = "http://localhost";
        private static Process _process;
        private static object _syncRoot = new object();
        private static AsyncLock _mutex = new AsyncLock();

        public int BasePort { get; private set; }

        //public async Task<string> CallServerAsync(string path, ServerPostData postData)
        //{
        //    await EnsureInitializedAsync();

        //    string url = $"{BASE_URL}:{BasePort}/{path.ToLowerInvariant()}";
        //    string json = JsonConvert.SerializeObject(postData);

        //    try
        //    {
        //        using (WebClient client = new WebClient())
        //        {
        //            return await client.UploadStringTaskAsync(url, json);
        //        }
        //    }
        //    catch (WebException)
        //    {
        //        Down();
        //        return string.Empty;
        //    }
        //}

        public async Task<string> CallServerAsync(string path, ServerPostData postData)
        {
            // Works (actually lints) if we manually copy the edge folder into path
            // C:\Users\{user}\AppData\Local\Microsoft\VisualStudio\15.0_b8342a0bExp\Extensions\Rich Newman\TypeScript Analyzer\1.3
            // and then have node_modules with tslint and typescript as a subfolder of edge
            object result = "";
            //await EnsureInitializedAsync();
            try
            {
                //                var increment = Edge.Func(@"
                //    var current = 0;

                //    return function (data, callback) {
                //        current += data;
                //        callback(null, current);
                //    }
                //");
                //                result = await increment(4);
                var func = Edge.Func(@"

                    function lintts(configFile, fixErrors, files) {
                        try {
                            var fs = require('fs');
                            var tslint = require('tslint');
                            var options = {
                                fix: fixErrors,
                                formatter: 'json'
                            };
                            var linter = new tslint.Linter(options);

                            for (var i = 0; i < files.length; i++)
                            {
                                var fileName = files[i];
                                var fileContents = fs.readFileSync(fileName, 'utf8');
                                var configuration = tslint.Configuration.findConfiguration(configFile, fileName).results;
                                linter.lint(fileName, fileContents, configuration);
                            }
                            return JSON.parse(linter.getResult().output);
                        }
                        catch (err) {
                            return err.message + ': ' + __dirname;
                        }
                    }

                    return function (data, callback) {
                        var input = JSON.parse(data);
                        var result = lintts(input.config, input.fixerrors, input.files);
                        callback(null, JSON.stringify(result));
                    }
                ");
                string json = JsonConvert.SerializeObject(postData);
                result = await func(json);
            }
            catch (Exception e)
            {
                string test = e.Message;
            }

            return result.ToString();
        }

        public void Down()
        {
            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                        _process.Kill();
                }
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }

        private async Task EnsureInitializedAsync()
        {
            using (await _mutex.LockAsync())
            {
                if (_process != null && !_process.HasExited)
                    return;

                try
                {
                    Down();
                    SelectAvailablePort();

                    ProcessStartInfo start = new ProcessStartInfo(Path.Combine(LinterFactory.ExecutionPath, "node.exe"))
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        Arguments = $"\"{Path.Combine(LinterFactory.ExecutionPath, "server.js")}\" {BasePort}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    _process = Process.Start(start);

                    // Give the node server some time to initialize
                    await Task.Delay(100);
                }
                catch (Exception)
                {
                    Down();
                }
            }
        }

        private void SelectAvailablePort()
        {
            // Creates the Socket to send data over a TCP connection.

            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                    socket.Bind(endPoint);
                    IPEndPoint endPointUsed = (IPEndPoint)socket.LocalEndPoint;
                    BasePort = endPointUsed.Port;
                }
            }
            catch (SocketException)
            {
                /* Couldn't get an available IPv4 port */
                try
                {
                    using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                    {
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Any, 0);
                        socket.Bind(endPoint);
                        IPEndPoint endPointUsed = (IPEndPoint)socket.LocalEndPoint;
                        BasePort = endPointUsed.Port;
                    }
                }
                catch (SocketException)
                {
                    /* Couldn't get an available IPv6 port either */
                }
            }
        }

        private static string GetNodeDirectory()
        {
            string toolsDir = Environment.GetEnvironmentVariable("VS140COMNTOOLS");

            if (!string.IsNullOrEmpty(toolsDir))
            {
                string parent = Directory.GetParent(toolsDir).Parent.FullName;
                return Path.Combine(parent, @"IDE\Extensions\Microsoft\Web Tools\External\Node");
            }

            return string.Empty;
        }
    }
}
