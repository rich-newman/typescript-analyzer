// A '.tsx' file enables JSX support in the TypeScript compiler, 
// for more information see the following page on the TypeScript wiki:
// https://github.com/Microsoft/TypeScript/wiki/JSX

//import * as React from 'react';
//import * as ReactDOM from 'react-dom';

/// <reference path="../../react.d.ts" />
/// <reference path="../../react-dom.d.ts" />

class MyTsx {
    sayHello() {
        ReactDOM.render(
            <h1> Hello tsx World! </h1>,
            document.getElementById('wrapper')
        );
    }
}