## TypeScript Analyzer

An extension to Visual Studio 2017 that runs TSLint on TypeScript files.  

This is a version of Mads Kristensen's [Web Analyzer for Visual Studio 2015](https://visualstudiogallery.msdn.microsoft.com/6edc26d4-47d8-4987-82ee-7c820d79be1d).  It has been upgraded to Visual Studio 2017.  Support for ESLint, CSSLint and CoffeeLint has been removed, as this is available in Visual Studio 2017 itself.  Only support for TSLint has been retained.

Please refer to Mads Kristensen's [documentation for the Web Analyzer](https://github.com/madskristensen/WebAnalyzer) for a full list of features.

Note that the menu options for the TypeScript Analyzer are separate from the menu options for the ESLint, CSSLint and CoffeeLint in Visual Studio 2017 ('Web Code Analysis').  In particular the TypeScript Analyzer can be run for a specific file or files by right-clicking in Solution Explorer and selecting 'Run TypeScript Analyzer'.  Settings can be edited using Tools/TypeScript Analyzer/Edit TSLint settings.  TypeScript Analyzer also has its own entry in the menu in Tools/Options.