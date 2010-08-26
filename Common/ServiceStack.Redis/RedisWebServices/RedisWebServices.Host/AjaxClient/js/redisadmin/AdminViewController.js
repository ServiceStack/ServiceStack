/**
 * Created by IntelliJ IDEA.
 * User: mythz
 * Date: 18-Jul-2010
 * Time: 00:13:45
 * To change this template use File | Settings | File Templates.
 */

goog.provide("redisadmin.AdminViewController");

goog.require("redisadmin.ViewController");

goog.require('goog.events');
goog.require('goog.dom');
goog.require('goog.ui.Toolbar');
goog.require('goog.ui.ToolbarButton');
goog.require('goog.ui.ToolbarSeparator');

/**
 * Handles the Admin tab content
 * @param {string} rootEl name of root element.
 * @param {redisadmin.App} the main app.
 * @constructor
 */
redisadmin.AdminViewController = function(rootEl, app)
{
    redisadmin.ViewController.call(this, rootEl, app);

    var $this = this;

    var toolbarServerOps = new goog.ui.Toolbar();
    toolbarServerOps.decorate(goog.dom.getElement('toolbarServerOps'));    

    var $btnRefreshServerInfo = goog.dom.getElement('btnRefreshServerInfo'),
        $btnRewriteAof = goog.dom.getElement('btnRewriteAof'),
        $btnFlushAll = goog.dom.getElement('btnFlushAll'),
        $btnFlushDb = goog.dom.getElement('btnFlushDb'),
        $btnResetDb = goog.dom.getElement('btnResetDb');

    goog.events.listen($btnRefreshServerInfo, goog.events.EventType.CLICK, function(e){
        $this.refreshServerInfo();
    });

    var resetAppFn = function() {
        $this.app.searchInMainNav(goog.dom.getElement("txtQuery").value);
        $this.refreshServerInfo();
    }

    goog.events.listen($btnRewriteAof, goog.events.EventType.CLICK, function(e){
        $this.app.getRedisClient().rewriteAppendOnlyFileAsync(function(e){
            alert("The Redis Append Only File was re-written");
        });
    });

    goog.events.listen($btnFlushAll, goog.events.EventType.CLICK, function(e){
        if (confirm("Are you sure you want to permanently delete all data in Redis?"))
        {
            $this.app.getRedisClient().flushAll(function(e){
                alert("The entire Redis datastore was flushed\n\n(Note: you can click Reset DB to restore some data)");
                resetAppFn();
            });
        }
    });

    goog.events.listen($btnFlushDb, goog.events.EventType.CLICK, function(e){
        var dbIndexStr = prompt("Enter the Redis Database Index you want to permanently delete:");
        if (!isNaN(dbIndexStr))
        {
            var dbIndex = parseInt(dbIndexStr); 
            $this.app.getRedisClient().flushDb(dbIndex, function(e){
                alert("Redis database '" + dbIndex + "' was flushed");
                resetAppFn();
            });
        }
    });

    goog.events.listen($btnResetDb, goog.events.EventType.CLICK, function(e){
        if (confirm("Are you sure you want to populate Redis with the Northwind Database?"))
        {
            $this.app.getRedisClient().populateRedisWithData(function(e){
                alert("The Redis was populated");
                resetAppFn();
            });
        }
    });
}
goog.inherits(redisadmin.AdminViewController, redisadmin.ViewController);

redisadmin.AdminViewController.prototype.init = function(path)
{
    this.refreshServerInfo();
};

redisadmin.AdminViewController.prototype.refreshServerInfo = function(path)
{
    var $this = this;

    this.app.getRedisClient().getServerInfo(
        function(serverInfo) {
            var html = "<table>";
            for (var k in serverInfo) {
                html += "<tr><th>" + k + "</th><td>" + serverInfo[k] + "</td></tr>";
            }
            html += "</table>";
            goog.dom.getElement("server-info").innerHTML = html;
        },
        function(e) {
            alert("There was a problem retrieving Redis server info:\n" + e);
        });
}

//goog.exportSymbol("redisadmin.AdminViewController", redisadmin.AdminViewController);
