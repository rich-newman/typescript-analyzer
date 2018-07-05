## TypeScript Analyzer

An extension to Visual Studio 2017 that runs TSLint on TypeScript files. 

To install visit [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=RichNewman.TypeScriptAnalyzer) or search for 'TypeScript Analyzer' in Extensions and Updates in Visual Studio 2017.

### Background

This is a version of Mads Kristensen's [Web Analyzer for Visual Studio 2015](https://visualstudiogallery.msdn.microsoft.com/6edc26d4-47d8-4987-82ee-7c820d79be1d).  It has been upgraded to Visual Studio 2017.  Support for ESLint, CSSLint and CoffeeLint has been removed, as this is available in Visual Studio 2017 itself.  Only support for TSLint has been retained.

**Please refer to Mads Kristensen's [documentation for the Web Analyzer](https://github.com/madskristensen/WebAnalyzer) for a full list of the original features.**

**Please also be aware that, as with the Web Analyzer for Visual Studio 2015, at present this extension needs a Visual Studio solution file to work.  If you open a folder in Visual Studio without a solution the menu options will not appear and the TypeScript Analyzer will not run.**

### Menu Options: TypeScript Analyzer vs Web Code Analysis

The menu options for the TypeScript Analyzer are separate from the menu options for the ESLint, CSSLint and CoffeeLint in Visual Studio 2017 ('Web Code Analysis').  In particular the TypeScript Analyzer can be run for a specific file or files by right-clicking in Solution Explorer and selecting 'Run TypeScript Analyzer'.  Settings can be edited using 'Tools/TypeScript Analyzer/Edit TSLint settings'.  TypeScript Analyzer also has its own entry in the menu in Tools/Options.

### TSLint Version

The TypeScript Analyzer is using TSLint version 5.10.0.

### Analyze Using tsconfig.json

By default the TypeScript Analyzer hands individual .ts and .tsx files to tslint for linting.  However, there is an option to use tsconfig.json files instead, Tools/Options/TypeScript Analyzer/Use tsconfig.json files.

If this option is set to true then the TypeScript Analyzer only ever passes tsconfig.json files found in the Visual Studio solution to tslint.  tslint will lint the files found in those tsconfig.jsons.

Also if this option is set then tslint can use the additional [tslint 'semantic' rules](https://palantir.github.io/tslint/usage/type-checking/).  These require a program object to be created, which can only be done from a tsconfig.json file.  These semantic rules are tagged with 'Requires Type Info' on the [tslint rules page](https://palantir.github.io/tslint/rules/).

##### Rules For Finding tsconfig.json Files

The rules around how the TypeScript Analyzer finds tsconfig.json files to be passed to tslint are a little complicated.  For completeness full details are below. 

- The TypeScript Analyzer can be run for one tsconfig.json by right-clicking it in Solution Explorer and selecting 'Run TypeScript Analyzer'.  In this case just the one tsconfig.json file is passed to tslint.
- If the TypeScript Analyzer is run for the entire solution then the analyzer finds all tsconfig.json files in any project in the solution and hands them all to tslint.  Note that the tsconfig.json files have to be included in the Visual Studio project, not just in the same folder.
- If the TypeScript Analyzer is run for an individual Visual Studio project then the analyzer finds all tsconfig.json files in that project.
- The TypeScript Analyzer can be run for an individual .ts or .tsx file, either from Solution Explorer or by opening or saving a file.  In this case the analyzer tries to find the nearest associated tsconfig.json file.  If there is one in the same folder it will use that, otherwise it will search up the folder tree looking in each parent folder until it finds one.  If no tsconfig.json file is found no linting takes place.  Linting results are then filtered so that only errors for the original file are displayed, rather than all errors from files in the tsconfig.json.
- The TypeScript Analyzer can also be run for a folder in a project.  In this case the same rules as for an individual .ts or .tsx file are applied: the analyzer looks in the folder for a tsconfig.json and hands that to the linter if it finds it.  Otherwise it searches up the folder tree.  No filtering is applied to the results.
- If more than one item is selected then the rules above are applied and a union of all tsconfig.jsons found is passed to tslint.  Results are only filtered to the individual files if **all** files are individual .ts or .tsx files.

It's clearly possible in some of the scenarios above that one TypeScript file might be included in more than one tsconfig.json passed to tslint.  If there are any exact duplicate errors in the results then only one copy of the error is shown in the error list.

Note that if 'Use tsconfig.json files' is true then the options 'Ignore nested files' and 'Ignore patterns' apply to the discovery of tsconfig.json files, not the files that are linted.

### Fix TypeScript Analyzer Errors

The TypeScript Analyzer puts a menu option on the context menu in Solution Explorer that will attempt to fix linting errors in the file or files clicked on.  This uses TSLint's 'fix' option.  It updates files in place on the hard drive.  That is, it overwrites them immediately, so use this option with care.  It also can only fix errors for which fixers have been written.  Please refer to the TSLint documentation for more details.

### Run on Build

There is a 'Run on build' option under Tools/Options/TypeScript Analyzer.  By default this is set to false.  If it is true the analyzer will run before any build and report linting errors in the files being built.

If, additionally, 'Show errors' is set to true then the analyzer will fail a build if it finds any errors when linting the build files.  For these purposes 'errors' are anything displayed as errors in the Error List Window.  See the section on Errors/Warnings below.

### Default tslint.json

The TypeScript Analyzer has a default tslint.json file.  This is used on initial install, or if it's reset with 'Tools/TypeScript Analyzer/Reset TypeScript Analyzer Settings'.  It can be overridden by including your own tslint.json in a project, or by editing it with 'Tools/TypeScript Analyzer/Edit TSLint settings'.

The current default tslint.json contains all of the recommended rules and settings for TSLint 5.2.0.    All of the rules have been added individually, rather than use the simpler 'extends' syntax that the file supports ("extends": "tslint:recommended").  This is because it's easier to enable or disable a rule without referring to the documentation in this format.  This also allows you to more easily set the severity of an individual rule (see below).  You are of course free to change your tslint.json to use the 'extends' syntax.

### TSLint Errors/Warnings and defaultSeverity

TSLint now has its own errors and warnings.  However TSLint 'errors' are different from the usual errors in Visual Studio in the sense that they are not serious problems that will halt a build.  Trailing whitespace is by default an 'error' in TSLint for example.

As a result all TSLint errors and warnings are by default displayed as warnings in the Visual Studio Error List.

However, it is possible to show TSLint errors as errors in the Error List, and TSLint warnings as warnings.  To do this go to the TypeScript Analyzer section in Tools/Options and set 'Show errors' to true.

If 'Show errors' is enabled you can configure individual rules to be errors or warnings using tslint.json.  You can change the default severity level for all rules from 'error' with "defaultSeverity": "warn" at the top level.  You can change the level of individual rules by adding "severity": "warn" to the rule at the same level as "options".  This is [documented on the tslint website](https://palantir.github.io/tslint/usage/configuration/).

### Only Run if Requested

There is an 'Only run if requested' option on Tools/Options/TypeScript Analyzer.  If this is set to true then the analyzer will only run if explicitly requested with 'Run TypeScript Analyzer' from the Solution Explorer context menu, or on a build if 'Run on Build' is also true.  This means we disable the default behavior of running the analyzer whenever an individual .ts or .tsx file is opened or saved.

### codelyzer

A locally installed instance of [codelyzer](http://codelyzer.com/) will work with the TypeScript Analyzer.  However, the analyzer ships with its own versions of tslint and typescript. It runs these from a temporary folder.  Hence for codelyzer to work it's best to install it locally along with the same versions, currently tslint 5.10.0 and typescript 2.9.2.  It will usually also work with other compatible versions.  However, we know that versions of codelyzer before 3.0 are not compatible with these versions of tslint and typescript.  See below for an alternative.

### 'Use local ng lint' Option

This option on Tools/Options/TypeScript Analyzer runs tslint locally for an [Angular CLI](https://cli.angular.io/) installation by issuing an 'ng lint' command from a hidden cmd.exe window in the project folder.  This is useful if your Angular project uses older versions of codelyzer (before 3.0).

**This option only works with versions of Angular CLI up to 1.7.4, and doesn't work with version 6.0.0 or later.**

This approach bypasses some of our existing infrastructure.  In particular it uses the local versions of tslint and typescript in the node_modules subfolder rather than the versions shipped with the analyzer.  It needs an [Angular CLI](https://cli.angular.io/) install to work.  It's also a little slower than our usual linting.  For the results to show up in the Error List the files to lint still have to be included in the Visual Studio project, or included in a regular tsconfig.json that is in the Visual Studio project with 'Use tsconfig.json files' set in the options.

If the call to 'ng lint' fails for any reason the analyzer falls back to using the prepackaged tslint.  At present we don't have good reporting if that happens.

### Debugging / developing

If you want to help enhancing TypeScript Analyzer, just ensure [node is installed](https://nodejs.org/en/download/), clone the repository and open the project with Visual Studio.  Set a breakpoint and start debugging project WebLinterVsix (F5).  It will open a new instance of Visual Studio in which you can make use of TypeScript Analyzer until your breakpoint will be hit.  Happy coding!  And don't forget to create a pull request!

