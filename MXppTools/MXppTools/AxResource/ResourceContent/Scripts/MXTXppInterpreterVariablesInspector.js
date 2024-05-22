(function () {
    'use strict';

    Globalize.addCultureInfo("en", {
        messages: {
            Name: "Name",
            Value: "Value",
            Type: "Type"
        },
    });

    Globalize.addCultureInfo("es", {
        messages: {
            Name: "Nombre",
            Value: "Valor",
            Type: "Tipo"
        },
    });


    function removeAllEntries() {
        $("#inspector").find("tr:gt(0)").remove();
    }

    function contructEntryRow(entry) {
        var element = "<div";

        if (entry.Editable) {
            element += " contenteditable";
        }

        var color = entry.HasChanged ? "red" : "black";

        element += " style =\"color: " + color + "\">";
        element += entry.Value;
        element += "</div>";
        return element;
    }

    function arrangeInspectorEntries(self, entries) {

        removeAllEntries();

        var table = document.getElementById("inspector");

        let i = 0;
        while (i < entries.length) {
            var entry = entries[i];
            var row = table.insertRow();

            row.insertCell(0).innerHTML = entry.Name;
            row.insertCell(1).innerHTML = contructEntryRow(entry);
            row.insertCell(2).innerHTML = entry.Type;

            i++;
        }

        document.querySelectorAll('td div')
            .forEach(e => e.addEventListener("focusout", function () {
                var div = this;
                var variableName = this.closest('tr').querySelector('td').innerHTML;

                $dyn.callFunction(self.TryUpdateVariableValue, div, { _variableName: variableName, _value: this.innerHTML }, function (ret) {
                    if (ret !== null && typeof ret !== 'undefined') {
                        div.innerHTML = ret.Value;
                        div.style.color = "red";
                        if (ret.Error !== '') {
                            alert(ret.Error);
                        }
                    }
                });
            }));
    }
    
    $dyn.ui.defaults.MXTXppInterpreterVariablesInspector = {};
    $dyn.controls.MXTXppInterpreterVariablesInspector = function (data, element) {

        var self = this;

        $dyn.ui.Control.apply(self, arguments);
        $dyn.ui.applyDefaults(self, data, $dyn.ui.defaults.MXTXppInterpreterVariablesInspector);

        $dyn.observe(this.InspectVariables, function (root) {
            if (root !== null && typeof root !== 'undefined') {
                arrangeInspectorEntries(self, root.Subentries);
            }
        });

        $dyn.observe(this.ExecutionPhase, function (value) {
            if (value === 2) {
                removeAllEntries(editor);
            }
        });
    };

    $dyn.controls.MXTXppInterpreterVariablesInspector.prototype = $dyn.extendPrototype($dyn.ui.Control.prototype, {
        init: function (data, element) {
            var self = this;
            $dyn.ui.Control.prototype.init.apply(this, arguments);
        }
    });

})();