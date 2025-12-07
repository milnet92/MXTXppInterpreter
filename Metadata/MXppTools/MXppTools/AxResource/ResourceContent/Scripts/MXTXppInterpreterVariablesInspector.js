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
        $("#inspector tbody").empty();
    }

    function insertChildrenRecursively(self, tbody, children, insertPosition, depth) {
        var totalRowsInserted = 0;

        for (let i = 0; i < children.length; i++) {
            var entry = children[i];
            var childRow = tbody.insertRow(insertPosition + totalRowsInserted);

            childRow.setAttribute('data-path', entry.Path);
            childRow.setAttribute('data-depth', depth);
            childRow.setAttribute('data-name', entry.Name);
            if (entry.EntryType) {
                childRow.setAttribute('data-entry-type', entry.EntryType);
            }

            if (entry.Expandable || (entry.Subentries && entry.Subentries.length > 0)) {
                childRow.setAttribute('data-has-children', 'true');

                // If subentries are already loaded, mark as loaded
                if (entry.Subentries && entry.Subentries.length > 0) {
                    childRow.setAttribute('data-children-loaded', 'true');
                    childRow.setAttribute('data-expanded', 'false');
                } else {
                    childRow.setAttribute('data-children-loaded', 'false');
                    childRow.setAttribute('data-expanded', 'false');
                }
            }

            childRow.insertCell(0).innerHTML = constructNameCell(entry, depth, '');
            childRow.insertCell(1).innerHTML = contructEntryRow(entry);
            childRow.insertCell(2).innerHTML = entry.Type;

            totalRowsInserted++;

            // Recursively insert nested subentries (but keep them hidden initially)
            if (entry.Subentries && entry.Subentries.length > 0) {
                var nestedRowsInserted = insertChildrenRecursively(self, tbody, entry.Subentries, insertPosition + totalRowsInserted, depth + 1);
                totalRowsInserted += nestedRowsInserted;

                // Hide the nested children initially
                hideChildRows(entry.Path);
            }
        }

        return totalRowsInserted;
    }

    function constructNameCell(entry, depth, searchTerm) {
        var indent = depth * 20;
        var element = "<div style='padding-left: " + indent + "px; display: flex; align-items: center;'>";

        if (entry.Expandable) {
            element += "<span class='expand-icon' style='cursor: pointer; margin-right: 5px; user-select: none;'>&#9654;</span>";
        } else {
            element += "<span style='margin-right: 5px; width: 10px; display: inline-block;'></span>";
        }

        var imgSrc = '/resources/images/ClassField_16x.png';

        if (entry.EntryType && (entry.EntryType == 1 || entry.EntryType == 11))
            imgSrc = '/resources/images/Class_16x.png';

        var iconHtml = "<img src='" + imgSrc + "' style='margin-right: 5px; width: 16px; height: 16px;' />";
        element += iconHtml;

        var displayName = entry.Name;
        if (searchTerm && searchTerm.length > 0) {
            var regex = new RegExp('(' + searchTerm.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'gi');
            displayName = displayName.replace(regex, '<span class="search-highlight">$1</span>');
        }

        element += "<span>" + displayName + "</span>";
        element += "</div>";
        return element;
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

    function insertEntryRows(self, table, entries, depth, searchTerm) {
        depth = depth || 0;
        searchTerm = searchTerm || '';

        for (let i = 0; i < entries.length; i++) {
            var entry = entries[i];
            var row = table.insertRow();

            row.setAttribute('data-path', entry.Path);
            row.setAttribute('data-depth', depth);
            row.setAttribute('data-name', entry.Name);
            if (entry.EntryType) {
                row.setAttribute('data-entry-type', entry.EntryType);
            }

            if (entry.Expandable) {
                row.setAttribute('data-has-children', 'true');
                row.setAttribute('data-expanded', 'false');
                row.setAttribute('data-children-loaded', 'false');
            }

            row.insertCell(0).innerHTML = constructNameCell(entry, depth, searchTerm);
            row.insertCell(1).innerHTML = contructEntryRow(entry);
            row.insertCell(2).innerHTML = entry.Type;
        }
    }

    function hideChildRows(parentPath) {
        var rows = document.querySelectorAll('#inspector tr[data-path]');
        rows.forEach(function (row) {
            var path = row.getAttribute('data-path');
            if (path && path.startsWith(parentPath + '.')) {
                row.style.display = 'none';
                // Reset the expanded state and icon for all descendants
                if (row.getAttribute('data-has-children') === 'true') {
                    row.setAttribute('data-expanded', 'false');
                    var icon = row.querySelector('.expand-icon');
                    if (icon) {
                        icon.innerHTML = '&#9654;';
                    }
                }
            }
        });
    }

    function showDirectChildren(parentPath) {
        var rows = document.querySelectorAll('#inspector tr[data-path]');
        rows.forEach(function (row) {
            var path = row.getAttribute('data-path');
            if (path && path.startsWith(parentPath + '.') && path.split('.').length === parentPath.split('.').length + 1) {
                row.style.display = '';
            }
        });
    }

    function loadChildrenAndExpand(self, row, callback) {
        var path = row.getAttribute('data-path');
        var childrenLoaded = row.getAttribute('data-children-loaded') === 'true';
        var icon = row.querySelector('.expand-icon');

        if (childrenLoaded) {
            // Children already loaded, just expand
            row.setAttribute('data-expanded', 'true');
            icon.innerHTML = '&#9660;';
            showDirectChildren(path);
            if (callback) callback();
        } else {
            // Need to load children from F&O
            var requestExpand = function () {
                $dyn.callFunction(self.RequestExpandVariable, row, { _path: path }, function (result) {
                    if (result !== null && typeof result !== 'undefined') {
                        // Result is a single entry, get its Subentries
                        var children = result.Subentries;

                        if (children && children.length > 0) {
                            // Insert the loaded children
                            var tbody = document.querySelector("#inspector tbody");
                            var currentRowIndex = Array.from(tbody.rows).indexOf(row);
                            var depth = parseInt(row.getAttribute('data-depth')) + 1;

                            var rowsInserted = insertChildrenRecursively(self, tbody, children, currentRowIndex + 1, depth);

                            // Attach event handlers to new rows
                            attachEventHandlers(self);

                            // Mark as loaded and expand
                            row.setAttribute('data-children-loaded', 'true');
                            row.setAttribute('data-expanded', 'true');

                            // Re-query the icon after attachEventHandlers (it might have been cloned)
                            var updatedIcon = row.querySelector('.expand-icon');
                            if (updatedIcon) {
                                updatedIcon.innerHTML = '&#9660;';
                            }

                            // Apply search filter if active
                            var searchInput = document.getElementById('inspector-search');
                            if (searchInput && searchInput.value) {
                                filterRows(searchInput.value);
                            }

                            if (callback) callback();
                        } else {
                            // No children, revert icon
                            icon.innerHTML = '&#9654;';
                            if (callback) callback();
                        }
                    } else {
                        // Error or null result, revert icon
                        icon.innerHTML = '&#9654;';
                        if (callback) callback();
                    }
                })
            };
            window.setTimeout(requestExpand, 0);
        }
    }

    function toggleExpand(self, row) {
        var hasChildren = row.getAttribute('data-has-children');
        if (hasChildren !== 'true') return;

        var isExpanded = row.getAttribute('data-expanded') === 'true';
        var path = row.getAttribute('data-path');
        var icon = row.querySelector('.expand-icon');

        if (isExpanded) {
            // Collapse
            row.setAttribute('data-expanded', 'false');
            icon.innerHTML = '&#9654;';
            hideChildRows(path);
        } else {
            // Load children and expand
            loadChildrenAndExpand(self, row);
        }
    }

    function filterRows(searchTerm) {
        var rows = document.querySelectorAll('#inspector tbody tr[data-path]');
        var lowerSearchTerm = searchTerm ? searchTerm.toLowerCase() : '';
        var visiblePaths = new Set();

        if (!searchTerm) {
            // No search term, remove highlighting and show rows based on collapsed state
            rows.forEach(function (row) {
                var depth = parseInt(row.getAttribute('data-depth'));
                var path = row.getAttribute('data-path');
                var nameCell = row.cells[0];
                var name = row.getAttribute('data-name');
                var hasChildren = row.getAttribute('data-has-children') === 'true';
                var indent = depth * 20;

                // Remove highlighting - reconstruct name cell
                var element = "<div style='padding-left: " + indent + "px; display: flex; align-items: center;'>";
                if (hasChildren) {
                    var icon = row.querySelector('.expand-icon');
                    var iconHtml = icon ? icon.innerHTML : '&#9654;';
                    element += "<span class='expand-icon' style='cursor: pointer; margin-right: 5px; user-select: none;'>" + iconHtml + "</span>";
                } else {
                    element += "<span style='margin-right: 5px; width: 10px; display: inline-block;'></span>";
                }

                // Add icon
                var iconAttr = row.getAttribute('data-entry-type');
                var imgSrc = '/resources/images/ClassField_16x.png';
                if (iconAttr && iconAttr == '1') {
                    imgSrc = '/resources/images/Class_16x.png';
                }
                element += "<img src='" + imgSrc + "' style='margin-right: 5px; width: 16px; height: 16px;' />";

                element += "<span>" + name + "</span></div>";
                nameCell.innerHTML = element;

                // Reattach event handlers after modifying innerHTML
                var newIcon = row.querySelector('.expand-icon');
                if (newIcon) {
                    var self = window.inspectorControlInstance;
                    newIcon.addEventListener('click', function (e) {
                        e.stopPropagation();
                        toggleExpand(self, row);
                    });
                }

                if (depth === 0) {
                    row.style.display = '';
                } else {
                    // Check if any parent is collapsed
                    var pathParts = path.split('.');
                    var shouldShow = true;
                    for (var i = 1; i < pathParts.length; i++) {
                        var parentPath = pathParts.slice(0, i).join('.');
                        var parentRow = document.querySelector('#inspector tbody tr[data-path="' + parentPath + '"]');
                        if (parentRow && parentRow.getAttribute('data-expanded') !== 'true') {
                            shouldShow = false;
                            break;
                        }
                    }
                    row.style.display = shouldShow ? '' : 'none';
                }
            });
            return;
        }

        // First pass: find all matching rows and their ancestors
        rows.forEach(function (row) {
            var name = row.getAttribute('data-name');
            var path = row.getAttribute('data-path');

            if (name.toLowerCase().indexOf(lowerSearchTerm) !== -1) {
                // This row matches, add it and all its parents
                visiblePaths.add(path);

                // Add all parent paths
                var pathParts = path.split('.');
                for (var i = 1; i < pathParts.length; i++) {
                    var parentPath = pathParts.slice(0, i).join('.');
                    visiblePaths.add(parentPath);
                }
            }
        });

        // Second pass: show/hide rows and apply highlighting
        rows.forEach(function (row) {
            var path = row.getAttribute('data-path');
            var name = row.getAttribute('data-name');
            var depth = parseInt(row.getAttribute('data-depth'));
            var hasChildren = row.getAttribute('data-has-children') === 'true';

            if (visiblePaths.has(path)) {
                row.style.display = '';

                // Apply highlighting to matching rows
                if (name.toLowerCase().indexOf(lowerSearchTerm) !== -1) {
                    var nameCell = row.cells[0];
                    var indent = depth * 20;
                    var regex = new RegExp('(' + searchTerm.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'gi');
                    var highlightedName = name.replace(regex, '<span class="search-highlight">$1</span>');

                    var element = "<div style='padding-left: " + indent + "px; display: flex; align-items: center;'>";
                    if (hasChildren) {
                        var icon = row.querySelector('.expand-icon');
                        var iconHtml = icon ? icon.innerHTML : '&#9654;';
                        element += "<span class='expand-icon' style='cursor: pointer; margin-right: 5px; user-select: none;'>" + iconHtml + "</span>";
                    } else {
                        element += "<span style='margin-right: 5px; width: 10px; display: inline-block;'></span>";
                    }

                    // Add icon
                    var iconAttr = row.getAttribute('data-entry-type');
                    var imgSrc = '/resources/images/ClassField_16x.png';
                    if (iconAttr && iconAttr == '1') {
                        imgSrc = '/resources/images/Class_16x.png';
                    }
                    element += "<img src='" + imgSrc + "' style='margin-right: 5px; width: 16px; height: 16px;' />";

                    element += "<span>" + highlightedName + "</span></div>";
                    nameCell.innerHTML = element;

                    // Reattach event handler
                    var newIcon = row.querySelector('.expand-icon');
                    if (newIcon) {
                        var self = window.inspectorControlInstance;
                        newIcon.addEventListener('click', function (e) {
                            e.stopPropagation();
                            toggleExpand(self, row);
                        });
                    }
                }
            } else {
                row.style.display = 'none';
            }
        });
    }

    function attachEventHandlers(self) {
        document.querySelectorAll('.expand-icon').forEach(function (icon) {
            var newIcon = icon.cloneNode(true);
            icon.parentNode.replaceChild(newIcon, icon);

            newIcon.addEventListener('click', function (e) {
                e.stopPropagation();
                var row = this.closest('tr');
                toggleExpand(self, row);
            });
        });

        // Add focusout handlers for editable fields
        document.querySelectorAll('td div[contenteditable]').forEach(function (element) {
            // Remove existing listeners by cloning
            var newElement = element.cloneNode(true);
            element.parentNode.replaceChild(newElement, element);

            // Store original value when field is focused (before editing)
            newElement.addEventListener("focus", function () {
                if (!this.getAttribute('data-original-value')) {
                    this.setAttribute('data-original-value', this.innerHTML);
                }
            });

            newElement.addEventListener("focusout", function () {
                var div = this;
                var row = this.closest('tr');
                var variableName = row.getAttribute('data-path');
                var originalValue = div.getAttribute('data-original-value');

                // No change made
                if (originalValue === this.innerHTML) {
                    return;
                }

                $dyn.callFunction(self.TryUpdateVariableValue, div, { _path: variableName, _value: this.innerHTML }, function (ret) {
                    if (ret !== null && typeof ret !== 'undefined') {
                        // Remove any existing error message
                        var existingError = div.parentElement.querySelector('.inline-error');
                        if (existingError) {
                            existingError.remove();
                        }

                        if (ret.Error !== '') {
                            // Display inline error and revert to original value
                            div.innerHTML = originalValue;
                            var errorMsg = document.createElement('span');
                            errorMsg.className = 'inline-error';
                            errorMsg.textContent = ret.Error;
                            div.parentElement.insertBefore(errorMsg, div);

                            // Auto-remove error after 5 seconds
                            setTimeout(function () {
                                if (errorMsg.parentElement) {
                                    errorMsg.remove();
                                }
                            }, 5000);
                        } else {
                            // Success - update value and mark as modified
                            div.innerHTML = ret.Value;
                            div.style.color = "red";
                            div.setAttribute('data-original-value', ret.Value);
                        }
                    }
                });
            });
        });
    }

    function arrangeInspectorEntries(self, entries, expandedPaths) {

        removeAllEntries();

        var tbody = document.querySelector("#inspector tbody");

        insertEntryRows(self, tbody, entries, 0, '');

        attachEventHandlers(self);

        // Apply initial filter if search term exists
        var searchInput = document.getElementById('inspector-search');
        if (searchInput && searchInput.value) {
            filterRows(searchInput.value);
        }

        // Restore expanded state if provided
        if (expandedPaths && expandedPaths.length > 0) {
            restoreExpandedPaths(self, expandedPaths, 0);
        }
    }

    function getExpandedPaths() {
        var paths = [];
        var rows = document.querySelectorAll('#inspector tbody tr[data-expanded="true"]');
        rows.forEach(function (row) {
            paths.push(row.getAttribute('data-path'));
        });
        return paths;
    }

    function restoreExpandedPaths(self, paths, index) {
        if (index >= paths.length) return;

        var path = paths[index];
        var row = document.querySelector('#inspector tbody tr[data-path="' + path + '"]');

        if (row && row.getAttribute('data-has-children') === 'true' && row.getAttribute('data-expanded') !== 'true') {
            loadChildrenAndExpand(self, row, function () {
                restoreExpandedPaths(self, paths, index + 1);
            });
        } else {
            restoreExpandedPaths(self, paths, index + 1);
        }
    }

    $dyn.ui.defaults.MXTXppInterpreterVariablesInspector = {};
    $dyn.controls.MXTXppInterpreterVariablesInspector = function (data, element) {

        var self = this;
        var currentEntries = null;

        // Store reference globally for event handlers
        window.inspectorControlInstance = self;

        $dyn.ui.Control.apply(self, arguments);
        $dyn.ui.applyDefaults(self, data, $dyn.ui.defaults.MXTXppInterpreterVariablesInspector);

        // Setup search handler
        var searchInput = document.getElementById('inspector-search');
        if (searchInput) {
            searchInput.addEventListener('input', function () {
                filterRows(searchInput.value);
            });
        }

        // Setup immediate window execute
        var historyEndPosition = 0;

        function setupImmediateWindow() {
            var immediateConsole = document.getElementById('immediate-console');
            if (!immediateConsole) {
                // Retry after a short delay if element not found
                setTimeout(setupImmediateWindow, 100);
                return;
            }

            // Handle all keys via addEventListener
            immediateConsole.addEventListener('keydown', function (e) {
                var keyCode = e.which || e.keyCode;

                // Enter key (13)
                if (keyCode === 13 && !e.shiftKey && !e.ctrlKey) {
                    e.preventDefault();
                    if (e.stopPropagation) e.stopPropagation();

                    var consoleText = immediateConsole.value;
                    var currentInput = consoleText.substring(historyEndPosition).trim();

                    if (currentInput) {
                        // Call F&O to execute the code
                        $dyn.callFunction(self.ExecuteImmediateCode, immediateConsole, { _code: currentInput }, function (result) {
                            if (result !== null && typeof result !== 'undefined') {
                                var output = '';
                                if (result.Error && result.Error !== '') {
                                    output = result.Error;
                                } else {
                                    output = (result.Output || '(No output)');
                                }

                                // Append output to console
                                immediateConsole.value = consoleText + '\n' + output + '\n';

                                // Update history end position
                                historyEndPosition = immediateConsole.value.length;

                                // Scroll to bottom
                                immediateConsole.scrollTop = immediateConsole.scrollHeight;

                                // Set cursor at the end
                                immediateConsole.setSelectionRange(historyEndPosition, historyEndPosition);
                            }
                        });
                    }

                    return false;
                }

                // Prevent deletion or editing of history
                var selectionStart = this.selectionStart;
                var selectionEnd = this.selectionEnd;

                // Backspace (8) and Delete (46)
                if (keyCode === 8 || keyCode === 46) {
                    if (selectionStart <= historyEndPosition || selectionEnd <= historyEndPosition) {
                        e.preventDefault();
                        return false;
                    }
                }

                // ArrowLeft (37), ArrowUp (38), Home (36)
                if (keyCode === 37 || keyCode === 38 || keyCode === 36) {
                    setTimeout(function () {
                        if (immediateConsole.selectionStart < historyEndPosition) {
                            immediateConsole.setSelectionRange(historyEndPosition, historyEndPosition);
                        }
                    }, 0);
                }
            });

            // Additional event listeners for click and select
            immediateConsole.addEventListener('click', function () {
                // Prevent clicking into history area
                if (this.selectionStart < historyEndPosition) {
                    this.setSelectionRange(historyEndPosition, historyEndPosition);
                }
            });

            immediateConsole.addEventListener('select', function () {
                // Prevent selecting into history area
                if (this.selectionStart < historyEndPosition) {
                    this.setSelectionRange(historyEndPosition, historyEndPosition);
                }
            });

            // Clear button functionality
            var immediateClearBtn = document.getElementById('immediate-clear-btn');
            if (immediateClearBtn) {
                immediateClearBtn.addEventListener('click', function () {
                    immediateConsole.value = '';
                    historyEndPosition = 0;
                    immediateConsole.focus();
                });
            }
        }

        // Initialize immediate window with retry logic
        setupImmediateWindow();

        $dyn.observe(this.InspectVariables, function (root) {
            if (root !== null && typeof root !== 'undefined') {
                // Save expanded paths before clearing
                var expandedPaths = getExpandedPaths();

                currentEntries = root.Subentries;
                arrangeInspectorEntries(self, currentEntries, expandedPaths);
            }
        });

        $dyn.observe(this.ExecutionPhase, function (value) {
            if (value === 2) {
                currentEntries = null;
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