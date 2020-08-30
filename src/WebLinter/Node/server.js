'use strict';

const http = require("http"),
    fs = require("fs"),
    path = require("path");

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

        req.on('end', async function () {
            var debug = false;
            try {
                if (body === "")
                    return;

                if (path === "eslint" || path === "tslint") {
                    var data = JSON.parse(body);
                    debug = data.debug;
                    var result = path === "eslint" ?
                        await linteslint(data.config, data.fixerrors, data.files, data.usetsconfig, data.debug) :
                        linttslint(data.config, data.fixerrors, data.files, data.usetsconfig, data.debug);
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

function linttslint(configFile, fixErrors, files, usetsconfig, debug) {
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
        return tslinttsconfigs(configFile, files, options, debug);
    } else {
        return tslintFiles(configFile, files, options, debug);
    }
}

async function linteslint(configFile, fixErrors, files, usetsconfig, debug) {
    if (debug) {
        console.log('Config file: ' + configFile);
        console.log('Try to fix: ' + fixErrors);
        console.log('Files to lint:');
        files.forEach(f => console.log(f));
        console.log('Use TSConfig: ' + usetsconfig);
        //console.log(process.versions);
        console.log('__dirname: ' + __dirname);
    }
    if (usetsconfig) {
        return await eslinttsconfigs(configFile, files, fixErrors, debug);
    } else {
        return await eslintFiles(configFile, files, fixErrors, debug);
    }
}

async function eslintFiles(eslintConfigFile, files, fixErrors, debug) {
    const results = await calleslint(eslintConfigFile, files, fixErrors, debug, {});
    return JSON.stringify(results);
}

async function eslinttsconfigs(eslintConfigFile, tsconfigFiles, fixErrors, debug) {
    let allResults = [];
    for (const tsconfigFile of tsconfigFiles) {
        const parserOptions = {
            tsconfigRootDir: path.dirname(tsconfigFile),
            project: [tsconfigFile],
            sourceType: "module"
        };
        const files = gettsconfigContents(tsconfigFile, debug);
        const results = await calleslint(eslintConfigFile, files, fixErrors, debug, parserOptions);
        allResults = allResults.concat(results);
    }
    return JSON.stringify(allResults);
}

function gettsconfigContents(tsconfigFile, debug) {
    // The replace deals with the node bollocks of leaving the BOM in there
    const contents = fs.readFileSync(tsconfigFile, "utf8").replace(/^\uFEFF/, '');
    const jsonContents = JSON.parse(contents);
    const tsconfigFilePath = path.dirname(tsconfigFile);
    const typescript = require("typescript");
    const tsResult = typescript.parseJsonConfigFileContent(jsonContents, typescript.sys, tsconfigFilePath);
    //if (debug) {
    //    console.log("tsconfig.json contents:");
    //    console.log(tsResult);
    //}
    return tsResult.fileNames || [];
}

async function calleslint(eslintConfigFile, files, fixErrors, debug, parserOptions) {
    const { ESLint } = require("eslint");
    const configData = {
        parser: __dirname + '\\node_modules\\@typescript-eslint\\parser\\dist\\index.js',
        plugins: ['@typescript-eslint'],
        parserOptions: parserOptions
    };
    const options = {
        overrideConfigFile: eslintConfigFile,
        overrideConfig: configData,
        resolvePluginsRelativeTo: __dirname,
        extensions: [".ts"]
    };
    if (fixErrors) {
        options.fix = true;
    }
    const ignoreFile = path.join(path.dirname(eslintConfigFile), '.eslintignore');
    if (fs.existsSync(ignoreFile)) {
        options.ignorePath = ignoreFile;
        if (debug) {
            console.log('Ignore file set: ' + ignoreFile);
        }
    }

    const eslint = new ESLint(options);
    //if (debug  && files.length > 0) {
    //    const config = await eslint.calculateConfigForFile(files[0]);
    //    console.log(config);
    //}

    const results = await eslint.lintFiles(files);
    if (debug) {
        const formatter = await eslint.loadFormatter("stylish");
        const prettyResult = formatter.format(results);
        console.log(prettyResult);
    }
    if (fixErrors) {
        if (debug) {
            console.log('Fixing errors');
        }
        await ESLint.outputFixes(results);
    }
    return results;
}

function tslintFiles(configFile, files, options, debug) {
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

function tslinttsconfigs(tslintConfigFile, tsconfigFiles, options, debug) {
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
