// Modifications Copyright Rich Newman 2017
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebLinter
{
    public class NodeServer
    {
        private const string BASE_URL = "http://localhost";
        private static Process _process;
        private static object _syncRoot = new object();
        private static AsyncLock _mutex = new AsyncLock();

        public int BasePort { get; private set; }

        public async Task<string> CallServerAsync(string path, ServerPostData postData)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            await EnsureInitializedAsync();
            sw.Stop();
            Debug.WriteLine("EnsureInitializedAsync: " + sw.ElapsedMilliseconds);

            string url = $"{BASE_URL}:{BasePort}/{path.ToLowerInvariant()}";
            string json = JsonConvert.SerializeObject(postData);
            File.WriteAllText(@"c:\temp\json.txt", json);

            try
            {
                using (WebClient client = new WebClient())
                {
                    sw = new Stopwatch();
                    sw.Start();
                    string result = await client.UploadStringTaskAsync(url, json);
                    sw.Stop();
                    Debug.WriteLine("client.UploadStringTaskAsync: " + sw.ElapsedMilliseconds);
                    File.WriteAllText(@"c:\temp\result.txt", result);
                    return result;
                }
            }
            catch (WebException)
            {
                Down();
                return string.Empty;
            }
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
