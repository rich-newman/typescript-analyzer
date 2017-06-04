using EdgeJs;
using System;
using System.Threading.Tasks;

namespace WebLinter
{
    public class NodeServer
    {
        private Func<object, Task<object>> lintFunc;
        public NodeServer()
        {
            lintFunc = CreateLintFunc();
        }

        private Func<object, Task<object>> CreateLintFunc()
        {
            return Edge.Func(@"
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
                        //sleep(3);
                        return linter.getResult().output;
                    }

                    return function (data, callback) {
                        var result = lintts(data.Config, data.FixErrors, data.Files);
                        callback(null, result);
                    }

                    function sleep(seconds) 
                    {
                      var e = new Date().getTime() + (seconds * 1000);
                      while (new Date().getTime() <= e) {}
                    }
                ");
        }

        public string CallServerSync(string path, ServerPostData postData)
        {
            // We assume we're called on the UI thread and it doesn't matter if we block it for the
            // duration of the Edge call
            try
            {
                Task<object> task = lintFunc(postData);
                // Don't block UI thread for more than two seconds: build will continue if we time out
                bool completed = task.Wait(2000);
                return completed ? task.Result.ToString() : null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error in linter call: " + e.Message);
                return null;
            }
        }

        public async Task<string> CallServerAsync(string path, ServerPostData postData)
        {
            try
            {
                object result = await lintFunc(postData);
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
