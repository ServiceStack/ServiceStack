/**
 * Created by demis.bellot@gmail.com
 * Open Source under the New BSD Licence: https://github.com/AjaxStack/AjaxStack/blob/master/LICENSE
 */

//for non-modern browsers i.e: <=IE8
!window.JSON && document.write(unescape('%3Cscript src=""http://ajax.cdnjs.com/ajax/libs/json2/20110223/json2.js""%3E%3C/script%3E'));

var cssText =
  ".jsonreport TABLE { border-collapse:collapse; border: solid 1px #ccc; clear: left; }\r\n" +
  ".jsonreport TH { text-align: left; padding: 4px 8px; text-shadow: #fff 1px 1px -1px; background: #f1f1f1; white-space:nowrap; cursor:pointer; font-weight: bold; }\r\n" +
  ".jsonreport TH, .jsonreport TD, .jsonreport TD DT, .jsonreport TD DD { font-size: 13px; font-family: Arial; }\r\n" +
  ".jsonreport TD { padding: 8px 8px 0 8px; vertical-align: top; }\r\n" +
  ".jsonreport DL { clear: left; }\r\n" +
  ".jsonreport DT { margin: 10px 0 5px 0; font: bold 18px Helvetica, Verdana, Arial; width: 200px; clear: left; float: left; display:block; white-space:nowrap; }\r\n" +
  ".jsonreport DD { margin: 5px 10px; font: 18px Arial; padding: 2px; display: block; float: left; }\r\n" +
  ".jsonreport DL DL DT { font: bold 16px Arial; }\r\n" +
  ".jsonreport DL DL DD { font: 16px Arial; }\r\n" +
  ".jsonreport HR { display:none; }\r\n" +
  ".jsonreport TD DL HR { display:block; padding: 0; clear: left; border: none; }\r\n" +
  ".jsonreport TD DL { padding: 4px; margin: 0; height:100%; max-width: 700px; }\r\n" +
  ".jsonreport DL TD DL DT { padding: 2px; margin: 0 10px 0 0; font-weight: bold; font-size: 13px; width: 120px; overflow: hidden; clear: left; float: left; display:block; }\r\n" +
  ".jsonreport DL TD DL DD { margin: 0; padding: 2px; font-size: 13px; display: block; float: left; }\r\n" +
  ".jsonreport TBODY>TR:last-child>TD { padding: 8px; }\r\n" +
  ".jsonreport THEAD { -webkit-user-select:none; -moz-user-select:none; }\r\n" +
  ".jsonreport .desc, .jsonreport .asc { background-color: #FAFAD2; }\r\n" +
  ".jsonreport .desc { background-color: #D4EDC9; }\r\n" +
  ".jsonreport TH B { display:block; float:right; margin: 0 0 0 5px; width: 0; height: 0; border-left: 5px solid transparent; border-right: 5px solid transparent; border-top: 5px solid #ccc; border-bottom: none; }\r\n" +
  ".jsonreport .asc B { border-left: 5px solid transparent; border-right: 5px solid transparent; border-top: 5px solid #333; border-bottom: none; }\r\n" +
  ".jsonreport .desc B { border-left: 5px solid transparent; border-right: 5px solid transparent; border-bottom: 5px solid #333; border-top: none; }\r\n" +
  ".jsonreport H3 { font-size: 18px; margin: 0 0 10px 0; }";

document.write('<style type="text/css">' + cssText + '</style>\r\n');

if (!_) let _ = {};
_.jsonreport = (function(){
    let root = this, doc = document,
        $ = function(id) { return doc.getElementById(id); },
        $$ = function(sel) { return doc.getElementsByTagName(sel); },
        $each = function(fn) { for (var i=0,len=this.length; i<len; i++) fn(i, this[i], this); },
        isIE = /msie/i.test(navigator.userAgent) && !/opera/i.test(navigator.userAgent);

    $.each = function(arr, fn) { $each.call(arr, fn); };

    let splitCase = function(t) { return typeof t != 'string' ? t : t.replace(/([A-Z]|[0-9]+)/g, ' $1').replace(/_/g,' '); },
        uniqueKeys = function(m){ let h={}; for (let i=0,len=m.length; i<len; i++) for (let k in m[i]) h[k] = k; return h; },
        keys = function(o){ let a=[]; for (let k in o) a.push(k); return a; }
    let tbls = [];

    function val(m) {
      if (m == null) return '';
      if (typeof m == 'number') return num(m);
      if (typeof m == 'string') return str(m);
      if (typeof m == 'boolean') return m ? 'true' : 'false';
      return m.length ? arr(m) : obj(m);
    }
    function num(m) { return m; }
    function str(m) {
      return m.substr(0,6) === '/Date(' ? dfmt(date(m)) : m;
    }
    function date(s) { return new Date(parseFloat(/Date\(([^)]+)\)/.exec(s)[1])); }
    function pad(d) { return d < 10 ? '0'+d : d; }
    function dfmt(d) { return d.getFullYear() + '/' + pad(d.getMonth() + 1) + '/' + pad(d.getDate()); }
    function obj(m) {
      let sb = '<dl>';
      for (let k in m) sb += '<dt class="ib">' + splitCase(k) + '</dt><dd>' + val(m[k]) + '</dd>';
      sb += '</dl>';
      return sb;
    }
    function arr(m) {
      if (typeof m[0] == 'string' || typeof m[0] == 'number') return m.join(', ');
      let id=tbls.length, h=uniqueKeys(m);
      let sb = '<table id="tbl-' + id + '"><caption></caption><thead><tr>';
      tbls.push(m);
      let i=0;
      for (let k in h) sb += '<th id="h-' + id + '-' + (i++) + '"><b></b>' + splitCase(k) + '</th>';
      sb += '</tr></thead><tbody>' + makeRows(h,m) + '</tbody></table>';
      return sb;
    }

    function makeRows(h,m) {
      let sb = '';
      for (let r=0,len=m.length; r<len; r++) {
        sb += '<tr>';
        let row = m[r];
        for (let k in h) sb += '<td>' + val(row[k]) + '</td>';
        sb += '</tr>';
      }
      return sb;
    }

    function setTableBody(tbody, html) {
      if (!isIE) { tbody.innerHTML = html; return; }
      let temp = tbody.ownerDocument.createElement('div');
      temp.innerHTML = '<table>' + html + '</table>';
      tbody.parentNode.replaceChild(temp.firstChild.firstChild, tbody);
    }

    function clearSel() {
      if (doc.selection && doc.selection.empty) doc.selection.empty();
      else if(root.getSelection) {
        let sel=root.getSelection();
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

    function enc(html) {
      if (typeof html != 'string') return html;
      return html.replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    function addEvent(obj, type, fn) {
      if (obj.attachEvent) {
        obj['e'+type+fn] = fn;
        obj[type+fn] = function(){obj['e'+type+fn]( root.event );}
        obj.attachEvent( 'on'+type, obj[type+fn] );
      } else
        obj.addEventListener( type, fn, false );
    }

    addEvent(doc, 'click', function (e) {
        e = e || root.event
        let el = e.target || e.srcElement, cls = el.className
        if (el.tagName === 'B') el = el.parentNode
        if (el.tagName !== 'TH') return
        el.className = cls === 'asc' ? 'desc' : (cls === 'desc' ? null : 'asc')
        $.each($$('TH'), function(i,th){ if (th === el) return; th.className = null; })
        clearSel()
        let ids=el.id.split('-'), tId=ids[1], cId=ids[2]
        let tbl=tbls[tId].slice(0), h=uniqueKeys(tbl), col=keys(h)[cId], tbody=el.parentNode.parentNode.nextSibling
        if (!el.className){ setTableBody(tbody, makeRows(h,tbls[tId])); return }
        let d=el.className==='asc'?1:-1
        tbl.sort(function(a,b){ return cmp(a[col],b[col]) * d; })
        setTableBody(tbody, makeRows(h,tbl))
    })

    return function(json) {
        let model = typeof json == "string" ? JSON.parse(json) : json;
        return val(model);
    };
})();

