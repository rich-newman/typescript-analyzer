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
            var debug = false;
            try {
                if (body === "")
                    return;

                var linter = linters[path];

                if (linter) {
                    var data = JSON.parse(body);
                    debug = data.debug;
                    var result = linter(data.config, data.fixerrors, data.files, data.usetsconfig, data.debug);

                    res.writeHead(200, { 'Content-Type': 'application/json' });
                    res.write(result);
                } else {
                    throw Error("No linter found for " + path);
                }
            }
            catch (e) {
                if (debug) {
                    console.log(e);
                }
                res.writeHead(500, { 'Content-Type': 'application/json' });
                var response = {
                    "exception": JSON.stringify(e, Object.getOwnPropertyNames(e)),
                    "error": true,
                    "message": e.message,
                    "path": path
                };
                res.write(JSON.stringify(response));
            }
            finally {
                res.end();
            }
        });

    }).listen(port);
};

var linters = {
    tslint: function (configFile, fixErrors, files, usetsconfig, debug) {
        if (debug) {
            console.log(configFile);
            files.forEach(f => console.log(f));
            console.log(usetsconfig);
        }
        var options = {
            fix: fixErrors,
            formatter: "json"
        };

        if (usetsconfig) {
            return linttsconfigs(configFile, files, options, debug);
        } else {
            return lintFiles(configFile, files, options, debug);
        }
    }

};

function lintFiles(configFile, files, options, debug) {
    var tslint = require("tslint");
    var linter = new tslint.Linter(options);
    for (var i = 0; i < files.length; i++) {
        var fileName = files[i];
        var fileContents = fs.readFileSync(fileName, "utf8");
        var configuration = tslint.Configuration.findConfiguration(configFile, fileName).results;
        linter.lint(fileName, fileContents, configuration);
    }
    if (debug) console.log(linter.getResult().failures.length + " errors found");
    return linter.getResult().output;

}

function linttsconfigs(tslintConfigFile, tsconfigFiles, options, debug) {
    var tslint = require("tslint");
    var failures = [];
    for (var tsconfigCtr = 0; tsconfigCtr < tsconfigFiles.length; tsconfigCtr++) {
        var program = tslint.Linter.createProgram(tsconfigFiles[tsconfigCtr]);
        var tsFiles = tslint.Linter.getFileNames(program);
        var linter = new tslint.Linter(options, program);

        for (var i = 0; i < tsFiles.length; i++) {
            var tsFileContents = program.getSourceFile(tsFiles[i]).getFullText();
            var tslintConfiguration = tslint.Configuration.findConfiguration(tslintConfigFile, tsFiles[i]).results;
            linter.lint(tsFiles[i], tsFileContents, tslintConfiguration);
        }
        failures = failures.concat(linter.getResult().failures);
    }
    if (debug) console.log(failures.length + " errors found");
    var failuresJson = failures.map(function (failure) { return failure.toJson(); });
    return JSON.stringify(failuresJson);
}

start(process.argv[2]);
