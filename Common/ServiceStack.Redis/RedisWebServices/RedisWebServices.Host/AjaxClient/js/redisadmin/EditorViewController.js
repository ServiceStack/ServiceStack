/**
 * Created by IntelliJ IDEA.
 * User: mythz
 * Date: 18-Jul-2010
 * Time: 00:13:45
 * To change this template use File | Settings | File Templates.
 */

goog.provide("redisadmin.EditorViewController");

goog.require('goog.events');
goog.require('goog.json');
goog.require('goog.dom');
goog.require('goog.style');

goog.require('goog.ui.Component');
goog.require('goog.ui.AutoComplete.Basic');
goog.require('goog.ui.Toolbar');

redisadmin.EditorViewController = function(rootEl, app)
{
    redisadmin.ViewController.call(this, rootEl, app);

    this.log = goog.debug.Logger.getLogger('redisadmin.EditorViewController');
    this.log.setLevel(goog.debug.Logger.Level.FINE);
    this.log.info('initializing EditorViewController()');
}
goog.inherits(redisadmin.EditorViewController, redisadmin.ViewController);

redisadmin.EditorViewController.prototype.init = function(path)
{    
};

redisadmin.EditorViewController.prototype.showKeyGroup = function(parentLabel, childKeys)
{
    var $this = this;

    this.app.getRedisClient().getValues(childKeys, function(values){
        var rows = [];
        for (var i=0; i<childKeys.length; i++)
        {
            var jsonText = values[i];

            var row = {Key:childKeys[i]}, obj = {};

            try
            {
                obj = goog.json.parse(jsonText);
            }
            catch(e){}

            for (var k in obj)
            {
                row[k] = obj[k];
            }
            rows.push(row);
        }

        var tableFormatFns = {
            Key: function(value) {
                return "<a href='#key/" + value + "'>" + value + "</a>";
            }
        };

        var html = "<div class='key-group'>"
            + "<input id='txtKeyGroupFilter' type='text' value='" + parentLabel + "' />"
            + '<div id="btnKeyGroupFilter" class="goog-custom-button goog-inline-block">Filter</div>'
            + "</div>";

        html += "<div id='keys-table'>" + A.toTable(rows, tableFormatFns) + "</div>";

        goog.dom.getElement($this.rootEl).innerHTML = html;

        var txtKeyGroupFilter = goog.dom.getElement('txtKeyGroupFilter');
        var autoComplete = new goog.ui.AutoComplete.Basic(childKeys, txtKeyGroupFilter, false);
        autoComplete.setAllowFreeSelect(true);
        autoComplete.setAutoHilite(false);

        var filterFn = function(e)
        {
            var filteredRows = [], filterText = txtKeyGroupFilter.value;
            for (var i=0; i<rows.length; i++)
            {
                var row = rows[i];
                if (row.Key.indexOf(filterText) != -1)
                {
                    filteredRows.push(row);
                }
            }
            goog.dom.getElement('keys-table').innerHTML = A.toTable(filteredRows, tableFormatFns);
        };

        goog.events.listen(autoComplete, goog.ui.AutoComplete.EventType.UPDATE, filterFn);

        var btnKeyGroupFilter = goog.dom.getElement("btnKeyGroupFilter");
        var button = goog.ui.decorate(btnKeyGroupFilter);
        button.setDispatchTransitionEvents(goog.ui.Component.State.ALL, true);
        goog.events.listen(btnKeyGroupFilter, goog.events.EventType.CLICK, filterFn);
    });
};

redisadmin.EditorViewController.prototype.showKeyDetails = function(key, textValue, canEdit)
{
    var $this = this;

    var html = "<h2 class='key'>" + key + "</h2>"
        + "<div id='keydetails-body'>"
        + "<div id='toolbarViewKey' class='key-options goog-toolbar'>"
        + "<div id='lnk-editkey' class='goog-toolbar-button nav-link'><span class='icon-edit goog-inline-block'></span>edit</div>"
        + "<div class='goog-toolbar-separator nav-separator'></div>"
        + "</div>"
        + "<div id='toolbarEditKey' class='goog-toolbar' style='display:none'>"
        + "<div id='lnk-back' class='goog-toolbar-button'><span class='icon-back goog-inline-block'></span>Back</div>"
        + "<hr/>"
        + "<div class='goog-toolbar-separator nav-separator'></div>"
        + "<div id='btnDeleteKey' class='goog-toolbar-button'><span class='icon-delete goog-inline-block'></span>Delete</div>"
        + "<hr/>"
        + "<div id='btnSaveKey' class='goog-toolbar-button'><span class='icon-save goog-inline-block'></span>Save</div>"
        + "</div>";

    try
    {
        var obj = goog.json.parse(textValue);

        html += "<div id='key-view'>"
              + "<dl>"
        for (var k in obj)
        {
            html += "<dt>" + k + "</dt>"
                  + "<dd>" + Dto.formatValue(obj[k]) + "</dd>";
        }
        html += "</dl>"
        + "</div>";
    }
    catch (e) {
        this.log.warning("Error parsing key as json: " + key + ", Error: " + e);
        html += "<div id='key-view'>" + textValue + "</div>";
    }

    html += "<div id='key-edit' style='display:none'>"
          + "<textarea id='txtEntryValue'></textarea>"
          + "</div>"
          + "<div>";

    goog.dom.getElement($this.rootEl).innerHTML = html;

    var toolbarViewKey = new goog.ui.Toolbar();
    toolbarViewKey.decorate(goog.dom.getElement('toolbarViewKey'));

    var toolbarEditKey = new goog.ui.Toolbar();
    toolbarEditKey.decorate(goog.dom.getElement('toolbarEditKey'));

    var lnkEdit = goog.dom.getElement('lnk-editkey'),
        lnkBack = goog.dom.getElement('lnk-back'),
        btnDeleteKey = goog.dom.getElement('btnDeleteKey'),
        btnSaveKey = goog.dom.getElement('btnSaveKey'),
        txtEntryValue = goog.dom.getElement('txtEntryValue');

    goog.dom.setTextContent(txtEntryValue, textValue);

    toolbarViewKey.getChildAt(0).setEnabled(!!canEdit);

    var editMode = false;

    var toggleEditModeFn = function(e) {
        editMode = !editMode;
        goog.style.showElement(goog.dom.getElement('key-view'), !editMode);
        goog.style.showElement(goog.dom.getElement('key-edit'), editMode);
        goog.style.showElement(goog.dom.getElement('toolbarViewKey'), !editMode);
        goog.style.showElement(goog.dom.getElement('toolbarEditKey'), editMode);
    };
    goog.events.listen(lnkEdit, goog.events.EventType.CLICK, toggleEditModeFn);
    goog.events.listen(lnkBack, goog.events.EventType.CLICK, toggleEditModeFn);

    goog.events.listen(btnDeleteKey, goog.events.EventType.CLICK,
        function(e) {
            $this.app.confirmDelete(key, txtEntryValue.value);
        });

    goog.events.listen(btnSaveKey, goog.events.EventType.CLICK,
        function(e) {
            $this.app.updateKey(key, txtEntryValue.value);
        });
}
