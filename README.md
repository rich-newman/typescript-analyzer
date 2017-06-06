## TypeScript Analyzer

An extension to Visual Studio 2017 that runs TSLint on TypeScript files.  

### Background

This is a version of Mads Kristensen's [Web Analyzer for Visual Studio 2015](https://visualstudiogallery.msdn.microsoft.com/6edc26d4-47d8-4987-82ee-7c820d79be1d).  It has been upgraded to Visual Studio 2017.  Support for ESLint, CSSLint and CoffeeLint has been removed, as this is available in Visual Studio 2017 itself.  Only support for TSLint has been retained.

**Please refer to Mads Kristensen's [documentation for the Web Analyzer](https://github.com/madskristensen/WebAnalyzer) for a full list of the original features.**

### Menu Options: TypeScript Analyzer vs Web Code Analysis

The menu options for the TypeScript Analyzer are separate from the menu options for the ESLint, CSSLint and CoffeeLint in Visual Studio 2017 ('Web Code Analysis').  In particular the TypeScript Analyzer can be run for a specific file or files by right-clicking in Solution Explorer and selecting 'Run TypeScript Analyzer'.  Settings can be edited using 'Tools/TypeScript Analyzer/Edit TSLint settings'.  TypeScript Analyzer also has its own entry in the menu in Tools/Options.

### TSLint Version

The TypeScript Analyzer has been upgraded to use TSLint version 5.4.2.

### Fix TypeScript Analyzer Errors

The TypeScript Analyzer puts a menu option on the context menu in Solution Explorer that will attempt to fix linting errors in the file or files clicked on.  This uses TSLint's 'fix' option.  It updates files in place on the hard drive.  That is, it overwrites them immediately, so use this option with care.  It also can only fix errors for which fixers have been written.  Please refer to the TSLint documentation for more details.

### Run on Build

There is a 'Run on build' option under Tools/Options/TypeScript Analyzer.  By default this is set to false.  If it is true the analyzer will run before any build and report linting errors in the files being built.

If, additionally, 'Show errors' is set to true then the analyzer will fail a build if it finds any errors when linting the build files.  For these purposes 'errors' are anything displayed as errors in the Error List Window.  See the section on Errors/Warnings below.

### Default tslint.json

The TypeScript Analyzer has a default tslint.json file.  This is used on initial install, or if it's reset with 'Tools/TypeScript Analyzer/Reset TypeScript Analyzer Settings'.  It can be overridden by including your own tslint.json in a project, or by editing it with 'Tools/TypeScript Analyzer/Edit TSLint settings'.

The current default tslint.json contains all of the recommended rules and settings for TSLint 5.2.0.    All of the rules have been added individually, rather than use the simpler 'extends' syntax that the file supports ("extends": "tslint:recommended").  This is because it's easier to enable or disable a rule without referring to the documentation in this format.  This also allows you to more easily set the severity of an individual rule (see below).  You are of course free to change your tslint.json to use the 'extends' syntax.

**If you installed an earlier version of the TypeScript Analyzer than 1.2 then you will need to reset your tslint.json to enable the new rules** ('Tools/TypeScript Analyzer/Reset TypeScript Analyzer Settings').

### TSLint Errors/Warnings and defaultSeverity

TSLint now has its own errors and warnings.  However TSLint 'errors' are different from the usual errors in Visual Studio in the sense that they are not serious problems that will halt a build.  Trailing whitespace is by default an 'error' in TSLint for example.

As a result all TSLint errors and warnings are by default displayed as warnings in the Visual Studio Error List.

However, it is possible to show TSLint errors as errors in the Error List, and TSLint warnings as warnings.  To do this go to the TypeScript Analyzer section in Tools/Options and set 'Show errors' to true.

If 'Show errors' is enabled you can configure individual rules to be errors or warnings using tslint.json.  You can change the default severity level for all rules from 'error' with "defaultSeverity": "warn" at the top level.  You can change the level of individual rules by adding "severity": "warn" to the rule at the same level as "options".  This is [documented on the tslint website](https://palantir.github.io/tslint/usage/configuration/).