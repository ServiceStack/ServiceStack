var jsonviewer = (function(win) {
    var cssText =
      ".jsonviewer TABLE { border-collapse:collapse; border: solid 1px #ccc; clear: left; }\r\n" +
      ".jsonviewer TH { text-align: left; padding: 4px 8px; text-shadow: #fff 1px 1px -1px; background: #f1f1f1; white-space:nowrap; cursor:pointer; font-weight: bold; }\r\n" +
      ".jsonviewer TH, .jsonviewer TD, .jsonviewer TD DT, .jsonviewer TD DD { }\r\n" +
      ".jsonviewer TD { padding: 8px 8px 0 8px; vertical-align: top; }\r\n" +
      ".jsonviewer DL { margin: 0; clear: left; }\r\n" +
      ".jsonviewer DT { font-weight: bold; width: 160px; clear: left; float: left; display:block; white-space:nowrap; }\r\n" +
      ".jsonviewer DD { display: block; float: left; }\r\n" +
      ".jsonviewer DL DL DT { font-weight: bold; }\r\n" +
      ".jsonviewer DL DL DD { }\r\n" +
      ".jsonviewer HR { display:none; }\r\n" +
      ".jsonviewer TD DL HR { display:block; padding: 0; clear: left; border: none; }\r\n" +
      ".jsonviewer TD DL { padding: 4px; margin: 0; height:100%; max-width: 700px; }\r\n" +
      ".jsonviewer DL TD DL DT { padding: 2px; margin: 0 10px 0 0; font-weight: bold; width: 120px; overflow: hidden; clear: left; float: left; display:block; }\r\n" +
      ".jsonviewer DL TD DL DD { margin: 0; padding: 2px; display: block; float: left; }\r\n" +
      ".jsonviewer TBODY>TR:last-child>TD { padding: 8px; }\r\n" +
      ".jsonviewer THEAD { -webkit-user-select:none; -moz-user-select:none; }\r\n" +
      ".jsonviewer .desc, .jsonviewer .asc { background-color: #FAFAD2; }\r\n" +
      ".jsonviewer .desc { background-color: #D4EDC9; }\r\n" +
      ".jsonviewer TH B { display:block; float:right; margin: 0 0 0 5px; width: 0; height: 0; border-left: 5px solid transparent; border-right: 5px solid transparent; border-top: 5px solid #ccc; border-bottom: none; }\r\n" +
      ".jsonviewer .asc B { border-left: 5px solid transparent; border-right: 5px solid transparent; border-top: 5px solid #333; border-bottom: none; }\r\n" +
      ".jsonviewer .desc B { border-left: 5px solid transparent; border-right: 5px solid transparent; border-bottom: 5px solid #333; border-top: none; }\r\n" +
      ".jsonviewer H3 { margin: 0 0 10px 0; }";

    document.write('<style type="text/css">' + cssText + '</style>\r\n');

    var doc = document,
        $ = function(id) { return doc.getElementById(id); },
        $$ = function(sel) { return doc.getElementsByTagName(sel); },
        $each = function(fn) { for (var i=0,len=this.length; i<len; i++) fn(i, this[i], this); },
        isIE = /msie/i.test(navigator.userAgent) && !/opera/i.test(navigator.userAgent);

    $.each = function(arr, fn) { $each.call(arr, fn); };

    var splitCase = function (t) { return typeof t != 'string' ? t : t.replace(/([A-Z]|[0-9]+)/g, ' $1'); },
        uniqueKeys = function (m) { var h = {}; for (var i = 0, len = m.length; i < len; i++) for (var k in m[i]) if (show(k)) h[k] = k; return h; },
        keys = function (o) { var a = []; for (var k in o) if (show(k)) a.push(k); return a; };
    var tbls = [];

    function val(m) {
        if (m == null) return '';
        if (typeof m == 'number') return num(m);
        if (typeof m == 'string') return str(m);
        if (typeof m == 'boolean') return m ? 'true' : 'false';
        return m.length ? arr(m) : obj(m);
    }
    function num(m) { return m; }
    function strFact(showFullDate){

        function shortDate(m){
            return m.substr(0,6) == '/Date(' ? dmft(date(m)) : m;
        }

        function fullDate(m){
            return m.substr(0,6) == '/Date(' ? dmfthm(date(m)) : m;
        }
        return showFullDate ? fullDate : shortDate;  
    }
    str = strFact(location.hash.indexOf('show=') != -1 && location.hash.indexOf('fulldates') != -1);
    function date(s) { return new Date(parseFloat(/Date\(([^)]+)\)/.exec(s)[1])); }
    function pad(d) { return d < 10 ? '0'+d : d; }
    function dmft(d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()); }
    function dmfthm(d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()) + ' ' + pad(d.getHours()) + ":" + pad(d.getMinutes()); }
    function show(k) { return typeof k != 'string' || k.substr(0,2) != '__'; }
    function obj(m) {
        var sb = '<dl>';
        for (var k in m) if (show(k)) sb += '<dt class="ib">' + splitCase(k) + '</dt><dd>' + val(m[k]) + '</dd>';
        sb += '</dl>';
        return sb;
    }
    function arr(m) {
        if (typeof m[0] == 'string' || typeof m[0] == 'number') return m.join(', ');
        var id=tbls.length, h=uniqueKeys(m);
        var sb = '<table id="tbl-' + id + '"><caption></caption><thead><tr>';
        tbls.push(m);
        var i=0;
        for (var k in h) sb += '<th id="h-' + id + '-' + (i++) + '"><b></b>' + splitCase(k) + '</th>';
        sb += '</tr></thead><tbody>' + makeRows(h,m) + '</tbody></table>';
        return sb;
    }

    function makeRows(h,m) {
        var sb = '';
        for (var r=0,len=m.length; r<len; r++) {
            sb += '<tr>';
            var row = m[r];
            for (var k in h) if (show(k)) sb += '<td>' + val(row[k]) + '</td>';
            sb += '</tr>';
        }  
        return sb;
    }

    doc.onclick = function(e) {
        e = e || window.event, el = e.target || e.srcElement, cls = el.className;
        if (el.tagName == 'B') el = el.parentNode;
        if (el.tagName != 'TH') return;
        el.className = cls == 'asc' ? 'desc' : (cls == 'desc' ? null : 'asc');
        $.each($$('TH'), function(i,th){ if (th == el) return; th.className = null; });
        clearSel();
        var ids=el.id.split('-'), tId=ids[1], cId=ids[2];
        if (!tbls[tId]) return;
        var tbl=tbls[tId].slice(0), h=uniqueKeys(tbl), col=keys(h)[cId], tbody=el.parentNode.parentNode.nextSibling;
        if (!el.className){ setTableBody(tbody, makeRows(h,tbls[tId])); return; }
        var d=el.className=='asc'?1:-1;
        tbl.sort(function(a,b){ return cmp(a[col],b[col]) * d; });
        setTableBody(tbody, makeRows(h,tbl));
    }

    function setTableBody(tbody, html) {
        if (!isIE) { tbody.innerHTML = html; return; }
        var temp = tbody.ownerDocument.createElement('div');
        temp.innerHTML = '<table>' + html + '</table>';
        tbody.parentNode.replaceChild(temp.firstChild.firstChild, tbody);
    }

    function clearSel() {
        if (doc.selection && doc.selection.empty) doc.selection.empty();
        else if(win.getSelection) {
            var sel=win.getSelection();
            if (sel && sel.removeAllRanges) sel.removeAllRanges();
        }
    }

    function cmp(v1, v2){
        let f1=parseFloat(v1), f2=parseFloat(v2)
        if (!isNaN(f1) && !isNaN(f2)) { v1=f1; v2=f2 }
        if (typeof v1 == 'string' && v1.substr(0,6) === '/Date(') { v1=date(v1); v2=date(v2) }
        if (v1 === v2) return 0
        return v1 > v2 ? 1 : -1
    }

    return val;
})(window);