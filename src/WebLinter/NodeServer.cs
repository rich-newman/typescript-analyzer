// Modifications Copyright Rich Newman 2017
using EdgeJs;
using Newtonsoft.Json;
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
                        return JSON.parse(linter.getResult().output);
                    }

                    return function (data, callback) {
                        try {
                            var result = lintts(data.Config, data.FixErrors, data.Files);
                            callback(null, JSON.stringify(result));
                        }
                        catch (err) {
                            callback(null, err.message);
                        }
                    }
                ");
        }

        public async Task<string> CallServerAsync(string path, ServerPostData postData)
        {
            object result = "";
            try
            {
                result = await lintFunc(postData);
            }
            catch (Exception e)
            {
                // TODO this is a bit rubbish
                result = e.Message;
            }
            return result.ToString();
        }
    }
}
