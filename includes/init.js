﻿/*
* init.js
*
* By: Steven de Salas
* On: Sep 2013
* 
*
* Generates the core running environment for
* javascript code to execute under
* 
*/

(function(GLOBAL) {

    // Save imported params
    var API = {
        phantom: GLOBAL.phantom,
        trifle: GLOBAL.trifle,
        console: GLOBAL.console,
        window: GLOBAL.window
    };

    delete GLOBAL.phantom;
    delete GLOBAL.trifle;
    delete GLOBAL.console;
    delete GLOBAL.window;

    // Initialise window object
    var window = GLOBAL.window = {
        API: API.window,
        window: window,
        setTimeout: function(callback, ms) {
            console.xdebug('window.setTimeout(callback, ' + ms + ')');
            if (typeof callback === 'function' && typeof ms === 'number') {
                window.API.SetTimeout((new trifle.Callback(function() {
                    callback.call(window);
                })).id, ms);
            }
        },
        clearTimeout: function(id) {
            console.xdebug('window.clearTimeout(' + id + ')');
            if (typeof id === 'number') {
                window.API.ClearTimeout(id);
            }
        },
        setInterval: function(callback, ms) {
            console.xdebug('window.setInterval(callback, ' + ms + ')');
            if (typeof callback === 'function' && typeof ms === 'number') {
                window.API.SetInterval((new trifle.Callback(function() {
                    callback.call(window);
                })).id, ms);
            }
        },
        clearInterval: function(id) {
            console.xdebug('window.clearInterval(' + id + ')');
            if (typeof id === 'number') {
                window.API.ClearInterval(id);
            }
        }
    };

    // Apply to global
    GLOBAL.window = window;
    GLOBAL.setTimeout = window.setTimeout;
    GLOBAL.setInterval = window.setInterval;
    GLOBAL.clearTimeout = window.clearTimeout;
    GLOBAL.clearInterval = window.clearInterval;

    // Initialise phantom object
    var phantom = GLOBAL.phantom = {
        API: API.phantom,
        version: API.phantom.Version,
        libraryPath: API.phantom.LibraryPath,
        scriptName: API.phantom.ScriptName,
        outputEncoding: API.phantom.OutputEncoding,
        cookiesEnabled: API.phantom.CookiesEnabled,
        args: API.phantom.Args,
        exit: function(code) {
            return phantom.API.Exit(code || 0);
        },
        injectJs: function(filename) {
            return phantom.API.InjectJs(filename || '');
        }
    };


    // TrifleJS object
    var trifle = GLOBAL.trifle = {
        API: API.trifle,
        version: API.trifle.Version,
        wait: function(ms) {
            return API.trifle.Wait(ms || 0);
        }
    };

    // Console object
    var console = GLOBAL.console = {
        API: API.console,
        clear: function() {
            this.API.clear();
        },
        log: function() {
            this._do('log', arguments);
        },
        error: function() {
            this._do('error', arguments);
        },
        xdebug: function() {
            this._do('xdebug', arguments);
        },
        debug: function() {
            this._do('debug', arguments);
        },
        warn: function() {
            this._do('warn', arguments);
        },
        _do: function(method, args) {
            if (method) {
                switch (args.length) {
                    case 0:
                        this.API[method]("");
                        break;
                    case 1:
                        if (typeof args[0] === 'function') { args[0] = args[0].toString(); }
                        this.API[method](args[0]);
                        break;
                    default:
                        var params = [];
                        for (var i = 0; i < args.length; i++) {
                            if (typeof args[i] === 'function') { args[i] = args[i].toString(); }
                            params[i] = args[i];
                        }
                        this.API[method](params);
                        break;
                }
            }
        }
    };


    // Loading module framework
    // @see http://wiki.commonjs.org/wiki/Modules/1.1.1
    var exports = GLOBAL.exports = {
        webpage: function() {
            return new trifle.modules.WebPage();
        },
        fs: function() {
            return new trifle.modules.FileSystem();
        },
        system: function() {
            return new trifle.modules.System();
        }
    };

    // Defines require() method
    var require = GLOBAL.require = function(name) {

        if (!exports[name]) {
            console.error('require() -- Invalid module: ' + name);
            return;
        }

        var module = exports[name]();
        module.create = function() {
            return this;
        }

        return module;
    }


})(this);


