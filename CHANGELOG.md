# Changelog

These are the changes to each version that have been released
on the official Visual Studio extension gallery.

## 1.21

**2020-01-09**

- Underlying TSLint upgraded to 5.20.1, TypeScript 3.7.4.

## 1.20

**2019-08-12**

- Underlying TSLint upgraded to 5.18.0, TypeScript 3.5.3.

## 1.19

**2019-05-31**

- Underlying TSLint upgraded to 5.17.0, TypeScript 3.5.1.
- Upgrade so will work in Visual Studio 2019 as well as Visual Studio 2017.
- As part of upgrade, make launch of extension asynchronous.  This will improve Visual Studio startup time.  The active open .ts file in the solution should be linted on solution open more reliably than previously.  Inactive open .ts files are not linted at startup (they never were).
- Install node files as part of extension install, rather than doing an npm install on the first run (or sooner) of the extension.  This makes the VSIX download slightly larger, but again means Visual Studio startup is less impacted by the extension.
- Fix unit tests so they work with the two versions of Visual Studio.  Note that Visual Studio 2017 has to be installed on the machine for the tests to work at all.  The tests should run reliably if the solution is opened in Visual Studio 2017.  They will also run in Visual Studio 2019 if Visual Studio 2017 is installed, but can get confused about library references (a 'Run All' can fail for certain tests).

## 1.18

**2019-02-03**

- Underlying TSLint upgraded to 5.13.1, TypeScript 3.3.3333.

## 1.17

**2018-08-08**

- Underlying TSLint upgraded to 5.11.0, TypeScript 3.0.1.
- Support for linting .js and .jsx files added, controlled by 'Lint .js and .jsx files' flag in Tools/Options/TypeScript Analyzer.

## 1.16

**2018-07-05**

- Underlying TSLint upgraded to 5.10.0, TypeScript 2.9.2.
- Improved error handling when the call to TSLint fails.  Messages are placed in the Error List as well as under TypeScript Analyzer in the Output window.  Also improved handling of status bar messages slightly.
- Increased timeouts for synchronous calls made on a build to the Analyzer, particularly for local ng lint calls.  Stopped timeouts crashing the extension.

Thanks to @lukeowlclaw for the Error List improvements.

## 1.15

**2018-04-19**

- Underlying TSLint upgraded to 5.9.1, TypeScript 2.8.1.

## 1.14

**2017-11-29**

- Underlying TypeScript upgraded to TypeScript 2.6.2.
- Fixed bug causing intermittent errors at startup if multiple code windows are open (root cause of error in [#23](https://github.com/rich-newman/typescript-analyzer/issues/23)).
- Fixed bug that could cause same file to be linted multiple times if multiple code windows open at startup.

## 1.13

**2017-10-30**

- Underlying TSLint upgraded to 5.8.0, TypeScript 2.5.3.
- Better exception handling for errors when cleaning the Error List ([#23](https://github.com/rich-newman/typescript-analyzer/issues/23)).
- Tried to make it clearer that this extension needs a Visual Studio Solution: it will not work in a folder view.

## 1.12

**2017-09-23**

- Bugfix release only.  No new functionality.
- Fixed bug causing error when the same file is linked into two Visual Studio projects and has errors ([#20](https://github.com/rich-newman/typescript-analyzer/issues/20)).
- Upgraded all Nuget Visual Studio and Newtonsoft NuGet dependencies to latest, some refactoring.

## 1.11

**2017-09-03**

- Underlying TSLint upgraded to 5.7.0, TypeScript 2.5.2.

## 1.10

**2017-08-22**

- Fixed bug in 'Use local ng lint' option when combined with the 'Run on build' and 'Show errors' options.  Previously the build was not halting if there were errors.
- Provided feedback in the Output window under TypeScript Analyzer dropdown when an attempt to use local ng lint is made.  You get a message saying whether it succeeded or failed and why.

## 1.9

**2017-08-15**

- Underlying TSLint upgraded to 5.6.0, TypeScript 2.4.2.
- Fixed the web links for codelyzer/angular errors in the Error List so you can click in the 'Code' column and go to the appropriate web page.
- Added a 'Use local ng lint' option in Tools/Options/TypeScript Analyzer.  This allows codelyzer linting for Angular CLI projects with older versions of codelyzer.

Many thanks to Chris Plewright (@zewpo) for these enhancements.

## 1.8

**2017-08-07**

- Bugfix release: fix bug when linting unloaded projects, defend better against exceptions when iterating projects and project items.  No new functionality.

## 1.7

**2017-07-29**

- Optimization release only.  No new functionality.
- Fixed: analysis was slow when analyzing a solution with a very large number of files (tens of thousands) in a complex folder structure (thousands of folders).  This was the case even if the files were not in the solution, and even if the folder was being ignored in Tools/Options.  The most common scenario where this could occur was with a complex node_modules folder and **not** using tsconfig.json files.
- Fixed: the analyzer could hang up Visual Studio trying to update the Error List with a large number of errors (thousands) if these were not in a project. It was repeatedly scanning the entire solution hierarchy to try to find the project name for each file.  The most common scenario where this could happen was including a complex node_modules folder in a tsconfig.json file in error, even if it was not in the solution. Note that if there are thousands of errors the analyzer is still not what you would call speedy: with 3000 errors in a node_modules folder with 20,000 files it is taking about 8 seconds to lint, parse and display the results on my not-very-fast machine.
- Improved general speed for ignored items in Tools/Options/TypeScript Analyzer/Ignore patterns.

## 1.6

**2017-07-15**

- Underlying TSLint upgraded to 5.5.0, TypeScript 2.4.1.
- Option to use tsconfig.json files for linting added to Tools/Options ([#11](https://github.com/rich-newman/typescript-analyzer/issues/11)).  If this is true the analyzer passes tsconfig.json files to tslint, rather than individual TypeScript files, which allows the [tslint 'semantic' rules](https://palantir.github.io/tslint/usage/type-checking/) to be used.  More details and the exact rules for how the analyzer finds tsconfig.json files are in the [readme](https://github.com/rich-newman/typescript-analyzer).
- Option to run the analyzer only if explicitly requested from the Solution Explorer added to Tools/Options ([#12](https://github.com/rich-newman/typescript-analyzer/issues/12)).  If true the default behavior of running the analyzer whenever a file is opened or saved is disabled.
- Files now only ever get analyzed if they are in a Visual Studio project in a loaded Visual Studio solution.  Previously a file might or might not get analyzed depending on how it was accessed if it was in the folder tree but not in a project.
- Introduced more robust error handling.
- Fixed bug with resetting options to default values ([#9](https://github.com/rich-newman/typescript-analyzer/issues/9)).
- The options on Tools/Options/TypeScript Analyzer screen are grouped more logically.
- Debug: Changes made to allow for simpler debugging of node calls.

## 1.5

**2017-06-24**

- Underlying TSLint upgraded to 5.4.3.
- Bugfixes for initialization when running a build with 'run on build' enabled.
- Improved exception handling for parsing of tslint results

## 1.4

**2017-06-06**

- Underlying TSLint upgraded to 5.4.2, TypeScript 2.3.4.
- Option to run the linter before any build added to Tools/Options.  This will stop the build if any TSLint errors show as Visual Studio errors in the Visual Studio Error List.  This can only happen if 'Show errors' is also set to true.
- Default tslint.json updated to comply with current schema.  This is the tslint.json made available on an initial install or on Tools/Reset TypeScript Analyzer Settings. 

## 1.3

**2017-05-20**

- Added menu option to allow TSLint to try to fix errors using its fixers
- Allowed linting on entire projects and solutions in Solution Explorer
- Message shown in status bar if try to run linting and it's disabled in Tools/Options


## 1.2

**2017-05-12**

- Underlying TSLint upgraded to version 5.2.0
- All recommended rules included individually in the default tslint.json, including new ones
- TSLint errors shown as errors in the Error List, warnings as warnings, but only if new Show Errors option in Tools/Options is set to true (default is false)
- ReadMe updated to discuss the issues above
- Can now use a tslint.json that has the 'extends' configuration property (#3)
- defaultSeverity can now be set in tslint.json.  TSLint errors and warnings will show correctly in the Error List if the new Show Errors option in Tools/Options is set to true (#1)

## 1.1

**2017-05-02**

- Correct links to help files

## 1.0

**2017-05-01**

- Initial upgraded version of Mad Kristensen's Web Analyzer
- ESLint, CSSLint, CoffeeLint removed, telemetry removed
-  Menu items for 'TypeScript Analyzer' added