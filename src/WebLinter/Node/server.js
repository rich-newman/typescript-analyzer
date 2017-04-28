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
                    var result = linter(data.config, data.files);

                    res.writeHead(200, { 'Content-Type': 'application/json' });
                    res.write(JSON.stringify(result));
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

    tslint: function (configFile, files) {
        var tslint = require("tslint");
        var options = {
            formatter: "json",
            configuration: JSON.parse(fs.readFileSync(configFile, "utf8").trim())
        };

        var results = [];

        for (var i = 0; i < files.length; i++) {
            var file = files[i];
            var ll = new tslint(file, fs.readFileSync(file, "utf8"), options);
            results = results.concat(JSON.parse(ll.lint().output));
        }

        return results;
    }

};

start(process.argv[2]);