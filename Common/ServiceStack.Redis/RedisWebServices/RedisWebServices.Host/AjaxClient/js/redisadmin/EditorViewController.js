/**
 * Created by IntelliJ IDEA.
 * User: mythz
 * Date: 18-Jul-2010
 * Time: 00:13:45
 * To change this template use File | Settings | File Templates.
 */

goog.provide("redisadmin.EditorViewController");

redisadmin.EditorViewController = function(rootEl, app)
{
    redisadmin.ViewController.call(this, rootEl, app);
}
goog.inherits(redisadmin.EditorViewController, redisadmin.ViewController);

redisadmin.EditorViewController.prototype.loadPath = function(path)
{
    
};