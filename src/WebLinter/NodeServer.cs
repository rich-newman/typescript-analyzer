﻿// Modifications Copyright Rich Newman 2017
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
        // _process is static, so even though we new the Linter and the NodeServer on every lint call
        // we reuse the same process if it's alive and kicking
        private static Process _process;
        private static object _syncRoot = new object();
        private static AsyncLock _mutex = new AsyncLock();

        public int BasePort { get; private set; }

        public async Task<string> CallServer(string path, ServerPostData postData, bool callSync)
        {
            try
            {
                await EnsureNodeProcessIsRunning(callSync);

                string url = $"{BASE_URL}:{BasePort}/{path.ToLowerInvariant()}";
                string json = JsonConvert.SerializeObject(postData);

                using (WebClient client = new WebClient())
                {
                    if (callSync)
                    {
                        Task<string> task = Task.Run(async () =>
                                                     await client.UploadStringTaskAsync(url, json));
                        bool completed = task.Wait(5000);
                        if (!completed) throw new Exception("TsLint call on build timed out.  Timeout is 5 seconds.");
                        return completed ? task.Result : null;
                    }
                    else
                    {
                        return await client.UploadStringTaskAsync(url, json);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Down();
                // return error message so that it will be shown in error list
                // e.g. if firewall blocks connection: "Unable to connect to the remote server"
                return "Failed to communicate with TsLint server: " + ex.Message + (ex.InnerException != null ? " --> " + ex.InnerException.Message : "");
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

        private async Task EnsureNodeProcessIsRunning(bool callSync)
        {
            using (await _mutex.Lock(callSync))
            {
                if (_process != null && !_process.HasExited)
                    return;
                //if (callSync) throw new Exception("Unable to lint: webserver not correctly initialized");
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
#if DEBUG
                    start.WindowStyle = ProcessWindowStyle.Normal;
                    start.CreateNoWindow = false;
                    start.Arguments = "--inspect " + start.Arguments;
#endif
                    _process = Process.Start(start);

                    // Give the node server some time to initialize
                    if (callSync)
                        System.Threading.Thread.Sleep(100);
                    else
                        await Task.Delay(100);

                    if (_process.HasExited)
                        throw new Exception($"TsLint server failed to start: {start.FileName} {start.Arguments}");
                }
                catch (Exception)
                {
                    Down();
                    throw;
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
