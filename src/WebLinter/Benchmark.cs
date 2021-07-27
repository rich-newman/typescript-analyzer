using System;
using System.Diagnostics;
using System.IO;

namespace WebLinter
{
    /// <summary>
    /// Writes timings for lint runs to the path in variable path.  Disabled on conditional compile flags by default.
    /// </summary>
    /// <remarks>Enable by adding BENCHMARK conditional compile flag to WebLinter AND WebLinterVsix projects</remarks>
    public static class Benchmark
    {
        private static Stopwatch stopwatch = new Stopwatch();
        private static string log = "";
        private static readonly string path = @"c:\Temp\benchmark.txt";

        [Conditional("BENCHMARK")]
        public static void Start(string message = "Benchmark run started")
        {
            Reset();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AddLogEntry(message);
            stopwatch.Start();
        }

        [Conditional("BENCHMARK")]
        private static void Reset()
        {
            log = "";
            stopwatch.Reset();
        }

        [Conditional("BENCHMARK")]
        public static void Log(string message)
        {
            AddLogEntry($"{message} Elapsed: {stopwatch.ElapsedMilliseconds}");
        }

        [Conditional("BENCHMARK")]
        public static void End(string message = "Benchmark run ended")
        {
            stopwatch.Stop();
            AddLogEntry($"{message} Elapsed: {stopwatch.ElapsedMilliseconds}");
            WriteLog();
        }

        [Conditional("BENCHMARK")]
        private static void AddLogEntry(string message)
        {
            log += $"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToShortDateString()} {message}\n";
        }

        [Conditional("BENCHMARK")]
        private static void WriteLog()
        {
            using (StreamWriter w = File.AppendText(path)) { w.Write(log); }
        }
    }
}
