(function () {
    'use strict';
    var Range = ace.require('ace/range').Range;

    var methodTooltip;
    var change_timer;
    var lastBreakpointHit = null;
    var listObjects = {
        Classes: [],
        Tables: [],
        Edts: [],
        Enums: [],
        GlobalFunctions: []
    }

    const knownTypes = ["var", "anytype", "date", "int", "int64", "date", "utcdatetime", "timeofday", "boolean", "container"];

    const themes = {
        "XCode": "xcode",
        "Eclipse": "eclipse",
        "GithubLightDefault": "github_light_default",
        "TextMate": "textmate",
        "Chrome": "chrome",
        "Kuroir": "kuroir",
        "Ambiance": "ambiance",
        "Dracula": "dracula",
        "Cobalt": "cobalt",
        "Terminal": "terminal",
        "GithubDark": "github_dark"
    };

    var removeMarker = function(editor, fromLine, fromPosition, toLine, toPosition, className) {
        const prevMarkers = editor.session.getMarkers();
        if (prevMarkers) {
            const prevMarkersArr = Object.keys(prevMarkers);
            for (let item of prevMarkersArr) {
                var marker = prevMarkers[item];
                if (marker.clazz !== className) continue;

                if (marker.range.start.row == fromLine &&
                    marker.range.start.column == fromPosition &&
                    marker.range.end.row == toLine &&
                    marker.range.end.column == toPosition) {
                    editor.session.removeMarker(marker.id);
                }
            }
        }
    }

    var removeLastBreakpointHitMarkerIfAny = function(editor) {
        if (lastBreakpointHit !== null) {
            removeMarker(
                editor,
                lastBreakpointHit.FromLine,
                lastBreakpointHit.FromPosition,
                lastBreakpointHit.ToLine,
                lastBreakpointHit.ToPosition,
                "breakpointHitMarked");

            lastBreakpointHit = null;
        }
    }

    var addBreakpointHitMarker = function(editor, breakpointHit) {

        // Remove last breakpointhit
        if (lastBreakpointHit !== null) {
            removeMarker(
                editor,
                lastBreakpointHit.FromLine,
                lastBreakpointHit.FromPosition,
                lastBreakpointHit.ToLine,
                lastBreakpointHit.ToPosition,
                "breakpointHitMarked");
        }

        lastBreakpointHit = breakpointHit;

        // Add hit and wait for the user
        editor.session.addMarker(new Range(
            breakpointHit.FromLine,
            breakpointHit.FromPosition,
            breakpointHit.ToLine,
            breakpointHit.ToPosition),
            "breakpointHitMarked",
            "text");
    }

    var addOrRemoveBreakpointMarker = function(editor, breakpoint) {
        if (breakpoint.Removed) {
            editor.session.clearBreakpoint(breakpoint.RemovedFromLine);
            removeMarker(
                editor,
                breakpoint.RemovedFromLine,
                breakpoint.RemovedFromPosition,
                breakpoint.RemovedToLine,
                breakpoint.RemovedToPosition,
                "breakpointMarker");
        }
        if (breakpoint.Created) {
            editor.session.setBreakpoint(breakpoint.FromLine);
            editor.session.addMarker(new Range(
                breakpoint.FromLine,
                breakpoint.FromPosition,
                breakpoint.ToLine,
                breakpoint.ToPosition),
                "breakpointMarker",
                "text");
        }
    }

    var clearErrors = function(editor) {

        editor.session.clearAnnotations();
        editor.session.clearBreakpoints();

        // Remove all markers
        const prevMarkers = editor.session.getMarkers();
        if (prevMarkers) {
            const prevMarkersArr = Object.keys(prevMarkers);
            for (let item of prevMarkersArr) {
                editor.session.removeMarker(prevMarkers[item].id);
            }
        }
    }

    var tokenMetadataCache = [];
    var getTokenFromCache = function (row, column) {
        return tokenMetadataCache.find((element) => element.Row == row && element.Column == column);
    }

    var showMethodToolTip = function (editor, self) {
        var TokenIterator = ace.require("ace/token_iterator").TokenIterator;
        var pos = editor.getCursorPosition();
        var stream = new TokenIterator(editor.session, pos.row, pos.column);
        var currentToken = stream.getCurrentToken();

        if (!currentToken || currentToken.type == 'string') return;

        var previousToken = stream.stepBackward();

        if ((currentToken.value == '(' && previousToken && (previousToken.type == 'identifier' || previousToken.type == 'keyword.other')) ||
            (currentToken.value == ',')) {
            clearTimeout(change_timer);

            $dyn.callFunction(self.GetTokenMetadata, self, { _line: pos.row, _position: currentToken.start + 1, _isMethodParameters: true }, function (ret) {
                if (ret !== null && typeof ret != 'undefined' && ret.DocHtml !== '') {
                    // Get client position
                    var position = editor.renderer.textToScreenCoordinates(pos.row, currentToken.start);
                    showTooTip(methodTooltip, position.pageX + 20, position.pageY, ret.DocHtml);
                }
            });
        }

    }

    var showTooTip = function(tooltip, x, y, message)
    {
        tooltip.setHtml(message);
        tooltip.show(null, x, y - 30);
    }

    $dyn.ui.defaults.MXTXppInterpreterCodeEditor = {};
    $dyn.controls.MXTXppInterpreterCodeEditor = function (data, element) {

        var self = this;

        $dyn.ui.Control.apply(self, arguments);
        $dyn.ui.applyDefaults(self, data, $dyn.ui.defaults.MXTXppInterpreterCodeEditor);

        var command = {
            name: "enter",
            bindKey: {
                mac: "Enter",
                win: "Enter"
            },
            exec: function () {
                editor.insert("\n");
            }
        }

        /// Setup completer
        ace.require("ace/ext/language_tools");
        var xppCompleter = {
            getCompletions: (editor, session, pos, prefix, callback) => {
                function getLocalVariables(editor, pos) {
                    var TokenIterator = ace.require("ace/token_iterator").TokenIterator;
                    var stream = new TokenIterator(editor.session, pos.row, pos.column);

                    let lastIdentifier = false;
                    let lastToken = null;
                    var token = stream.getCurrentToken();
                    var currentScope = 0;
                    var localVariables = [];
                    function match(t, v) {
                        if (v instanceof Array)
                            return t && t.type !== 'string' && t.type !== 'comment' && v.includes(t.value);
                        return t && t.type !== 'string' && t.type !== 'comment' && t.value === v;
                    }

                    function matchType(t, types) {
                        return t && types.includes(t.type);
                    }

                    while (token) {
                        token = stream.stepBackward();
                        if (token == null || token.type === 'text') continue;

                        // Detect scopes
                        if (match(token, '}')) currentScope++;
                        else if (match(token, '{')) currentScope--;

                        if (currentScope <= 0) {
                            if (!lastIdentifier && !match(lastToken, '(') && token.type == 'identifier') {
                                lastIdentifier = true;
                            } else if (lastIdentifier) {
                                lastIdentifier = false;
                                if (matchType(token, ['identifier']) || match(token, knownTypes)) {
                                    localVariables.push({
                                        value: lastToken.value,
                                        name: lastToken.value,
                                        type: 'LocalVariable',
                                        score: 1
                                    });
                                }
                            }
                        }
                        lastToken = token;
                    }

                    return localVariables;
                }

                function shouldSkipCompleter(editor, token, previousToken, stream, trigger) {

                    if (!token || token.type == 'string' || token.type == 'comment') return true;

                    const isNumeric = (string) => /^[+-]?\d+(\.\d+)?$/.test(string);
                    var restoreState = false;
                    if (previousToken) {
                        if (previousToken.type == 'text') {
                            previousToken = stream.stepBackward();
                            restoreState = true;
                        }

                        if (previousToken &&
                            (previousToken.type == 'identifier' || knownTypes.includes(previousToken.value)) &&
                            !trigger.includes(':') &&
                            !trigger.includes('.')) {
                            return true;
                        }

                        if (restoreState) previousToken = stream.stepForward();
                        else if (isNumeric(previousToken.value)) return true;
                    }

                    return false;
                }

                var TokenIterator = ace.require("ace/token_iterator").TokenIterator;
                var stream = new TokenIterator(editor.session, pos.row, pos.column);

                var currentToken = stream.getCurrentToken();

                if (currentToken) {

                    var triggerChar = currentToken.value;
                    var previousToken = stream.stepBackward();

                    if (shouldSkipCompleter(editor, currentToken, previousToken, stream, triggerChar)) {
                        return callback(null, []);
                    }

                    if (previousToken && (previousToken.type == 'identifier' || previousToken.value.endsWith(')')) &&
                        (triggerChar.includes('::') || triggerChar.startsWith('.'))) {

                        clearTimeout(change_timer);
                        return $dyn.callFunction(self.GetAutocompletions, self, { _line: pos.row, _position: pos.column - 1, _staticCompletion: triggerChar.includes('::') }, function (ret) {
                            ret && callback(null, ret.Completions.map(function (c) {
                                return {
                                    value: c.Value,
                                    name: c.Value,
                                    type: c.Type,
                                    docHTML: c.DocHtml
                                };
                            }));
                        });
                    }
                    else if (previousToken && triggerChar.includes(':')) return callback(null, []);
                }

                callback(null, [
                    ...getLocalVariables(editor, pos),
                    ...listObjects.Classes,
                    ...listObjects.Tables,
                    ...listObjects.Enums,
                    ...listObjects.Edts,
                    ...listObjects.GlobalFunctions,
                    ...intrinsicCompleterList,
                    ...keywordsCompleterList])

            }
        }

        var editor = ace.edit("editor");
        // trigger autocomplete whenever . or : is pressed
        var Autocomplete = ace.require("ace/autocomplete").Autocomplete;
        editor.commands.on("afterExec", function (e) {
            // Always hide method tooltip if shown

            if (e.args !== ' ') methodTooltip.hide();

            if (e.command.name == "insertstring") {
                if (e.args == ':' || e.args == '.') {
                    if (!editor.completer) {
                        editor.completer = new Autocomplete();
                    }
                    editor.completer.showPopup(editor);
                } else if (e.args == '(' || e.args == ',') {
                    showMethodToolTip(editor, self);
                }
            }
        });

        editor.commands.addCommand(command);
        editor.setShowFoldWidgets(true);
        editor.setTheme(themes["ace/theme/" + $dyn.value(this.Theme)]);
        editor.setFontSize($dyn.value(this.FontSize));
        editor.session.setMode($dyn.value(this.Mode));
        editor.session.setValue($dyn.value(this.SourceCode));

        editor.completers = [xppCompleter];
        editor.completer = new Autocomplete();
        editor.completer.exactMatch = true;
        editor.completer.ignoreCaption = true;

        editor.$mouseHandler.$enableJumpToDef = true
        editor.setShowPrintMargin(false);
        editor.setOptions({
            behavioursEnabled: true,
            wrapBehavioursEnabled: true,
            enableBasicAutocompletion: true,
            enableLiveAutocompletion: true,
            cursorStyle: 'smooth'
        });
        
        // Setup hover tooltip
        var HoverTooltip = ace.require('./tooltip').HoverTooltip;
        var hoverToolTip = new HoverTooltip();

        hoverToolTip.$gatherData = function (last, editor) {
            
            var TokenIterator = ace.require('ace/token_iterator').TokenIterator;
            var stream = new TokenIterator(editor.session, last.$pos.row, last.$pos.column);
            var currentToken = stream.getCurrentToken();

            if (currentToken) {

                switch (currentToken.type) {
                    case 'keyword':
                        showTooTip(hoverToolTip, last.x, last.y, '(keyword) ' + '<span style="color:blue">' + currentToken.value + '</span>');
                        break;
                    case 'keyword.other':
                    case 'identifier':
                        $dyn.callFunction(self.GetTokenMetadata, self, { _line: last.$pos.row, _position: last.$pos.column, _isMethodParameters: false}, function (ret) {
                            
                            if (ret && ret.DocHtml !== '') {
                                showTooTip(hoverToolTip, last.x, last.y, ret.DocHtml);
                            }
                        });
                        break;
                    default:
                        // Label case
                        if (currentToken.type == 'string' && currentToken.value.startsWith('@')) {
                            $dyn.callFunction(self.GetTokenMetadata, self, { _line: last.$pos.row, _position: last.$pos.column, _isMethodParameters: false }, function (ret) {
                                
                                if (ret && ret.DocHtml !== '') {
                                    showTooTip(hoverToolTip, last.x, last.y, ret.DocHtml);
                                }
                            });
                        }
                }
            }
        }

        hoverToolTip.addToEditor(editor);

        // Retrieve metadata list
        $dyn.callFunction(this.GetMetadataElements, self, {}, function (ret) {
            listObjects = {
                Classes: ret.Classes.map(function (e) { return { value: e, name: e, type: 'Class' } }),
                Tables: ret.Tables.map(function (e) { return { value: e, name: e, type: 'Table' } }),
                Enums: ret.Enums.map(function (e) { return { value: e, name: e, type: 'Enum' } }),
                Edts: ret.Edts.map(function (e) { return { value: e, name: e, type: 'Edt' } }),
                GlobalFunctions: ret.GlobalFunctions.map(function (e) { return { value: e, name: e, type: 'GlobalFunction' } }),
            };
        });

        var ToolTip = ace.require('./tooltip').Tooltip;
        methodTooltip = new ToolTip(editor.container);

        editor.clearSelection();

        editor.session.on('change', function (delta) {
            tokenMetadataCache = [];
            clearErrors(editor);
            var sc = editor.getValue();

            self.SourceCode(sc);

            // Set source code back to X++ control
            if (sc !== '') {
                clearTimeout(change_timer);
                change_timer = setTimeout(function () {
                    $dyn.callFunction(self.Parse, self, {}, function (value) {
                        if (value !== null && typeof value !== 'undefined') {
                            editor.session.setAnnotations([{
                                row: value.Line,
                                column: value.Column,
                                text: value.Name,
                                type: "error"
                            }]);
                        }
                    });
                }, 400);
            }
        });

        // Add / Remove breakpoint event
        editor.on("guttermousedown", function (e) {
            var target = e.domEvent.target;

            if (target.className.indexOf("ace_gutter-cell") == -1 || !editor.isFocused())
                return;

            if (e.clientX > 25 + target.getBoundingClientRect().left)
                return;

            var row = e.getDocumentPosition().row;
            var column = e.getDocumentPosition().column;

            $dyn.callFunction(self.AddOrRemoveBreakpoint, self, { _line: row, _position: column }, function (ret) {
                if (ret !== null && typeof ret !== 'undefined')
                    addOrRemoveBreakpointMarker(editor, ret);
            });

            e.stop();
        });

        $dyn.observe(this.Enabled, function (value) {
            editor.setReadOnly(!value);
        });

        $dyn.observe(this.Theme, function (value) {
            if (value !== null && typeof value != 'undefined') {
                editor.setTheme("ace/theme/" + themes[value]);
            }
        });

        $dyn.observe(this.FontSize, function (value) {
            if (value !== null && typeof value != 'undefined') {
                editor.setFontSize(value);
            }
        });

        $dyn.observe(this.ParseError, function (value) {
            if (value !== null) {
                editor.session.setAnnotations([{
                    row: value.Line,
                    column: value.Column,
                    text: value.Name,
                    type: "error" // also warning and information
                }]);
            }
        });

        $dyn.observe(this.BreakpointHit, function (value) {
            if (value !== null && typeof value !== 'undefined') {
                addBreakpointHitMarker(editor, value);
                editor.focus();
                editor.gotoLine(value.ToLine + 1, value.FromPosition);
            }
        });

        $dyn.observe(this.ExecutionPhase, function (value) {
            clearTimeout(change_timer);

            if (value === 2) {
                removeLastBreakpointHitMarkerIfAny(editor);
                editor.session.clearAnnotations();
            }
        });
    }

    $dyn.controls.MXTXppInterpreterCodeEditor.prototype = $dyn.extendPrototype($dyn.ui.Control.prototype, {
        init: function (data, element) {
            var self = this;
            $dyn.ui.Control.prototype.init.apply(this, arguments);
        },
        Shortcuts: {
            Execute: {
                Keys: { ctrl: true, keyCode: $dyn.ui.KeyCodes.letterE },
                Handler: function (evt) {
                    $dyn.callFunction(this.Execute, this, {});
                },
                Category: "Code",
                Description: "Executes the code that is currently on the editor"
            },
            AddOrRemoveBreakpoint: {
                Keys: { ctrl: true, keyCode: $dyn.ui.KeyCodes.f9 },
                Handler: function (evt) {
                    var editor = ace.edit("editor");

                    var row = editor.getSelectionRange().start.row;
                    var column = editor.getSelectionRange().start.column;
                    $dyn.callFunction(this.AddOrRemoveBreakpoint, this, { _line: row, _position: column }, function (ret) {
                        if (ret !== null && typeof ret !== 'undefined') {
                            addOrRemoveBreakpointMarker(editor, ret);
                        }
                    });
                },
                Category: "Code",
                Description: "Adds or removes a breakpoint"
            },
            StepOver: {
                Keys: { ctrl: true, keyCode: $dyn.ui.KeyCodes.f10 },
                Handler: function (evt) {
                    $dyn.callFunction(this.StepOverExecution, this, {});
                },
                Category: "Code",
                Description: "Step over to the next instruction when debugging"
            },
            Continue: {
                Keys: { ctrl: true, keyCode: $dyn.ui.KeyCodes.f8 },
                Handler: function (evt) {
                    $dyn.callFunction(this.ContinueExecution, this, {});
                },
                Category: "Code",
                Description: "Continue to the next breakpoint when debugging"
            }
        },
        keydown: function (event) {
            $dyn.util.handleShortcuts(this, event);
        }
    });

})();