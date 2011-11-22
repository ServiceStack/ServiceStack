var MiniProfiler = (function ($) {

    var options,
        container,
        controls,
        fetchedIds = [],
        fetchingIds = []  // so we never pull down a profiler twice
        ;

    var hasLocalStorage = function () {
        try {
            return 'localStorage' in window && window['localStorage'] !== null;
        } catch (e) {
            return false;
        }
    };

    var getVersionedKey = function (keyPrefix) {
        return keyPrefix + '-' + options.version;
    }

    var save = function (keyPrefix, value) {
        if (!hasLocalStorage()) { return; }

        // clear old keys with this prefix, if any
        for (var i = 0; i < localStorage.length; i++) {
            if ((localStorage.key(i) || '').indexOf(keyPrefix) > -1) {
                localStorage.removeItem(localStorage.key(i));
            }
        }

        // save under this version
        localStorage[getVersionedKey(keyPrefix)] = value;
    };

    var load = function (keyPrefix) {
        if (!hasLocalStorage()) { return null; }

        return localStorage[getVersionedKey(keyPrefix)];
    };

    var fetchTemplates = function (success) {
        var key = 'mini-profiler-templates',
            cached = load(key);

        if (cached) {
            $('body').append(cached);
            success();
        }
        else {
            $.get(options.path + 'mini-profiler-includes.tmpl?v=' + options.version, function (data) {
                if (data) {
                    save(key, data);
                    $('body').append(data);
                    success();
                }
            });
        }
    };

    var fetchResults = function (ids) {
        for (var i = 0, id; i < ids.length; i++) {
            id = ids[i];
            if ($.inArray(id, fetchedIds) < 0 && $.inArray(id, fetchingIds) < 0) {
                var idx = fetchingIds.push(id) - 1;

                $.ajax({
                    url: options.path + 'mini-profiler-results?id=' + id + '&popup=1',
                    dataType: 'json',
                    success: function (json) {
                        fetchedIds.push(id);
                        buttonShow(json);
                    },
                    complete: function () {
                        fetchingIds.splice(idx, 1);
                    }
                });
            }
        }
    };

    var renderTemplate = function (json) {
        return $('#profilerTemplate').tmpl(json);
    };

    var buttonShow = function (json) {
        var result = renderTemplate(json);

        if (controls)
            result.insertBefore(controls);
        else
            result.appendTo(container);

        var button = result.find('.profiler-button'),
            popup = result.find('.profiler-popup');

        // button will appear in corner with the total profiling duration - click to show details
        button.click(function () { buttonClick(button, popup); });

        // small duration steps and the column with aggregate durations are hidden by default; allow toggling
        toggleHidden(popup);

        // lightbox in the queries
        popup.find('.queries-show').click(function () { queriesShow($(this), result); });

        // limit count
        if (container.find('.profiler-result').length > options.maxTracesToShow)
            container.find('.profiler-result').first().remove();
        button.show();
    };

    var toggleHidden = function (popup) {
        var trivial = popup.find('.toggle-trivial');
        var childrenTime = popup.find('.toggle-duration-with-children');
        var trivialGaps = popup.parent().find('.toggle-trivial-gaps');

        var toggleIt = function (node) {
            var link = $(node),
                klass = link.attr('class').substr('toggle-'.length),
                isHidden = link.text().indexOf('show') > -1;

            popup.parent().find('.' + klass).toggle(isHidden);
            link.text(link.text().replace(isHidden ? 'show' : 'hide', isHidden ? 'hide' : 'show'));

            popupPreventHorizontalScroll(popup);
        };

        childrenTime.add(trivial).add(trivialGaps).click(function () {
            toggleIt(this);
        });

        // if option is set or all our timings are trivial, go ahead and show them
        if (options.showTrivial || trivial.data('show-on-load')) {
            toggleIt(trivial);
        }
        // if option is set, go ahead and show time with children
        if (options.showChildrenTime) {
            toggleIt(childrenTime);
        }
    };

    var buttonClick = function (button, popup) {
        // we're toggling this button/popup
        if (popup.is(':visible')) {
            popupHide(button, popup);
        }
        else {
            var visiblePopups = container.find('.profiler-popup:visible'),
                theirButtons = visiblePopups.siblings('.profiler-button');

            // hide any other popups
            popupHide(theirButtons, visiblePopups);

            // before showing the one we clicked
            popupShow(button, popup);
        }
    };

    var popupShow = function (button, popup) {
        button.addClass('profiler-button-active');

        popupSetDimensions(button, popup);

        popup.show();

        popupPreventHorizontalScroll(popup);
    };

    var popupSetDimensions = function (button, popup) {
        var top = button.position().top - 1, // position next to the button we clicked
            windowHeight = $(window).height(),
            maxHeight = windowHeight - top - 40; // make sure the popup doesn't extend below the fold

        popup
            .css({ 'top': top, 'max-height': maxHeight })
            .css(options.renderPosition, button.outerWidth() - 3); // move left or right, based on config
    };

    var popupPreventHorizontalScroll = function (popup) {
        var childrenHeight = 0;

        popup.children().each(function () { childrenHeight += $(this).height(); });

        popup.css({ 'padding-right': childrenHeight > popup.height() ? 40 : 10 });
    }

    var popupHide = function (button, popup) {
        button.removeClass('profiler-button-active');
        popup.hide();
    };

    var queriesShow = function (link, result) {

        var px = 30,
            win = $(window),
            width = win.width() - 2 * px,
            height = win.height() - 2 * px,
            queries = result.find('.profiler-queries');

        // opaque background
        $('<div class="profiler-queries-bg"/>').appendTo('body').css({ 'height': $(document).height() }).show();

        // center the queries and ensure long content is scrolled
        queries.css({ 'top': px, 'max-height': height, 'width': width }).css(options.renderPosition, px)
            .find('table').css({ 'width': width });

        // have to show everything before we can get a position for the first query
        queries.show();

        queriesScrollIntoView(link, queries, queries);

        // syntax highlighting
        prettyPrint();
    };

    var queriesScrollIntoView = function (link, queries, whatToScroll) {
        var id = link.closest('tr').attr('data-timing-id'),
            cells = queries.find('tr[data-timing-id="' + id + '"] td');

        // ensure they're in view
        whatToScroll.scrollTop(whatToScroll.scrollTop() + cells.first().position().top - 100);

        // highlight and then fade back to original bg color; do it ourselves to prevent any conflicts w/ jquery.UI or other implementations of Resig's color plugin
        cells.each(function () {
            var cell = $(this),
                highlightHex = '#FFFFBB',
                highlightRgb = getRGB(highlightHex),
                originalRgb = getRGB(cell.css('background-color')),
                getColorDiff = function (fx, i) {
                    // adapted from John Resig's color plugin: http://plugins.jquery.com/project/color
                    return Math.max(Math.min(parseInt((fx.pos * (originalRgb[i] - highlightRgb[i])) + highlightRgb[i]), 255), 0);
                };

            // we need to animate some other property to piggy-back on the step function, so I choose you, opacity!
            cell.css({ 'opacity': 1, 'background-color': highlightHex })
                .animate({ 'opacity': 1 }, { duration: 2000, step: function (now, fx) {
                    fx.elem.style['backgroundColor'] = "rgb(" + [getColorDiff(fx, 0), getColorDiff(fx, 1), getColorDiff(fx, 2)].join(",") + ")";
                }
                });
        });
    };

    // Color Conversion functions from highlightFade
    // By Blair Mitchelmore
    // http://jquery.offput.ca/highlightFade/
    // Parse strings looking for color tuples [255,255,255]
    var getRGB = function (color) {
        var result;

        // Check if we're already dealing with an array of colors
        if (color && color.constructor == Array && color.length == 3) return color;

        // Look for rgb(num,num,num)
        if (result = /rgb\(\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\s*,\s*([0-9]{1,3})\s*\)/.exec(color)) return [parseInt(result[1]), parseInt(result[2]), parseInt(result[3])];

        // Look for rgb(num%,num%,num%)
        if (result = /rgb\(\s*([0-9]+(?:\.[0-9]+)?)\%\s*,\s*([0-9]+(?:\.[0-9]+)?)\%\s*,\s*([0-9]+(?:\.[0-9]+)?)\%\s*\)/.exec(color)) return [parseFloat(result[1]) * 2.55, parseFloat(result[2]) * 2.55, parseFloat(result[3]) * 2.55];

        // Look for #a0b1c2
        if (result = /#([a-fA-F0-9]{2})([a-fA-F0-9]{2})([a-fA-F0-9]{2})/.exec(color)) return [parseInt(result[1], 16), parseInt(result[2], 16), parseInt(result[3], 16)];

        // Look for #fff
        if (result = /#([a-fA-F0-9])([a-fA-F0-9])([a-fA-F0-9])/.exec(color)) return [parseInt(result[1] + result[1], 16), parseInt(result[2] + result[2], 16), parseInt(result[3] + result[3], 16)];

        // Look for rgba(0, 0, 0, 0) == transparent in Safari 3
        if (result = /rgba\(0, 0, 0, 0\)/.exec(color)) return colors['transparent'];

        return null;
    };

    var bindDocumentEvents = function () {
        $(document).bind('click keyup', function (e) {

            // this happens on every keystroke, and :visible is crazy expensive in IE <9
            // and in this case, the display:none check is sufficient.
            var popup = $('.profiler-popup').filter(function () { return $(this).css("display") !== "none"; });

            if (!popup.length) {
                return;
            }

            var button = popup.siblings('.profiler-button'),
                queries = popup.closest('.profiler-result').find('.profiler-queries'),
                bg = $('.profiler-queries-bg'),
                isEscPress = e.type == 'keyup' && e.which == 27,
                hidePopup = false,
                hideQueries = false;

            if (bg.is(':visible')) {
                hideQueries = isEscPress || (e.type == 'click' && !$.contains(queries[0], e.target) && !$.contains(popup[0], e.target));
            }
            else if (popup.is(':visible')) {
                hidePopup = isEscPress || (e.type == 'click' && !$.contains(popup[0], e.target) && !$.contains(button[0], e.target) && button[0] != e.target);
            }

            if (hideQueries) {
                bg.remove();
                queries.hide();
            }

            if (hidePopup) {
                popupHide(button, popup);
            }
        });
    };

    var initFullView = function () {

        // first, get jquery tmpl, then render and bind handlers
        fetchTemplates(function () {

            // profiler will be defined in the full page's head
            renderTemplate(profiler).appendTo(container);

            var popup = $('.profiler-popup');

            toggleHidden(popup);

            prettyPrint();

            // since queries are already shown, just highlight and scroll when clicking a "1 sql" link
            popup.find('.queries-show').click(function () {
                queriesScrollIntoView($(this), $('.profiler-queries'), $(document));
            });
        });
    };

    var initControls = function (container) {
        if (options.showControls) {
            controls = $('<div class="profiler-controls"><span class="profiler-min-max">m</span><span class="profiler-clear">c</span></div>').appendTo(container);

            $('.profiler-controls .profiler-min-max').click(function () {
                container.toggleClass('profiler-min');
            });

            container.hover(function () {
                if ($(this).hasClass('profiler-min')) {
                    $(this).find('.profiler-min-max').show();
                }
            },
            function () {
                if ($(this).hasClass('profiler-min')) {
                    $(this).find('.profiler-min-max').hide();
                }
            });

            $('.profiler-controls .profiler-clear').click(function () {
                container.find('.profiler-result').remove();
            });
        }
        else {
            container.addClass('profiler-no-controls');
        }
    };

    var initPopupView = function () {
        // all fetched profilings will go in here
        container = $('<div class="profiler-results"/>').appendTo('body');

        // MiniProfiler.RenderIncludes() sets which corner to render in - default is upper left
        container.addClass(options.renderPosition);

        //initialize the controls
        initControls(container);

        // we'll render results json via a jquery.tmpl - after we get the templates, we'll fetch the initial json to populate it
        fetchTemplates(function () {
            // get master page profiler results
            fetchResults(options.ids);
        });

        // fetch profile results for any ajax calls
        $(document).ajaxComplete(function (e, xhr, settings) {
            if (xhr) {
                // should be an array of strings, e.g. ["008c4813-9bd7-443d-9376-9441ec4d6a8c","16ff377b-8b9c-4c20-a7b5-97cd9fa7eea7"]
                var stringIds = xhr.getResponseHeader('X-MiniProfiler-Ids');
                if (stringIds) {
                    var ids = typeof JSON != 'undefined' ? JSON.parse(stringIds) : eval(stringIds);
                    fetchResults(ids);
                }
            }
        });


        // fetch results after ASP Ajax calls
        if (typeof (Sys) != 'undefined' && typeof (Sys.WebForms) != 'undefined' && typeof (Sys.WebForms.PageRequestManager) != 'undefined') {
            // Get the instance of PageRequestManager.
            var PageRequestManager = Sys.WebForms.PageRequestManager.getInstance();

            PageRequestManager.add_endRequest(function (sender, args) {
                if (args) {
                    var response = args.get_response();
                    if (response.get_responseAvailable() && response._xmlHttpRequest != null) {
                        var stringIds = args.get_response().getResponseHeader('X-MiniProfiler-Ids');
                        if (stringIds) {
                            var ids = typeof JSON != 'undefined' ? JSON.parse(stringIds) : eval(stringIds);
                            fetchResults(ids);
                        }
                    }
                }
            });
        }

        // some elements want to be hidden on certain doc events
        bindDocumentEvents();
    };

    return {

        init: function (opt) {

            options = opt || {};

            // when rendering a shared, full page, this div will exist
            container = $('.profiler-result-full');

            if (container.length) {
                initFullView();
            }
            else {
                initPopupView();
            }
        },

        renderDate: function (jsonDate) { // JavaScriptSerializer sends dates as /Date(1308024322065)/
            if (jsonDate) {
                return (typeof jsonDate === 'string') ? new Date(parseInt(jsonDate.replace("/Date(", "").replace(")/", ""), 10)).toUTCString() : jsonDate;
            }
        },

        renderIndent: function (depth) {
            var result = '';
            for (var i = 0; i < depth; i++) {
                result += '&nbsp;';
            }
            return result;
        },

        renderExecuteType: function (typeId) {
            // see MvcMiniProfiler.ExecuteType enum
            switch (typeId) {
                case 0: return 'None';
                case 1: return 'NonQuery';
                case 2: return 'Scalar';
                case 3: return 'Reader';
            }
        },

        shareUrl: function (id) {
            return options.path + 'mini-profiler-results?id=' + id;
        },

        getSqlTimings: function (root) {
            var result = [],
                addToResults = function (timing) {
                    if (timing.SqlTimings) {
                        for (var i = 0, sqlTiming; i < timing.SqlTimings.length; i++) {
                            sqlTiming = timing.SqlTimings[i];

                            // HACK: add info about the parent Timing to each SqlTiming so UI can render
                            sqlTiming.ParentTimingName = timing.Name;
                            result.push(sqlTiming);
                        }
                    }

                    if (timing.Children) {
                        for (var i = 0; i < timing.Children.length; i++) {
                            addToResults(timing.Children[i]);
                        }
                    }
                };

            // start adding at the root and recurse down
            addToResults(root);

            var removeDuration = function (list, duration) {

                var newList = [];
                for (var i = 0; i < list.length; i++) {

                    var item = list[i];
                    if (duration.start > item.start) {
                        if (duration.start > item.finish) {
                            newList.push(item);
                            continue;
                        }
                        newList.push({ start: item.start, finish: duration.start });
                    }

                    if (duration.finish < item.finish) {
                        if (duration.finish < item.start) {
                            newList.push(item);
                            continue;
                        }
                        newList.push({ start: duration.finish, finish: item.finish });
                    }
                }

                return newList;
            }

            var processTimes = function (elem, parent) {
                var duration = { start: elem.StartMilliseconds, finish: (elem.StartMilliseconds + elem.DurationMilliseconds) };
                elem.richTiming = [duration];
                if (parent != null) {
                    elem.parent = parent;
                    elem.parent.richTiming = removeDuration(elem.parent.richTiming, duration);
                }

                if (elem.Children) {
                    for (var i = 0; i < elem.Children.length; i++) {
                        processTimes(elem.Children[i], elem);
                    }
                }
            };

            processTimes(root, null);

            // sort results by time
            result.sort(function (a, b) { return a.StartMilliseconds - b.StartMilliseconds; });

            var determineOverlap = function (gap, node) {
                var overlap = 0;
                for (var i = 0; i < node.richTiming.length; i++) {
                    var current = node.richTiming[i];
                    if (current.start > gap.finish) {
                        break;
                    }
                    if (current.finish < gap.start) {
                        continue;
                    }

                    overlap += Math.min(gap.finish, current.finish) - Math.max(gap.start, current.start);
                }
                return overlap;
            }

            var determineGap = function (gap, node, match) {
                var overlap = determineOverlap(gap, node);
                if (match == null || overlap > match.duration) {
                    match = { name: node.Name, duration: overlap };
                }
                else if (match.name == node.Name) {
                    match.duration += overlap;
                }

                if (node.Children) {
                    for (var i = 0; i < node.Children.length; i++) {
                        match = determineGap(gap, node.Children[i], match);
                    }
                }
                return match;
            };

            var time = 0;
            var prev = null;
            $.each(result, function () {
                this.prevGap = {
                    duration: (this.StartMilliseconds - time).toFixed(2),
                    start: time,
                    finish: this.StartMilliseconds
                };

                this.prevGap.topReason = determineGap(this.prevGap, root, null);

                time = this.StartMilliseconds + this.DurationMilliseconds;
                prev = this;
            });


            if (result.length > 0) {
                var me = result[result.length - 1];
                me.nextGap = {
                    duration: (root.DurationMilliseconds - time).toFixed(2),
                    start: time,
                    finish: root.DurationMilliseconds
                };
                me.nextGap.topReason = determineGap(me.nextGap, root, null);
            }

            return result;
        },

        getSqlTimingsCount: function (root) {
            var result = 0,
                countSql = function (timing) {
                    if (timing.SqlTimings) {
                        result += timing.SqlTimings.length;
                    }

                    if (timing.Children) {
                        for (var i = 0; i < timing.Children.length; i++) {
                            countSql(timing.Children[i]);
                        }
                    }
                };
            countSql(root);
            return result;
        },

        fetchResultsExposed: function (ids) {
            return fetchResults(ids);
        },

        formatDuration: function (duration) {
            return (duration || 0).toFixed(1);
        }
    };
})(jQuery);

// prettify.js
// http://code.google.com/p/google-code-prettify/

window.PR_SHOULD_USE_CONTINUATION=true;window.PR_TAB_WIDTH=8;window.PR_normalizedHtml=window.PR=window.prettyPrintOne=window.prettyPrint=void 0;window._pr_isIE6=function(){var y=navigator&&navigator.userAgent&&navigator.userAgent.match(/\bMSIE ([678])\./);y=y?+y[1]:false;window._pr_isIE6=function(){return y};return y};
(function(){function y(b){return b.replace(L,"&amp;").replace(M,"&lt;").replace(N,"&gt;")}function H(b,f,i){switch(b.nodeType){case 1:var o=b.tagName.toLowerCase();f.push("<",o);var l=b.attributes,n=l.length;if(n){if(i){for(var r=[],j=n;--j>=0;)r[j]=l[j];r.sort(function(q,m){return q.name<m.name?-1:q.name===m.name?0:1});l=r}for(j=0;j<n;++j){r=l[j];r.specified&&f.push(" ",r.name.toLowerCase(),'="',r.value.replace(L,"&amp;").replace(M,"&lt;").replace(N,"&gt;").replace(X,"&quot;"),'"')}}f.push(">");
for(l=b.firstChild;l;l=l.nextSibling)H(l,f,i);if(b.firstChild||!/^(?:br|link|img)$/.test(o))f.push("</",o,">");break;case 3:case 4:f.push(y(b.nodeValue));break}}function O(b){function f(c){if(c.charAt(0)!=="\\")return c.charCodeAt(0);switch(c.charAt(1)){case "b":return 8;case "t":return 9;case "n":return 10;case "v":return 11;case "f":return 12;case "r":return 13;case "u":case "x":return parseInt(c.substring(2),16)||c.charCodeAt(1);case "0":case "1":case "2":case "3":case "4":case "5":case "6":case "7":return parseInt(c.substring(1),
8);default:return c.charCodeAt(1)}}function i(c){if(c<32)return(c<16?"\\x0":"\\x")+c.toString(16);c=String.fromCharCode(c);if(c==="\\"||c==="-"||c==="["||c==="]")c="\\"+c;return c}function o(c){var d=c.substring(1,c.length-1).match(RegExp("\\\\u[0-9A-Fa-f]{4}|\\\\x[0-9A-Fa-f]{2}|\\\\[0-3][0-7]{0,2}|\\\\[0-7]{1,2}|\\\\[\\s\\S]|-|[^-\\\\]","g"));c=[];for(var a=[],k=d[0]==="^",e=k?1:0,h=d.length;e<h;++e){var g=d[e];switch(g){case "\\B":case "\\b":case "\\D":case "\\d":case "\\S":case "\\s":case "\\W":case "\\w":c.push(g);
continue}g=f(g);var s;if(e+2<h&&"-"===d[e+1]){s=f(d[e+2]);e+=2}else s=g;a.push([g,s]);if(!(s<65||g>122)){s<65||g>90||a.push([Math.max(65,g)|32,Math.min(s,90)|32]);s<97||g>122||a.push([Math.max(97,g)&-33,Math.min(s,122)&-33])}}a.sort(function(v,w){return v[0]-w[0]||w[1]-v[1]});d=[];g=[NaN,NaN];for(e=0;e<a.length;++e){h=a[e];if(h[0]<=g[1]+1)g[1]=Math.max(g[1],h[1]);else d.push(g=h)}a=["["];k&&a.push("^");a.push.apply(a,c);for(e=0;e<d.length;++e){h=d[e];a.push(i(h[0]));if(h[1]>h[0]){h[1]+1>h[0]&&a.push("-");
a.push(i(h[1]))}}a.push("]");return a.join("")}function l(c){for(var d=c.source.match(RegExp("(?:\\[(?:[^\\x5C\\x5D]|\\\\[\\s\\S])*\\]|\\\\u[A-Fa-f0-9]{4}|\\\\x[A-Fa-f0-9]{2}|\\\\[0-9]+|\\\\[^ux0-9]|\\(\\?[:!=]|[\\(\\)\\^]|[^\\x5B\\x5C\\(\\)\\^]+)","g")),a=d.length,k=[],e=0,h=0;e<a;++e){var g=d[e];if(g==="(")++h;else if("\\"===g.charAt(0))if((g=+g.substring(1))&&g<=h)k[g]=-1}for(e=1;e<k.length;++e)if(-1===k[e])k[e]=++n;for(h=e=0;e<a;++e){g=d[e];if(g==="("){++h;if(k[h]===undefined)d[e]="(?:"}else if("\\"===
g.charAt(0))if((g=+g.substring(1))&&g<=h)d[e]="\\"+k[h]}for(h=e=0;e<a;++e)if("^"===d[e]&&"^"!==d[e+1])d[e]="";if(c.ignoreCase&&r)for(e=0;e<a;++e){g=d[e];c=g.charAt(0);if(g.length>=2&&c==="[")d[e]=o(g);else if(c!=="\\")d[e]=g.replace(/[a-zA-Z]/g,function(s){s=s.charCodeAt(0);return"["+String.fromCharCode(s&-33,s|32)+"]"})}return d.join("")}for(var n=0,r=false,j=false,q=0,m=b.length;q<m;++q){var t=b[q];if(t.ignoreCase)j=true;else if(/[a-z]/i.test(t.source.replace(/\\u[0-9a-f]{4}|\\x[0-9a-f]{2}|\\[^ux]/gi,
""))){r=true;j=false;break}}var p=[];q=0;for(m=b.length;q<m;++q){t=b[q];if(t.global||t.multiline)throw Error(""+t);p.push("(?:"+l(t)+")")}return RegExp(p.join("|"),j?"gi":"g")}function Y(b){var f=0;return function(i){for(var o=null,l=0,n=0,r=i.length;n<r;++n)switch(i.charAt(n)){case "\t":o||(o=[]);o.push(i.substring(l,n));l=b-f%b;for(f+=l;l>=0;l-=16)o.push("                ".substring(0,l));l=n+1;break;case "\n":f=0;break;default:++f}if(!o)return i;o.push(i.substring(l));return o.join("")}}function I(b,
f,i,o){if(f){b={source:f,c:b};i(b);o.push.apply(o,b.d)}}function B(b,f){var i={},o;(function(){for(var r=b.concat(f),j=[],q={},m=0,t=r.length;m<t;++m){var p=r[m],c=p[3];if(c)for(var d=c.length;--d>=0;)i[c.charAt(d)]=p;p=p[1];c=""+p;if(!q.hasOwnProperty(c)){j.push(p);q[c]=null}}j.push(/[\0-\uffff]/);o=O(j)})();var l=f.length;function n(r){for(var j=r.c,q=[j,z],m=0,t=r.source.match(o)||[],p={},c=0,d=t.length;c<d;++c){var a=t[c],k=p[a],e=void 0,h;if(typeof k==="string")h=false;else{var g=i[a.charAt(0)];
if(g){e=a.match(g[1]);k=g[0]}else{for(h=0;h<l;++h){g=f[h];if(e=a.match(g[1])){k=g[0];break}}e||(k=z)}if((h=k.length>=5&&"lang-"===k.substring(0,5))&&!(e&&typeof e[1]==="string")){h=false;k=P}h||(p[a]=k)}g=m;m+=a.length;if(h){h=e[1];var s=a.indexOf(h),v=s+h.length;if(e[2]){v=a.length-e[2].length;s=v-h.length}k=k.substring(5);I(j+g,a.substring(0,s),n,q);I(j+g+s,h,Q(k,h),q);I(j+g+v,a.substring(v),n,q)}else q.push(j+g,k)}r.d=q}return n}function x(b){var f=[],i=[];if(b.tripleQuotedStrings)f.push([A,/^(?:\'\'\'(?:[^\'\\]|\\[\s\S]|\'{1,2}(?=[^\']))*(?:\'\'\'|$)|\"\"\"(?:[^\"\\]|\\[\s\S]|\"{1,2}(?=[^\"]))*(?:\"\"\"|$)|\'(?:[^\\\']|\\[\s\S])*(?:\'|$)|\"(?:[^\\\"]|\\[\s\S])*(?:\"|$))/,
null,"'\""]);else b.multiLineStrings?f.push([A,/^(?:\'(?:[^\\\']|\\[\s\S])*(?:\'|$)|\"(?:[^\\\"]|\\[\s\S])*(?:\"|$)|\`(?:[^\\\`]|\\[\s\S])*(?:\`|$))/,null,"'\"`"]):f.push([A,/^(?:\'(?:[^\\\'\r\n]|\\.)*(?:\'|$)|\"(?:[^\\\"\r\n]|\\.)*(?:\"|$))/,null,"\"'"]);b.verbatimStrings&&i.push([A,/^@\"(?:[^\"]|\"\")*(?:\"|$)/,null]);if(b.hashComments)if(b.cStyleComments){f.push([C,/^#(?:(?:define|elif|else|endif|error|ifdef|include|ifndef|line|pragma|undef|warning)\b|[^\r\n]*)/,null,"#"]);i.push([A,/^<(?:(?:(?:\.\.\/)*|\/?)(?:[\w-]+(?:\/[\w-]+)+)?[\w-]+\.h|[a-z]\w*)>/,
null])}else f.push([C,/^#[^\r\n]*/,null,"#"]);if(b.cStyleComments){i.push([C,/^\/\/[^\r\n]*/,null]);i.push([C,/^\/\*[\s\S]*?(?:\*\/|$)/,null])}b.regexLiterals&&i.push(["lang-regex",RegExp("^"+Z+"(/(?=[^/*])(?:[^/\\x5B\\x5C]|\\x5C[\\s\\S]|\\x5B(?:[^\\x5C\\x5D]|\\x5C[\\s\\S])*(?:\\x5D|$))+/)")]);b=b.keywords.replace(/^\s+|\s+$/g,"");b.length&&i.push([R,RegExp("^(?:"+b.replace(/\s+/g,"|")+")\\b"),null]);f.push([z,/^\s+/,null," \r\n\t\u00a0"]);i.push([J,/^@[a-z_$][a-z_$@0-9]*/i,null],[S,/^@?[A-Z]+[a-z][A-Za-z_$@0-9]*/,
null],[z,/^[a-z_$][a-z_$@0-9]*/i,null],[J,/^(?:0x[a-f0-9]+|(?:\d(?:_\d+)*\d*(?:\.\d*)?|\.\d\+)(?:e[+\-]?\d+)?)[a-z]*/i,null,"0123456789"],[E,/^.[^\s\w\.$@\'\"\`\/\#]*/,null]);return B(f,i)}function $(b){function f(D){if(D>r){if(j&&j!==q){n.push("</span>");j=null}if(!j&&q){j=q;n.push('<span class="',j,'">')}var T=y(p(i.substring(r,D))).replace(e?d:c,"$1&#160;");e=k.test(T);n.push(T.replace(a,s));r=D}}var i=b.source,o=b.g,l=b.d,n=[],r=0,j=null,q=null,m=0,t=0,p=Y(window.PR_TAB_WIDTH),c=/([\r\n ]) /g,
d=/(^| ) /gm,a=/\r\n?|\n/g,k=/[ \r\n]$/,e=true,h=window._pr_isIE6();h=h?b.b.tagName==="PRE"?h===6?"&#160;\r\n":h===7?"&#160;<br>\r":"&#160;\r":"&#160;<br />":"<br />";var g=b.b.className.match(/\blinenums\b(?::(\d+))?/),s;if(g){for(var v=[],w=0;w<10;++w)v[w]=h+'</li><li class="L'+w+'">';var F=g[1]&&g[1].length?g[1]-1:0;n.push('<ol class="linenums"><li class="L',F%10,'"');F&&n.push(' value="',F+1,'"');n.push(">");s=function(){var D=v[++F%10];return j?"</span>"+D+'<span class="'+j+'">':D}}else s=h;
for(;;)if(m<o.length?t<l.length?o[m]<=l[t]:true:false){f(o[m]);if(j){n.push("</span>");j=null}n.push(o[m+1]);m+=2}else if(t<l.length){f(l[t]);q=l[t+1];t+=2}else break;f(i.length);j&&n.push("</span>");g&&n.push("</li></ol>");b.a=n.join("")}function u(b,f){for(var i=f.length;--i>=0;){var o=f[i];if(G.hasOwnProperty(o))"console"in window&&console.warn("cannot override language handler %s",o);else G[o]=b}}function Q(b,f){b&&G.hasOwnProperty(b)||(b=/^\s*</.test(f)?"default-markup":"default-code");return G[b]}
function U(b){var f=b.f,i=b.e;b.a=f;try{var o,l=f.match(aa);f=[];var n=0,r=[];if(l)for(var j=0,q=l.length;j<q;++j){var m=l[j];if(m.length>1&&m.charAt(0)==="<"){if(!ba.test(m))if(ca.test(m)){f.push(m.substring(9,m.length-3));n+=m.length-12}else if(da.test(m)){f.push("\n");++n}else if(m.indexOf(V)>=0&&m.replace(/\s(\w+)\s*=\s*(?:\"([^\"]*)\"|'([^\']*)'|(\S+))/g,' $1="$2$3$4"').match(/[cC][lL][aA][sS][sS]=\"[^\"]*\bnocode\b/)){var t=m.match(W)[2],p=1,c;c=j+1;a:for(;c<q;++c){var d=l[c].match(W);if(d&&
d[2]===t)if(d[1]==="/"){if(--p===0)break a}else++p}if(c<q){r.push(n,l.slice(j,c+1).join(""));j=c}else r.push(n,m)}else r.push(n,m)}else{var a;p=m;var k=p.indexOf("&");if(k<0)a=p;else{for(--k;(k=p.indexOf("&#",k+1))>=0;){var e=p.indexOf(";",k);if(e>=0){var h=p.substring(k+3,e),g=10;if(h&&h.charAt(0)==="x"){h=h.substring(1);g=16}var s=parseInt(h,g);isNaN(s)||(p=p.substring(0,k)+String.fromCharCode(s)+p.substring(e+1))}}a=p.replace(ea,"<").replace(fa,">").replace(ga,"'").replace(ha,'"').replace(ia," ").replace(ja,
"&")}f.push(a);n+=a.length}}o={source:f.join(""),h:r};var v=o.source;b.source=v;b.c=0;b.g=o.h;Q(i,v)(b);$(b)}catch(w){if("console"in window)console.log(w&&w.stack?w.stack:w)}}var A="str",R="kwd",C="com",S="typ",J="lit",E="pun",z="pln",P="src",V="nocode",Z=function(){for(var b=["!","!=","!==","#","%","%=","&","&&","&&=","&=","(","*","*=","+=",",","-=","->","/","/=",":","::",";","<","<<","<<=","<=","=","==","===",">",">=",">>",">>=",">>>",">>>=","?","@","[","^","^=","^^","^^=","{","|","|=","||","||=",
"~","break","case","continue","delete","do","else","finally","instanceof","return","throw","try","typeof"],f="(?:^^|[+-]",i=0;i<b.length;++i)f+="|"+b[i].replace(/([^=<>:&a-z])/g,"\\$1");f+=")\\s*";return f}(),L=/&/g,M=/</g,N=/>/g,X=/\"/g,ea=/&lt;/g,fa=/&gt;/g,ga=/&apos;/g,ha=/&quot;/g,ja=/&amp;/g,ia=/&nbsp;/g,ka=/[\r\n]/g,K=null,aa=RegExp("[^<]+|<!--[\\s\\S]*?--\>|<!\\[CDATA\\[[\\s\\S]*?\\]\\]>|</?[a-zA-Z](?:[^>\"']|'[^']*'|\"[^\"]*\")*>|<","g"),ba=/^<\!--/,ca=/^<!\[CDATA\[/,da=/^<br\b/i,W=/^<(\/?)([a-zA-Z][a-zA-Z0-9]*)/,
la=x({keywords:"break continue do else for if return while auto case char const default double enum extern float goto int long register short signed sizeof static struct switch typedef union unsigned void volatile catch class delete false import new operator private protected public this throw true try typeof alignof align_union asm axiom bool concept concept_map const_cast constexpr decltype dynamic_cast explicit export friend inline late_check mutable namespace nullptr reinterpret_cast static_assert static_cast template typeid typename using virtual wchar_t where break continue do else for if return while auto case char const default double enum extern float goto int long register short signed sizeof static struct switch typedef union unsigned void volatile catch class delete false import new operator private protected public this throw true try typeof abstract boolean byte extends final finally implements import instanceof null native package strictfp super synchronized throws transient as base by checked decimal delegate descending event fixed foreach from group implicit in interface internal into is lock object out override orderby params partial readonly ref sbyte sealed stackalloc string select uint ulong unchecked unsafe ushort var break continue do else for if return while auto case char const default double enum extern float goto int long register short signed sizeof static struct switch typedef union unsigned void volatile catch class delete false import new operator private protected public this throw true try typeof debugger eval export function get null set undefined var with Infinity NaN caller delete die do dump elsif eval exit foreach for goto if import last local my next no our print package redo require sub undef unless until use wantarray while BEGIN END break continue do else for if return while and as assert class def del elif except exec finally from global import in is lambda nonlocal not or pass print raise try with yield False True None break continue do else for if return while alias and begin case class def defined elsif end ensure false in module next nil not or redo rescue retry self super then true undef unless until when yield BEGIN END break continue do else for if return while case done elif esac eval fi function in local set then until ",
hashComments:true,cStyleComments:true,multiLineStrings:true,regexLiterals:true}),G={};u(la,["default-code"]);u(B([],[[z,/^[^<?]+/],["dec",/^<!\w[^>]*(?:>|$)/],[C,/^<\!--[\s\S]*?(?:-\->|$)/],["lang-",/^<\?([\s\S]+?)(?:\?>|$)/],["lang-",/^<%([\s\S]+?)(?:%>|$)/],[E,/^(?:<[%?]|[%?]>)/],["lang-",/^<xmp\b[^>]*>([\s\S]+?)<\/xmp\b[^>]*>/i],["lang-js",/^<script\b[^>]*>([\s\S]*?)(<\/script\b[^>]*>)/i],["lang-css",/^<style\b[^>]*>([\s\S]*?)(<\/style\b[^>]*>)/i],["lang-in.tag",/^(<\/?[a-z][^<>]*>)/i]]),["default-markup",
"htm","html","mxml","xhtml","xml","xsl"]);u(B([[z,/^[\s]+/,null," \t\r\n"],["atv",/^(?:\"[^\"]*\"?|\'[^\']*\'?)/,null,"\"'"]],[["tag",/^^<\/?[a-z](?:[\w.:-]*\w)?|\/?>$/i],["atn",/^(?!style[\s=]|on)[a-z](?:[\w:-]*\w)?/i],["lang-uq.val",/^=\s*([^>\'\"\s]*(?:[^>\'\"\s\/]|\/(?=\s)))/],[E,/^[=<>\/]+/],["lang-js",/^on\w+\s*=\s*\"([^\"]+)\"/i],["lang-js",/^on\w+\s*=\s*\'([^\']+)\'/i],["lang-js",/^on\w+\s*=\s*([^\"\'>\s]+)/i],["lang-css",/^style\s*=\s*\"([^\"]+)\"/i],["lang-css",/^style\s*=\s*\'([^\']+)\'/i],
["lang-css",/^style\s*=\s*([^\"\'>\s]+)/i]]),["in.tag"]);u(B([],[["atv",/^[\s\S]+/]]),["uq.val"]);u(x({keywords:"break continue do else for if return while auto case char const default double enum extern float goto int long register short signed sizeof static struct switch typedef union unsigned void volatile catch class delete false import new operator private protected public this throw true try typeof alignof align_union asm axiom bool concept concept_map const_cast constexpr decltype dynamic_cast explicit export friend inline late_check mutable namespace nullptr reinterpret_cast static_assert static_cast template typeid typename using virtual wchar_t where ",
hashComments:true,cStyleComments:true}),["c","cc","cpp","cxx","cyc","m"]);u(x({keywords:"null true false"}),["json"]);u(x({keywords:"break continue do else for if return while auto case char const default double enum extern float goto int long register short signed sizeof static struct switch typedef union unsigned void volatile catch class delete false import new operator private protected public this throw true try typeof abstract boolean byte extends final finally implements import instanceof null native package strictfp super synchronized throws transient as base by checked decimal delegate descending event fixed foreach from group implicit in interface internal into is lock object out override orderby params partial readonly ref sbyte sealed stackalloc string select uint ulong unchecked unsafe ushort var ",
hashComments:true,cStyleComments:true,verbatimStrings:true}),["cs"]);u(x({keywords:"break continue do else for if return while auto case char const default double enum extern float goto int long register short signed sizeof static struct switch typedef union unsigned void volatile catch class delete false import new operator private protected public this throw true try typeof abstract boolean byte extends final finally implements import instanceof null native package strictfp super synchronized throws transient ",
cStyleComments:true}),["java"]);u(x({keywords:"break continue do else for if return while case done elif esac eval fi function in local set then until ",hashComments:true,multiLineStrings:true}),["bsh","csh","sh"]);u(x({keywords:"break continue do else for if return while and as assert class def del elif except exec finally from global import in is lambda nonlocal not or pass print raise try with yield False True None ",hashComments:true,multiLineStrings:true,tripleQuotedStrings:true}),["cv","py"]);
u(x({keywords:"caller delete die do dump elsif eval exit foreach for goto if import last local my next no our print package redo require sub undef unless until use wantarray while BEGIN END ",hashComments:true,multiLineStrings:true,regexLiterals:true}),["perl","pl","pm"]);u(x({keywords:"break continue do else for if return while alias and begin case class def defined elsif end ensure false in module next nil not or redo rescue retry self super then true undef unless until when yield BEGIN END ",hashComments:true,
multiLineStrings:true,regexLiterals:true}),["rb"]);u(x({keywords:"break continue do else for if return while auto case char const default double enum extern float goto int long register short signed sizeof static struct switch typedef union unsigned void volatile catch class delete false import new operator private protected public this throw true try typeof debugger eval export function get null set undefined var with Infinity NaN ",cStyleComments:true,regexLiterals:true}),["js"]);u(B([],[[A,/^[\s\S]+/]]),
["regex"]);window.PR_normalizedHtml=H;window.prettyPrintOne=function(b,f){var i={f:b,e:f};U(i);return i.a};window.prettyPrint=function(b){function f(){for(var t=window.PR_SHOULD_USE_CONTINUATION?j.now()+250:Infinity;q<o.length&&j.now()<t;q++){var p=o[q];if(p.className&&p.className.indexOf("prettyprint")>=0){var c=p.className.match(/\blang-(\w+)\b/);if(c)c=c[1];for(var d=false,a=p.parentNode;a;a=a.parentNode)if((a.tagName==="pre"||a.tagName==="code"||a.tagName==="xmp")&&a.className&&a.className.indexOf("prettyprint")>=
0){d=true;break}if(!d){a=p;if(null===K){d=document.createElement("PRE");d.appendChild(document.createTextNode('<!DOCTYPE foo PUBLIC "foo bar">\n<foo />'));K=!/</.test(d.innerHTML)}if(K){d=a.innerHTML;if("XMP"===a.tagName)d=y(d);else{a=a;if("PRE"===a.tagName)a=true;else if(ka.test(d)){var k="";if(a.currentStyle)k=a.currentStyle.whiteSpace;else if(window.getComputedStyle)k=window.getComputedStyle(a,null).whiteSpace;a=!k||k==="pre"}else a=true;a||(d=d.replace(/(<br\s*\/?>)[\r\n]+/g,"$1").replace(/(?:[\r\n]+[ \t]*)+/g,
" "))}d=d}else{d=[];for(a=a.firstChild;a;a=a.nextSibling)H(a,d);d=d.join("")}d=d.replace(/(?:\r\n?|\n)$/,"");m={f:d,e:c,b:p};U(m);if(p=m.a){c=m.b;if("XMP"===c.tagName){d=document.createElement("PRE");for(a=0;a<c.attributes.length;++a){k=c.attributes[a];if(k.specified)if(k.name.toLowerCase()==="class")d.className=k.value;else d.setAttribute(k.name,k.value)}d.innerHTML=p;c.parentNode.replaceChild(d,c)}else c.innerHTML=p}}}}if(q<o.length)setTimeout(f,250);else b&&b()}for(var i=[document.getElementsByTagName("pre"),
document.getElementsByTagName("code"),document.getElementsByTagName("xmp")],o=[],l=0;l<i.length;++l)for(var n=0,r=i[l].length;n<r;++n)o.push(i[l][n]);i=null;var j=Date;j.now||(j={now:function(){return(new Date).getTime()}});var q=0,m;f()};window.PR={combinePrefixPatterns:O,createSimpleLexer:B,registerLangHandler:u,sourceDecorator:x,PR_ATTRIB_NAME:"atn",PR_ATTRIB_VALUE:"atv",PR_COMMENT:C,PR_DECLARATION:"dec",PR_KEYWORD:R,PR_LITERAL:J,PR_NOCODE:V,PR_PLAIN:z,PR_PUNCTUATION:E,PR_SOURCE:P,PR_STRING:A,
PR_TAG:"tag",PR_TYPE:S}})()

;

// lang-sql.js
// http://code.google.com/p/google-code-prettify/

PR.registerLangHandler(PR.createSimpleLexer([["pln",/^[\t\n\r \xA0]+/,null,"\t\n\r \u00a0"],["str",/^(?:"(?:[^\"\\]|\\.)*"|'(?:[^\'\\]|\\.)*')/,null,"\"'"]],[["com",/^(?:--[^\r\n]*|\/\*[\s\S]*?(?:\*\/|$))/],["kwd",/^(?:ADD|ALL|ALTER|AND|ANY|AS|ASC|AUTHORIZATION|BACKUP|BEGIN|BETWEEN|BREAK|BROWSE|BULK|BY|CASCADE|CASE|CHECK|CHECKPOINT|CLOSE|CLUSTERED|COALESCE|COLLATE|COLUMN|COMMIT|COMPUTE|CONSTRAINT|CONTAINS|CONTAINSTABLE|CONTINUE|CONVERT|CREATE|CROSS|CURRENT|CURRENT_DATE|CURRENT_TIME|CURRENT_TIMESTAMP|CURRENT_USER|CURSOR|DATABASE|DBCC|DEALLOCATE|DECLARE|DEFAULT|DELETE|DENY|DESC|DISK|DISTINCT|DISTRIBUTED|DOUBLE|DROP|DUMMY|DUMP|ELSE|END|ERRLVL|ESCAPE|EXCEPT|EXEC|EXECUTE|EXISTS|EXIT|FETCH|FILE|FILLFACTOR|FOR|FOREIGN|FREETEXT|FREETEXTTABLE|FROM|FULL|FUNCTION|GOTO|GRANT|GROUP|HAVING|HOLDLOCK|IDENTITY|IDENTITYCOL|IDENTITY_INSERT|IF|IN|INDEX|INNER|INSERT|INTERSECT|INTO|IS|JOIN|KEY|KILL|LEFT|LIKE|LINENO|LOAD|NATIONAL|NOCHECK|NONCLUSTERED|NOT|NULL|NULLIF|OF|OFF|OFFSETS|ON|OPEN|OPENDATASOURCE|OPENQUERY|OPENROWSET|OPENXML|OPTION|OR|ORDER|OUTER|OVER|PERCENT|PLAN|PRECISION|PRIMARY|PRINT|PROC|PROCEDURE|PUBLIC|RAISERROR|READ|READTEXT|RECONFIGURE|REFERENCES|REPLICATION|RESTORE|RESTRICT|RETURN|REVOKE|RIGHT|ROLLBACK|ROWCOUNT|ROWGUIDCOL|RULE|SAVE|SCHEMA|SELECT|SESSION_USER|SET|SETUSER|SHUTDOWN|SOME|STATISTICS|SYSTEM_USER|TABLE|TEXTSIZE|THEN|TO|TOP|TRAN|TRANSACTION|TRIGGER|TRUNCATE|TSEQUAL|UNION|UNIQUE|UPDATE|UPDATETEXT|USE|USER|VALUES|VARYING|VIEW|WAITFOR|WHEN|WHERE|WHILE|WITH|WRITETEXT)(?=[^\w-]|$)/i,
null],["lit",/^[+-]?(?:0x[\da-f]+|(?:(?:\.\d+|\d+(?:\.\d*)?)(?:e[+\-]?\d+)?))/i],["pln",/^[a-z_][\w-]*/i],["pun",/^[^\w\t\n\r \xA0\"\'][^\w\t\n\r \xA0+\-\"\']*/]]),["sql"])

;