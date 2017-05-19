# Changelog

These are the changes to each version that have been released
on the official Visual Studio extension gallery.

## 1.3

**2017-05-20**

- Add menu option to allow TSLint to try to fix errors using its fixers
- Allow linting on entire projects and solutions in Solution Explorer
- Give message in status bar if try to run linting and it's disabled in Tools/Options


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