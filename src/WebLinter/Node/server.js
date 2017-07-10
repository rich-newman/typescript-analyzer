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
        files.forEach(f => console.log(f));
        console.log(usetsconfig);
        try {
            var options = {
                fix: fixErrors,
                formatter: "json"
            };

            if (usetsconfig) {
                return linttsconfigs(configFile, files, options);
            } else {
                return lintFiles(configFile, files, options);
            }
        }
        catch (err) {
            console.log(err.message + "::" + message);
            return err.message + "::" + message;
        }
    }

};

function lintFiles(configFile, files, options) {
    var tslint = require("tslint");
    var linter = new tslint.Linter(options);
    for (var i = 0; i < files.length; i++) {
        var fileName = files[i];
        var fileContents = fs.readFileSync(fileName, "utf8");
        var configuration = tslint.Configuration.findConfiguration(configFile, fileName).results;
        linter.lint(fileName, fileContents, configuration);
    }
    return linter.getResult().output;

}

function linttsconfigs(tslintConfigFile, tsconfigFiles, options) {
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
    var failuresJson = failures.map(function (failure) { return failure.toJson(); });
    return JSON.stringify(failuresJson);
}

start(process.argv[2]);
