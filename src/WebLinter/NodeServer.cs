// Modifications Copyright Rich Newman 2017
using EdgeJs;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace WebLinter
{
    public class NodeServer
    {
        public async Task<string> CallServerAsync(string path, ServerPostData postData)
        {
            object result = "";
            try
            {
                // TODO don't need to require on every call
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
                            return err.message;
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
                // TODO this is a bit rubbish
                result = e.Message;
            }

            return result.ToString();
        }
    }
}
