(function () {
    'use strict';

    var lastBreakpointHit = null;
    var Range = ace.require('ace/range').Range;

    function removeMarker(editor, fromLine, fromPosition, toLine, toPosition, className) {
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

    function removeLastBreakpointHitMarkerIfAny(editor) {
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

    function addBreakpointHitMarker(editor, breakpointHit) {

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

    function addOrRemoveBreakpointMarker(editor, breakpoint) {

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

    function clearErrors(editor) {

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
                editor.session.insert(editor.getCursorPosition(), "\n");
            }
        }

        var editor = ace.edit("editor");
        var change_timer;

        editor.commands.addCommand(command);
        editor.setShowFoldWidgets(true);
        editor.session.setMode($dyn.value(this.Mode));
        editor.session.setValue($dyn.value(this.SourceCode));
        editor.setShowPrintMargin(false);
        
        editor.clearSelection();

        editor.session.on('change', function (delta) {

            clearErrors(editor);
            var sc = editor.getValue();

            self.SourceCode(sc);

            // Set source code back to X++ contorl
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
                }, 600);
            }
        });

        // Add / Remove breakpoint event
        editor.on("guttermousedown", function (e) {
            var target = e.domEvent.target;

            if (target.className.indexOf("ace_gutter-cell") == -1) {
                return;
            }
            if (!editor.isFocused())
                return;

            if (e.clientX > 25 + target.getBoundingClientRect().left)
                return;

            var row = e.getDocumentPosition().row;
            var column = e.getDocumentPosition().column;

            $dyn.callFunction(self.AddOrRemoveBreakpoint, self, { _line: row, _position: column }, function (ret) {
                if (ret !== null && typeof ret !== 'undefined') {
                    addOrRemoveBreakpointMarker(editor, ret);
                }
            });

            e.stop();
        });

        $dyn.observe(this.Enabled, function (value) {
            editor.setReadOnly(!value);
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