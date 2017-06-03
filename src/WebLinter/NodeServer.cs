using EdgeJs;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WebLinter
{
    public class NodeServer
    {
        private Func<object, Task<object>> lintFunc;
        public NodeServer()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                CreateLintFunc();
                sw.Stop();
                Debug.WriteLine("CreateLintFunc: " + sw.ElapsedMilliseconds);
            }
            catch (Exception)
            {
                Debug.WriteLine("NodeServer");
                throw;
            }
        }

        private void CreateLintFunc()
        {
            try
            {
                lintFunc = Edge.Func(@"
                    var fs = require('fs');
                    var tslint = require('tslint');

                    function lintts(configFile, fixErrors, files) {
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
                        // We can't return the failure objects as the graph is circular somewhere
                        return linter.getResult().output;
                    }

                    return function (data, callback) {
                        var result = lintts(data.Config, data.FixErrors, data.Files);
                        callback(null, result);
                    }
                ");
            }
            catch (Exception)
            {
                Debug.WriteLine("CreateLintFunc");
                throw;
            }
        }

        public async Task<string> CallServerAsync(string path, ServerPostData postData)
        {
            try
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();
                object result = await lintFunc(postData);
                sw.Stop();
                Debug.WriteLine("lintFunc(postData): " + sw.ElapsedMilliseconds);

                return result.ToString();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error in linter call: " + e.Message);
                return null;
            }
        }
    }
}
