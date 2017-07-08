// Modifications Copyright Rich Newman 2017
var http = require("http"),
    fs = require("fs");

var start = function (port) {
    http.createServer(function (req, res) {

        if (!req.url || req.url.length < 2) {
            res.writeHead(200, { 'Content-Type': 'text/plain' });
            res.end();
            return;
        }

        var path = req.url.substring(1);
        var body = "";

        if (path === "ping") {
            res.writeHead(200, { 'Content-Type': 'text/plain' });
            res.end("pong");
            return;
        }

        req.on('data', function (data) {
            body += data;
        });

        req.on('end', function () {
            try {
                if (body === "")
                    return;

                var linter = linters[path];

                if (linter) {
                    var data = JSON.parse(body);
                    var result = linter(data.config, data.fixerrors, data.files, data.usetsconfig);

                    res.writeHead(200, { 'Content-Type': 'application/json' });
                    res.write(result);
                }
            }
            catch (e) {
                res.writeHead(500, { 'Content-Type': 'text/plain' });
                res.write("Server error: " + e.message);
            }
            finally {
                res.end();
            }
        });

    }).listen(port);
};

var linters = {
    tslint: function (configFile, fixErrors, files, usetsconfig) {
        console.log(configFile);
        console.log(files[0]);
        console.log(usetsconfig);
        try {
            var tslint = require("tslint");
            var options = {
                fix: fixErrors,
                formatter: "json"
            };

            if (usetsconfig) {
                // Easy to get confused between tsconfig.json which is in files[], and 
                // tslint.json which is tsConfigFileConfiguration here
                var failures = [];
                for (var tsconfigctr = 0; tsconfigctr < files.length; tsconfigctr++) {
                    var program = tslint.Linter.createProgram(files[tsconfigctr]);
                    var tsfiles = tslint.Linter.getFileNames(program);
                    var tsConfigLinter = new tslint.Linter(options, program);

                    for (var i = 0; i < tsfiles.length; i++) {
                        var sourceFile = program.getSourceFile(tsfiles[i]);
                        var tsConfigFileContents = sourceFile.getFullText();
                        var tsConfigFileConfiguration = tslint.Configuration.findConfiguration(configFile, tsfiles[i]).results;
                        tsConfigLinter.lint(tsfiles[i], tsConfigFileContents, tsConfigFileConfiguration);
                    }
                    failures = failures.concat(tsConfigLinter.getResult().failures);
                }
                var failuresJSON = failures.map(function (failure) { return failure.toJson(); });
                return JSON.stringify(failuresJSON);
                //var results = tsfiles.map(file => {
                //    var fileContents = program.getSourceFile(file).getFullText();
                //    var linter = new tslint.Linter(file, fileContents, options, program);
                //    return linter.lint();
                //});
            } else {
                var linter = new tslint.Linter(options);
                for (var i = 0; i < files.length; i++) {
                    var fileName = files[i];
                    var fileContents = fs.readFileSync(fileName, "utf8");
                    var configuration = tslint.Configuration.findConfiguration(configFile, fileName).results;
                    linter.lint(fileName, fileContents, configuration);
                }
                return linter.getResult().output;
            }
        }
        catch (err) {
            console.log(err.message + "::" + message);
            return err.message + "::" + message;
        }
    }

};

start(process.argv[2]);
