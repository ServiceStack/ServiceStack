using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace ServiceStack.Html
{
    //C# Port from: http://blog.magerquark.de/c-port-of-googles-htmlcompressor-library/
    public static class HtmlCompressorExtensions
    {
        public static void AddPreservePattern(this HtmlCompressor compressor, params Regex[] regexes)
        {
            var org = compressor.PreservePatterns;

            var preservePatterns = new List<Regex>();
            if (org != null) preservePatterns.AddRange(org);
            preservePatterns.AddRange(regexes);

            compressor.PreservePatterns = preservePatterns;
        }
    }

    // Original von: https://code.google.com/p/htmlcompressor/
    // Diese Datei von https://code.google.com/p/htmlcompressor/source/browse/trunk/src/main/java/com/googlecode/htmlcompressor/compressor/HtmlCompressor.java
    // Tipps auf http://stackoverflow.com/questions/3789472/what-is-the-c-sharp-regex-equivalent-to-javas-appendreplacement-and-appendtail
    // Java-Regex auf http://www.devarticles.com/c/a/Java/Introduction-to-the-Javautilregex-Object-Model/8/

    /**
     * Class that compresses given HTML source by removing comments, extra spaces and 
     * line breaks while preserving content within &lt;pre>, &lt;textarea>, &lt;script> 
     * and &lt;style> tags. 
     * <p>Blocks that should be additionally preserved could be marked with:
     * <br><code>&lt;!-- {{{ -->
     * <br>&nbsp;&nbsp;&nbsp;&nbsp;...
     * <br>&lt;!-- }}} --></code> 
     * <br>or any number of user defined patterns. 
     * <p>Content inside &lt;script> or &lt;style> tags could be optionally compressed using 
     * <a href="http://developer.yahoo.com/yui/compressor/">Yahoo YUI ICompressor</a> or <a href="http://code.google.com/closure/compiler/">Google Closure Compiler</a>
     * libraries.
     * 
     * @author <a href="mailto:serg472@gmail.com">Sergiy Kovalchuk</a>
     */

    public sealed class HtmlCompressor : ICompressor
    {
        //public static readonly string JS_COMPRESSOR_YUI = "yui";
        //public static readonly string JS_COMPRESSOR_CLOSURE = "closure";

        /**
         * Predefined pattern that matches <code>&lt;?php ... ?></code> tags. 
         * Could be passed inside a list to {@link #setPreservePatterns(List) setPreservePatterns} method.
         */

        public static readonly Regex PHP_TAG_PATTERN = new Regex("<\\?php.*?\\?>",
                                                                 RegexOptions.Singleline | RegexOptions.IgnoreCase);

        /**
         * Predefined pattern that matches <code>&lt;% ... %></code> tags. 
         * Could be passed inside a list to {@link #setPreservePatterns(List) setPreservePatterns} method.
         */
        public static readonly Regex SERVER_SCRIPT_TAG_PATTERN = new Regex("<%.*?%>", RegexOptions.Singleline);

        /**
         * Predefined pattern that matches <code>&lt;--# ... --></code> tags. 
         * Could be passed inside a list to {@link #setPreservePatterns(List) setPreservePatterns} method.
         */
        public static readonly Regex SERVER_SIDE_INCLUDE_PATTERN = new Regex("<!--\\s*#.*?-->", RegexOptions.Singleline);

        /**
         * Predefined list of tags that are very likely to be block-level. 
         * Could be passed to {@link #setRemoveSurroundingSpaces(string) setRemoveSurroundingSpaces} method.
         */
        public static readonly string BLOCK_TAGS_MIN = "html,head,body,br,p";

        /**
         * Predefined list of tags that are block-level by default, excluding <code>&lt;div></code> and <code>&lt;li></code> tags. 
         * Table tags are also included.
         * Could be passed to {@link #setRemoveSurroundingSpaces(string) setRemoveSurroundingSpaces} method.
         */

        public static readonly string BLOCK_TAGS_MAX = BLOCK_TAGS_MIN +
                                                       ",h1,h2,h3,h4,h5,h6,blockquote,center,dl,fieldset,form,frame,frameset,hr,noframes,ol,table,tbody,tr,td,th,tfoot,thead,ul";

        /**
         * Could be passed to {@link #setRemoveSurroundingSpaces(string) setRemoveSurroundingSpaces} method 
         * to remove all surrounding spaces (not recommended).
         */
        public static readonly string ALL_TAGS = "all";

        /**
         * If set to <code>false</code> all compression will be bypassed. Might be useful for testing purposes. 
         * Default is <code>true</code>.
         * 
         * @param enabled set <code>false</code> to bypass all compression
         */
        public bool Enabled = true;

        /**
         * Gets or Sets JavaScript compressor implementation that will be used 
         * to compress inline JavaScript in HTML. 
         */
        public ICompressor JavaScriptCompressor = null;

        /**
         * Returns CSS compressor implementation that will be used 
         * to compress inline CSS in HTML.
         */
        public ICompressor CssCompressor = null;


        //default settings
        /**
         * If set to <code>true</code> all HTML comments will be removed.   
         * Default is <code>true</code>.
         * 
         * @param removeComments set <code>true</code> to remove all HTML comments
         */
        public bool RemoveComments = true;

        /**
         * If set to <code>true</code> all multiple whitespace characters will be replaced with single spaces.
         * Default is <code>true</code>.
         * 
         * @param removeMultiSpaces set <code>true</code> to replace all multiple whitespace characters 
         * will single spaces.
         */
        public bool RemoveMultiSpaces = true;

        //optional settings

        /**
         * If set to <code>true</code> all inter-tag whitespace characters will be removed.
         * Default is <code>false</code>.
         * 
         * <p><b>Note:</b> It is fairly safe to turn this option on unless you 
         * rely on spaces for page formatting. Even if you do, you can always preserve 
         * required spaces with <code>&amp;nbsp;</code>. This option has no performance impact.    
         * 
         * @param removeIntertagSpaces set <code>true</code> to remove all inter-tag whitespace characters
         */
        public bool RemoveIntertagSpaces = false;

        /**
         * If set to <code>true</code> all unnecessary quotes will be removed  
         * from tag attributes. Default is <code>false</code>.
         * 
         * <p><b>Note:</b> Even though quotes are removed only when it is safe to do so, 
         * it still might break strict HTML validation. Turn this option on only if 
         * a page validation is not very important or to squeeze the most out of the compression.
         * This option has no performance impact. 
         * 
         * @param removeQuotes set <code>true</code> to remove unnecessary quotes from tag attributes
         */
        public bool RemoveQuotes = false;

        /**
         * Enables JavaScript compression within &lt;script> tags 
         * if set to <code>true</code>. Default is <code>false</code> for performance reasons.
         *  
         * <p><b>Note:</b> Compressing JavaScript is not recommended if pages are 
         * compressed dynamically on-the-fly because of performance impact. 
         * You should consider putting JavaScript into a separate file and
         * compressing it using standalone YUICompressor for example.</p>
         * 
         * @param compressJavaScript set <code>true</code> to enable JavaScript compression. 
         * Default is <code>false</code>
         */
        public bool CompressJavaScript = false;

        /**
         * Enables CSS compression within &lt;style> tags using 
         * <a href="http://developer.yahoo.com/yui/compressor/">Yahoo YUI ICompressor</a> 
         * if set to <code>true</code>. Default is <code>false</code> for performance reasons.
         *  
         * <p><b>Note:</b> Compressing CSS is not recommended if pages are 
         * compressed dynamically on-the-fly because of performance impact. 
         * You should consider putting CSS into a separate file and
         * compressing it using standalone YUICompressor for example.</p>
         * 
         * @param compressCss set <code>true</code> to enable CSS compression. 
         * Default is <code>false</code>
         */
        public bool CompressCss = false;

        /**
         * If set to <code>true</code>, existing DOCTYPE declaration will be replaced with simple <code>&lt;!DOCTYPE html></code> declaration.
         * Default is <code>false</code>.
         * 
         * @param simpleDoctype set <code>true</code> to replace existing DOCTYPE declaration with <code>&lt;!DOCTYPE html></code>
         */
        public bool SimpleDoctype = false;

        /**
         * If set to <code>true</code>, following attributes will be removed from <code>&lt;script></code> tags: 
         * <ul>
         * <li>type="text/javascript"</li>
         * <li>type="application/javascript"</li>
         * <li>language="javascript"</li>
         * </ul>
         * 
         * <p>Default is <code>false</code>.
         * 
         * @param removeScriptAttributes set <code>true</code> to remove unnecessary attributes from <code>&lt;script></code> tags 
         */
        public bool RemoveScriptAttributes = false;

        /**
         * If set to <code>true</code>, <code>type="text/style"</code> attributes will be removed from <code>&lt;style></code> tags. Default is <code>false</code>.
         * 
         * @param removeStyleAttributes set <code>true</code> to remove <code>type="text/style"</code> attributes from <code>&lt;style></code> tags
         */
        public bool RemoveStyleAttributes = false;

        /**
         * If set to <code>true</code>, following attributes will be removed from <code>&lt;link rel="stylesheet"></code> and <code>&lt;link rel="alternate stylesheet"></code> tags: 
         * <ul>
         * <li>type="text/css"</li>
         * <li>type="text/plain"</li>
         * </ul>
         * 
         * <p>Default is <code>false</code>.
         * 
         * @param removeLinkAttributes set <code>true</code> to remove unnecessary attributes from <code>&lt;link></code> tags
         */
        public bool RemoveLinkAttributes = false;

        /**
         * If set to <code>true</code>, <code>method="get"</code> attributes will be removed from <code>&lt;form></code> tags. Default is <code>false</code>.
         * 
         * @param removeFormAttributes set <code>true</code> to remove <code>method="get"</code> attributes from <code>&lt;form></code> tags
         */
        public bool RemoveFormAttributes = false;

        /**
         * If set to <code>true</code>, <code>type="text"</code> attributes will be removed from <code>&lt;input></code> tags. Default is <code>false</code>.
         * 
         * @param removeInputAttributes set <code>true</code> to remove <code>type="text"</code> attributes from <code>&lt;input></code> tags
         */
        public bool RemoveInputAttributes = false;

        /**
         * If set to <code>true</code>, any values of following bool attributes will be removed:
         * <ul>
         * <li>checked</li>
         * <li>selected</li>
         * <li>disabled</li>
         * <li>readonly</li>
         * </ul>
         * 
         * <p>For example, <code>&ltinput readonly="readonly"></code> would become <code>&ltinput readonly></code>
         * 
         * <p>Default is <code>false</code>.
         * 
         * @param simpleBooleanAttributes set <code>true</code> to simplify bool attributes
         */
        public bool SimpleBooleanAttributes = false;

        /**
         * If set to <code>true</code>, <code>javascript:</code> pseudo-protocol will be removed from inline event handlers.
         * 
         * <p>For example, <code>&lta onclick="javascript:alert()"></code> would become <code>&lta onclick="alert()"></code>
         * 
         * <p>Default is <code>false</code>.
         * 
         * @param removeJavaScriptProtocol set <code>true</code> to remove <code>javascript:</code> pseudo-protocol from inline event handlers.
         */
        public bool RemoveJavaScriptProtocol = false;

        /**
         * If set to <code>true</code>, <code>HTTP</code> protocol will be removed from <code>href</code>, <code>src</code>, <code>cite</code>, and <code>action</code> tag attributes.
         * URL without a protocol would make a browser use document's current protocol instead. 
         * 
         * <p>Tags marked with <code>rel="external"</code> will be skipped.
         * 
         * <p>For example: 
         * <p><code>&lta href="http://example.com"> &ltscript src="http://google.com/js.js" rel="external"></code> 
         * <p>would become: 
         * <p><code>&lta href="//example.com"> &ltscript src="http://google.com/js.js" rel="external"></code>
         * 
         * <p>Default is <code>false</code>.
         * 
         * @param removeHttpProtocol set <code>true</code> to remove <code>HTTP</code> protocol from tag attributes
         */
        public bool RemoveHttpProtocol = false;

        /**
         * If set to <code>true</code>, <code>HTTPS</code> protocol will be removed from <code>href</code>, <code>src</code>, <code>cite</code>, and <code>action</code> tag attributes.
         * URL without a protocol would make a browser use document's current protocol instead.
         * 
         * <p>Tags marked with <code>rel="external"</code> will be skipped.
         * 
         * <p>For example: 
         * <p><code>&lta href="https://example.com"> &ltscript src="https://google.com/js.js" rel="external"></code> 
         * <p>would become: 
         * <p><code>&lta href="//example.com"> &ltscript src="https://google.com/js.js" rel="external"></code>
         * 
         * <p>Default is <code>false</code>.
         * 
         * @param removeHttpsProtocol set <code>true</code> to remove <code>HTTP</code> protocol from tag attributes
         */
        public bool RemoveHttpsProtocol = false;

        public bool PreserveLineBreaks = false;

        /**
         * Enables surrounding spaces removal around provided comma separated list of tags.
         * 
         * <p>Besides custom defined lists, you can pass one of 3 predefined lists of tags: 
         * {@link #BLOCK_TAGS_MIN BLOCK_TAGS_MIN},
         * {@link #BLOCK_TAGS_MAX BLOCK_TAGS_MAX},
         * {@link #ALL_TAGS ALL_TAGS}.
         * 
         * @param tagList a comma separated list of tags around which spaces will be removed
         */
        public string RemoveSurroundingSpaces = null;

        /**
         * This method allows setting custom block preservation rules defined by regular 
         * expression patterns. Blocks that match provided patterns will be skipped during HTML compression. 
         * 
         * <p>Custom preservation rules have higher priority than default rules.
         * Priority between custom rules are defined by their position in a list 
         * (beginning of a list has higher priority).
         * 
         * <p>Besides custom patterns, you can use 3 predefined patterns: 
         * {@link #PHP_TAG_PATTERN PHP_TAG_PATTERN},
         * {@link #SERVER_SCRIPT_TAG_PATTERN SERVER_SCRIPT_TAG_PATTERN},
         * {@link #SERVER_SIDE_INCLUDE_PATTERN SERVER_SIDE_INCLUDE_PATTERN}.
         * 
         * @param preservePatterns List of <code>Regex</code> objects that will be 
         * used to skip matched blocks during compression  
         */
        public List<Regex> PreservePatterns = null;

        //statistics
        /**
         * If set to <code>true</code>, HTML compression statistics will be generated. 
         * 
         * <p><strong>Important:</strong> Enabling statistics makes HTML compressor not thread safe. 
         * 
         * <p>Default is <code>false</code>.
         * 
         * @param generateStatistics set <code>true</code> to generate HTML compression statistics 
         */
        public bool GenerateStatistics = false;

        /**
         * Returns {@link HtmlCompressorStatistics} object containing statistics of the last HTML compression, if enabled. 
         * Should be called after {@link #compress(string)}
         * 
         * @return {@link HtmlCompressorStatistics} object containing last HTML compression statistics
         * 
         * @see HtmlCompressorStatistics
         * @see #setGenerateStatistics(bool)
         */
        public HtmlCompressorStatistics Statistics = null;

        //temp replacements for preserved blocks 
        private static readonly string tempCondCommentBlock = "%%%~COMPRESS~COND~{0}~%%%";
        private static readonly string tempPreBlock = "%%%~COMPRESS~PRE~{0}~%%%";
        private static readonly string tempTextAreaBlock = "%%%~COMPRESS~TEXTAREA~{0}~%%%";
        private static readonly string tempScriptBlock = "%%%~COMPRESS~SCRIPT~{0}~%%%";
        private static readonly string tempStyleBlock = "%%%~COMPRESS~STYLE~{0}~%%%";
        private static readonly string tempEventBlock = "%%%~COMPRESS~EVENT~{0}~%%%";
        private static readonly string tempLineBreakBlock = "%%%~COMPRESS~LT~{0}~%%%";
        private static readonly string tempSkipBlock = "%%%~COMPRESS~SKIP~{0}~%%%";
        private static readonly string tempUserBlock = "%%%~COMPRESS~USER{0}~{1}~%%%";

        //compiled regex patterns
        private static readonly Regex emptyPattern = new Regex("\\s");

        private static readonly Regex skipPattern = new Regex("<!--\\s*\\{\\{\\{\\s*-->(.*?)<!--\\s*\\}\\}\\}\\s*-->",
                                                              RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex condCommentPattern = new Regex("(<!(?:--)?\\[[^\\]]+?]>)(.*?)(<!\\[[^\\]]+]-->)",
                                                                     RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex commentPattern = new Regex("<!---->|<!--[^\\[].*?-->",
                                                                 RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex intertagPattern_TagTag = new Regex(">\\s+<",
                                                                         RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex intertagPattern_TagCustom = new Regex(">\\s+%%%~",
                                                                            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex intertagPattern_CustomTag = new Regex("~%%%\\s+<",
                                                                            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex intertagPattern_CustomCustom = new Regex("~%%%\\s+%%%~",
                                                                               RegexOptions.Singleline |
                                                                               RegexOptions.IgnoreCase);

        private static readonly Regex multispacePattern = new Regex("\\s+", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex tagEndSpacePattern = new Regex("(<(?:[^>]+?))(?:\\s+?)(/?>)",
                                                                     RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex tagLastUnquotedValuePattern = new Regex("=\\s*[a-z0-9-_]+$", RegexOptions.IgnoreCase);

        private static readonly Regex tagQuotePattern = new Regex("\\s*=\\s*([\"'])([a-z0-9-_]+?)\\1(/?)(?=[^<]*?>)",
                                                                  RegexOptions.IgnoreCase);

        private static readonly Regex prePattern = new Regex("(<pre[^>]*?>)(.*?)(</pre>)",
                                                             RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex taPattern = new Regex("(<textarea[^>]*?>)(.*?)(</textarea>)",
                                                            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex scriptPattern = new Regex("(<script[^>]*?>)(.*?)(</script>)",
                                                                RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex stylePattern = new Regex("(<style[^>]*?>)(.*?)(</style>)",
                                                               RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex tagPropertyPattern = new Regex("(\\s\\w+)\\s*=\\s*(?=[^<]*?>)", RegexOptions.IgnoreCase);

        private static readonly Regex cdataPattern = new Regex("\\s*<!\\[CDATA\\[(.*?)\\]\\]>\\s*",
                                                               RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex doctypePattern = new Regex("<!DOCTYPE[^>]*>",
                                                                 RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex typeAttrPattern = new Regex("type\\s*=\\s*([\\\"']*)(.+?)\\1",
                                                                  RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex jsTypeAttrPattern =
            new Regex("(<script[^>]*)type\\s*=\\s*([\"']*)(?:text|application)/javascript\\2([^>]*>)",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex jsLangAttrPattern =
            new Regex("(<script[^>]*)language\\s*=\\s*([\"']*)javascript\\2([^>]*>)",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex styleTypeAttrPattern =
            new Regex("(<style[^>]*)type\\s*=\\s*([\"']*)text/style\\2([^>]*>)",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex linkTypeAttrPattern =
            new Regex("(<link[^>]*)type\\s*=\\s*([\"']*)text/(?:css|plain)\\2([^>]*>)",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex linkRelAttrPattern =
            new Regex("<link(?:[^>]*)rel\\s*=\\s*([\"']*)(?:alternate\\s+)?stylesheet\\1(?:[^>]*)>",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex formMethodAttrPattern = new Regex("(<form[^>]*)method\\s*=\\s*([\"']*)get\\2([^>]*>)",
                                                                        RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex inputTypeAttrPattern = new Regex("(<input[^>]*)type\\s*=\\s*([\"']*)text\\2([^>]*>)",
                                                                       RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex booleanAttrPattern =
            new Regex("(<\\w+[^>]*)(checked|selected|disabled|readonly)\\s*=\\s*([\"']*)\\w*\\3([^>]*>)",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex eventJsProtocolPattern = new Regex("^javascript:\\s*(.+)",
                                                                         RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex httpProtocolPattern =
            new Regex("(<[^>]+?(?:href|src|cite|action)\\s*=\\s*['\"])http:(//[^>]+?>)",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex httpsProtocolPattern =
            new Regex("(<[^>]+?(?:href|src|cite|action)\\s*=\\s*['\"])https:(//[^>]+?>)",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex relExternalPattern =
            new Regex("<(?:[^>]*)rel\\s*=\\s*([\"']*)(?:alternate\\s+)?external\\1(?:[^>]*)>",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex eventPattern1 =
            new Regex("(\\son[a-z]+\\s*=\\s*\")([^\"\\\\\\r\\n]*(?:\\\\.[^\"\\\\\\r\\n]*)*)(\")", RegexOptions.IgnoreCase);
        //unmasked: \son[a-z]+\s*=\s*"[^"\\\r\n]*(?:\\.[^"\\\r\n]*)*"

        private static readonly Regex eventPattern2 =
            new Regex("(\\son[a-z]+\\s*=\\s*')([^'\\\\\\r\\n]*(?:\\\\.[^'\\\\\\r\\n]*)*)(')", RegexOptions.IgnoreCase);

        private static readonly Regex lineBreakPattern = new Regex("(?:[ \t]*(\\r?\\n)[ \t]*)+");

        private static readonly Regex surroundingSpacesMinPattern =
            new Regex("\\s*(</?(?:" + BLOCK_TAGS_MIN.Replace(",", "|") + ")(?:>|[\\s/][^>]*>))\\s*",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex surroundingSpacesMaxPattern =
            new Regex("\\s*(</?(?:" + BLOCK_TAGS_MAX.Replace(",", "|") + ")(?:>|[\\s/][^>]*>))\\s*",
                      RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Regex surroundingSpacesAllPattern = new Regex("\\s*(<[^>]+>)\\s*",
                                                                              RegexOptions.Singleline |
                                                                              RegexOptions.IgnoreCase);

        //patterns for searching for temporary replacements
        private static readonly Regex tempCondCommentPattern = new Regex("%%%~COMPRESS~COND~(\\d+?)~%%%");
        private static readonly Regex tempPrePattern = new Regex("%%%~COMPRESS~PRE~(\\d+?)~%%%");
        private static readonly Regex tempTextAreaPattern = new Regex("%%%~COMPRESS~TEXTAREA~(\\d+?)~%%%");
        private static readonly Regex tempScriptPattern = new Regex("%%%~COMPRESS~SCRIPT~(\\d+?)~%%%");
        private static readonly Regex tempStylePattern = new Regex("%%%~COMPRESS~STYLE~(\\d+?)~%%%");
        private static readonly Regex tempEventPattern = new Regex("%%%~COMPRESS~EVENT~(\\d+?)~%%%");
        private static readonly Regex tempSkipPattern = new Regex("%%%~COMPRESS~SKIP~(\\d+?)~%%%");
        private static readonly Regex tempLineBreakPattern = new Regex("%%%~COMPRESS~LT~(\\d+?)~%%%");

        /**
         * The main method that compresses given HTML source and returns compressed
         * result.
         * 
         * @param html HTML content to compress
         * @return compressed content.
         */

        public string Compress(string html)
        {
            if (!Enabled || string.IsNullOrEmpty(html))
            {
                return html;
            }

            //calculate uncompressed statistics
            initStatistics(html);

            //preserved block containers
            List<string> condCommentBlocks = new List<string>();
            List<string> preBlocks = new List<string>();
            List<string> taBlocks = new List<string>();
            List<string> scriptBlocks = new List<string>();
            List<string> styleBlocks = new List<string>();
            List<string> eventBlocks = new List<string>();
            List<string> skipBlocks = new List<string>();
            List<string> lineBreakBlocks = new List<string>();
            List<List<string>> userBlocks = new List<List<string>>();

            //preserve blocks
            html = preserveBlocks(html, preBlocks, taBlocks, scriptBlocks, styleBlocks, eventBlocks, condCommentBlocks,
                                  skipBlocks, lineBreakBlocks, userBlocks);

            //process pure html
            html = processHtml(html);

            //process preserved blocks
            processPreservedBlocks(preBlocks, taBlocks, scriptBlocks, styleBlocks, eventBlocks, condCommentBlocks, skipBlocks,
                                   lineBreakBlocks, userBlocks);

            //put preserved blocks back
            html = returnBlocks(html, preBlocks, taBlocks, scriptBlocks, styleBlocks, eventBlocks, condCommentBlocks, skipBlocks,
                                lineBreakBlocks, userBlocks);

            //calculate compressed statistics
            endStatistics(html);

            return html;
        }

        private void initStatistics(string html)
        {
            //create stats
            if (GenerateStatistics)
            {
                Statistics = new HtmlCompressorStatistics {
                    Time = DateTime.Now.Ticks,
                    OriginalMetrics = {Filesize = html.Length}
                };

                //calculate number of empty chars
                var matcher = emptyPattern.Matches(html);
                foreach (Match match in matcher)
                {
                    Statistics.OriginalMetrics.EmptyChars = Statistics.OriginalMetrics.EmptyChars + 1;
                }
            }
            else
            {
                Statistics = null;
            }
        }

        private void endStatistics(string html)
        {
            //calculate compression time
            if (GenerateStatistics)
            {
                Statistics.Time = DateTime.Now.Ticks - Statistics.Time;
                Statistics.CompressedMetrics.Filesize = html.Length;

                //calculate number of empty chars
                var matcher = emptyPattern.Matches(html);
                foreach (Match match in matcher)
                {
                    Statistics.CompressedMetrics.EmptyChars = Statistics.CompressedMetrics.EmptyChars + 1;
                }
            }
        }

        private string preserveBlocks(
            string html,
            List<string> preBlocks,
            List<string> taBlocks,
            List<string> scriptBlocks,
            List<string> styleBlocks,
            List<string> eventBlocks,
            List<string> condCommentBlocks,
            List<string> skipBlocks, List<string> lineBreakBlocks,
            List<List<string>> userBlocks)
        {
            //preserve user blocks
            if (PreservePatterns != null)
            {
                for (var p = 0; p < PreservePatterns.Count; p++)
                {
                    var userBlock = new List<string>();

                    var matches = PreservePatterns[p].Matches(html);
                    var index = 0;
                    var sb = new StringBuilder();
                    var lastValue = 0;

                    foreach (Match match in matches)
                    {
                        if (match.Groups[0].Value.Trim().Length > 0)
                        {
                            userBlock.Add(match.Groups[0].Value);

                            sb.Append(html.Substring(lastValue, match.Index - lastValue));
                            //matches.appendReplacement(sb1, string.Format(tempUserBlock, p, index++));
                            sb.Append(match.Result(string.Format(tempUserBlock, p, index++)));

                            lastValue = match.Index + match.Length;
                        }
                    }

                    //matches.appendTail(sb1);
                    sb.Append(html.Substring(lastValue));

                    html = sb.ToString();
                    userBlocks.Add(userBlock);
                }
            }

            var skipBlockIndex = 0;

            //preserve <!-- {{{ ---><!-- }}} ---> skip blocks
            if (true)
            {
                var matcher = skipPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    if (match.Groups[1].Value.Trim().Length > 0)
                    {
                        skipBlocks.Add(match.Groups[1].Value);

                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, string.Format(tempSkipBlock, skipBlockIndex++));
                        sb.Append(match.Result(string.Format(tempSkipBlock, skipBlockIndex++)));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //preserve conditional comments
            if (true)
            {
                var condCommentCompressor = createCompressorClone();
                var matcher = condCommentPattern.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    if (match.Groups[2].Value.Trim().Length > 0)
                    {
                        condCommentBlocks.Add(
                            match.Groups[1].Value + condCommentCompressor.Compress(match.Groups[2].Value) + match.Groups[3].Value);

                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, string.Format(tempCondCommentBlock, index++));
                        sb.Append(match.Result(string.Format(tempCondCommentBlock, index++)));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //preserve inline events
            if (true)
            {
                var matcher = eventPattern1.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    if (match.Groups[2].Value.Trim().Length > 0)
                    {
                        eventBlocks.Add(match.Groups[2].Value);

                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1" + string.Format(tempEventBlock, index++) + "$3");
                        sb.Append(match.Result("$1" + string.Format(tempEventBlock, index++) + "$3"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            if (true)
            {
                var matcher = eventPattern2.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    if (match.Groups[2].Value.Trim().Length > 0)
                    {
                        eventBlocks.Add(match.Groups[2].Value);

                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1" + string.Format(tempEventBlock, index++) + "$3");
                        sb.Append(match.Result("$1" + string.Format(tempEventBlock, index++) + "$3"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //preserve PRE tags
            if (true)
            {
                var matcher = prePattern.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    if (match.Groups[2].Value.Trim().Length > 0)
                    {
                        preBlocks.Add(match.Groups[2].Value);

                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1" + string.Format(tempPreBlock, index++) + "$3");
                        sb.Append(match.Result("$1" + string.Format(tempPreBlock, index++) + "$3"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //preserve SCRIPT tags
            if (true)
            {
                var matcher = scriptPattern.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    //ignore empty scripts
                    if (match.Groups[2].Value.Trim().Length > 0)
                    {

                        //check type
                        string type = "";
                        var typeMatcher = typeAttrPattern.Match(match.Groups[1].Value);
                        if (typeMatcher.Success)
                        {
                            type = typeMatcher.Groups[2].Value.ToLowerInvariant();
                        }

                        if (type.Length == 0 || type.Equals("text/javascript") || type.Equals("application/javascript"))
                        {
                            //javascript block, preserve and compress with js compressor
                            scriptBlocks.Add(match.Groups[2].Value);

                            sb.Append(html.Substring(lastValue, match.Index - lastValue));
                            //matcher.appendReplacement(sb, "$1" + string.Format(tempScriptBlock, index++) + "$3");
                            sb.Append(match.Result("$1" + string.Format(tempScriptBlock, index++) + "$3"));

                            lastValue = match.Index + match.Length;
                        }
                        else if (type.Equals("text/x-jquery-tmpl"))
                        {
                            //jquery template, ignore so it gets compressed with the rest of html
                        }
                        else
                        {
                            //some custom script, preserve it inside "skip blocks" so it won't be compressed with js compressor 
                            skipBlocks.Add(match.Groups[2].Value);

                            sb.Append(html.Substring(lastValue, match.Index - lastValue));
                            //matcher.appendReplacement(sb, "$1" + string.Format(tempSkipBlock, skipBlockIndex++) + "$3");
                            sb.Append(match.Result("$1" + string.Format(tempSkipBlock, skipBlockIndex++) + "$3"));

                            lastValue = match.Index + match.Length;
                        }
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //preserve STYLE tags
            if (true)
            {
                var matcher = stylePattern.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    if (match.Groups[2].Value.Trim().Length > 0)
                    {
                        styleBlocks.Add(match.Groups[2].Value);

                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1" + string.Format(tempStyleBlock, index++) + "$3");
                        sb.Append(match.Result("$1" + string.Format(tempStyleBlock, index++) + "$3"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //preserve TEXTAREA tags
            if (true)
            {
                var matcher = taPattern.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    if (match.Groups[2].Value.Trim().Length > 0)
                    {
                        taBlocks.Add(match.Groups[2].Value);

                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1" + string.Format(tempTextAreaBlock, index++) + "$3");
                        sb.Append(match.Result("$1" + string.Format(tempTextAreaBlock, index++) + "$3"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //preserve line breaks
            if (PreserveLineBreaks)
            {
                var matcher = lineBreakPattern.Matches(html);
                var index = 0;
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    lineBreakBlocks.Add(match.Groups[1].Value);

                    sb.Append(html.Substring(lastValue, match.Index - lastValue));
                    //matcher.appendReplacement(sb, string.Format(tempLineBreakBlock, index++));
                    sb.Append(match.Result(string.Format(tempLineBreakBlock, index++)));

                    lastValue = match.Index + match.Length;
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            return html;
        }

        private string returnBlocks(
            string html,
            List<string> preBlocks,
            List<string> taBlocks,
            List<string> scriptBlocks,
            List<string> styleBlocks,
            List<string> eventBlocks,
            List<string> condCommentBlocks,
            List<string> skipBlocks,
            List<string> lineBreakBlocks,
            List<List<string>> userBlocks)
        {
            //put line breaks back
            if (PreserveLineBreaks)
            {
                var matcher = tempLineBreakPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    var i = int.Parse(match.Groups[1].Value);
                    if (lineBreakBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, lineBreakBlocks[i]);
                        sb.Append(match.Result(lineBreakBlocks[i]));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put TEXTAREA blocks back
            if (true)
            {
                var matcher = tempTextAreaPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    int i = int.Parse(match.Groups[1].Value);
                    if (taBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, Regex.Escape(taBlocks[i]));
                        sb.Append(match.Result(/*Regex.Escape*/(taBlocks[i])));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put STYLE blocks back
            if (true)
            {
                var matcher = tempStylePattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    int i = int.Parse(match.Groups[1].Value);
                    if (styleBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, Regex.Escape(styleBlocks[i]));
                        sb.Append(match.Result(/*Regex.Escape*/(styleBlocks[i])));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put SCRIPT blocks back
            if (true)
            {
                var matcher = tempScriptPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    int i = int.Parse(match.Groups[1].Value);
                    if (scriptBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, Regex.Escape(scriptBlocks[i]));
                        sb.Append(match.Result(/*Regex.Escape*/(scriptBlocks[i])));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put PRE blocks back
            if (true)
            {
                var matcher = tempPrePattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    int i = int.Parse(match.Groups[1].Value);
                    if (preBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, Regex.Escape(preBlocks[i]));
                        sb.Append(match.Result(/*Regex.Escape*/(preBlocks[i])));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put event blocks back
            if (true)
            {
                var matcher = tempEventPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    int i = int.Parse(match.Groups[1].Value);
                    if (eventBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, Regex.Escape(eventBlocks[i]));
                        sb.Append(match.Result(/*Regex.Escape*/(eventBlocks[i])));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put conditional comments back
            if (true)
            {
                var matcher = tempCondCommentPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    int i = int.Parse(match.Groups[1].Value);
                    if (condCommentBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, Regex.Escape(condCommentBlocks[i]));
                        sb.Append(match.Result(/*Regex.Escape*/(condCommentBlocks[i])));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put skip blocks back
            if (true)
            {
                var matcher = tempSkipPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    int i = int.Parse(match.Groups[1].Value);
                    if (skipBlocks.Count > i)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, Regex.Escape(skipBlocks[i]));
                        sb.Append(match.Result(/*Regex.Escape*/(skipBlocks[i])));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }

            //put user blocks back
            if (PreservePatterns != null)
            {
                for (int p = PreservePatterns.Count - 1; p >= 0; p--)
                {
                    Regex tempUserPattern = new Regex("%%%~COMPRESS~USER" + p + "~(\\d+?)~%%%");
                    var matcher = tempUserPattern.Matches(html);
                    var sb = new StringBuilder();
                    var lastValue = 0;

                    foreach (Match match in matcher)
                    {
                        int i = int.Parse(match.Groups[1].Value);
                        if (userBlocks.Count > p && userBlocks[p].Count > i)
                        {
                            sb.Append(html.Substring(lastValue, match.Index - lastValue));
                            //matcher.appendReplacement(sb, Regex.Escape(userBlocks[p][i]));
                            sb.Append(match.Result(/*Regex.Escape*/(userBlocks[p][i])));

                            lastValue = match.Index + match.Length;
                        }
                    }

                    //matcher.appendTail(sb);
                    sb.Append(html.Substring(lastValue));

                    html = sb.ToString();
                }
            }

            return html;
        }

        private string processHtml(string html)
        {

            //remove comments
            html = removeComments(html);

            //simplify doctype
            html = simpleDoctype(html);

            //remove script attributes
            html = removeScriptAttributes(html);

            //remove style attributes
            html = removeStyleAttributes(html);

            //remove link attributes
            html = removeLinkAttributes(html);

            //remove form attributes
            html = removeFormAttributes(html);

            //remove input attributes
            html = removeInputAttributes(html);

            //simplify bool attributes
            html = simpleBooleanAttributes(html);

            //remove http from attributes
            html = removeHttpProtocol(html);

            //remove https from attributes
            html = removeHttpsProtocol(html);

            //remove inter-tag spaces
            html = removeIntertagSpaces(html);

            //remove multi whitespace characters
            html = removeMultiSpaces(html);

            //remove spaces around equals sign and ending spaces
            html = removeSpacesInsideTags(html);

            //remove quotes from tag attributes
            html = removeQuotesInsideTags(html);

            //remove surrounding spaces
            html = removeSurroundingSpaces(html);

            return html.Trim();
        }

        private string removeSurroundingSpaces(string html)
        {
            //remove spaces around provided tags
            if (RemoveSurroundingSpaces != null)
            {
                Regex pattern;
                if (string.Compare(RemoveSurroundingSpaces, BLOCK_TAGS_MIN, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    pattern = surroundingSpacesMinPattern;
                }
                else if (string.Compare(RemoveSurroundingSpaces, BLOCK_TAGS_MAX, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    pattern = surroundingSpacesMaxPattern;
                }
                if (string.Compare(RemoveSurroundingSpaces, ALL_TAGS, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    pattern = surroundingSpacesAllPattern;
                }
                else
                {
                    pattern = new Regex(string.Format("\\s*(</?(?:{0})(?:>|[\\s/][^>]*>))\\s*",
                                                      RemoveSurroundingSpaces.Replace(",", "|")),
                                        RegexOptions.Singleline | RegexOptions.IgnoreCase);
                }

                var matcher = pattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    sb.Append(html.Substring(lastValue, match.Index - lastValue));
                    //matcher.appendReplacement(sb, "$1");
                    sb.Append(match.Result("$1"));

                    lastValue = match.Index + match.Length;
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();

            }
            return html;
        }

        private string removeQuotesInsideTags(string html)
        {
            //remove quotes from tag attributes
            if (RemoveQuotes)
            {
                var matcher = tagQuotePattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    //if quoted attribute is followed by "/" add extra space
                    if (match.Groups[3].Value.Trim().Length == 0)
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "=$2");
                        sb.Append(match.Result("=$2"));

                        lastValue = match.Index + match.Length;
                    }
                    else
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "=$2 $3");
                        sb.Append(match.Result("=$2 $3"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();

            }
            return html;
        }

        private string removeSpacesInsideTags(string html)
        {
            //remove spaces around equals sign inside tags
            html = tagPropertyPattern.Replace(html, "$1=");

            //remove ending spaces inside tags
            //html = tagEndSpacePattern.Matches(html).Replace("$1$2");
            var matcher = tagEndSpacePattern.Matches(html);
            var sb = new StringBuilder();
            var lastValue = 0;

            foreach (Match match in matcher)
            {
                //keep space if attribute value is unquoted before trailing slash
                if (match.Groups[2].Value.StartsWith("/") && tagLastUnquotedValuePattern.IsMatch(match.Groups[1].Value))
                {
                    sb.Append(html.Substring(lastValue, match.Index - lastValue));
                    //matcher.appendReplacement(sb, "$1 $2");
                    sb.Append(match.Result("$1 $2"));

                    lastValue = match.Index + match.Length;
                }
                else
                {
                    sb.Append(html.Substring(lastValue, match.Index - lastValue));
                    //matcher.appendReplacement(sb, "$1$2");
                    sb.Append(match.Result("$1$2"));

                    lastValue = match.Index + match.Length;
                }
            }

            //matcher.appendTail(sb);
            sb.Append(html.Substring(lastValue));

            html = sb.ToString();

            return html;
        }

        private string removeMultiSpaces(string html)
        {
            //collapse multiple spaces
            if (RemoveMultiSpaces)
            {
                html = multispacePattern.Replace(html, " ");
            }
            return html;
        }

        private string removeIntertagSpaces(string html)
        {
            //remove inter-tag spaces
            if (RemoveIntertagSpaces)
            {
                html = intertagPattern_TagTag.Replace(html, "><");
                html = intertagPattern_TagCustom.Replace(html, ">%%%~");
                html = intertagPattern_CustomTag.Replace(html, "~%%%<");
                html = intertagPattern_CustomCustom.Replace(html, "~%%%%%%~");
            }
            return html;
        }

        private string removeComments(string html)
        {
            //remove comments
            if (RemoveComments)
            {
                html = commentPattern.Replace(html, "");
            }
            return html;
        }

        private string simpleDoctype(string html)
        {
            //simplify doctype
            if (SimpleDoctype)
            {
                html = doctypePattern.Replace(html, "<!DOCTYPE html>");
            }
            return html;
        }

        private string removeScriptAttributes(string html)
        {

            if (RemoveScriptAttributes)
            {
                //remove type from script tags
                html = jsTypeAttrPattern.Replace(html, "$1$3");

                //remove language from script tags
                html = jsLangAttrPattern.Replace(html, "$1$3");
            }
            return html;
        }

        private string removeStyleAttributes(string html)
        {
            //remove type from style tags
            if (RemoveStyleAttributes)
            {
                html = styleTypeAttrPattern.Replace(html, "$1$3");
            }
            return html;
        }

        private string removeLinkAttributes(string html)
        {
            //remove type from link tags with rel=stylesheet
            if (RemoveLinkAttributes)
            {
                var matcher = linkTypeAttrPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    //if rel=stylesheet
                    if (matches(linkRelAttrPattern, match.Groups[0].Value))
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1$3");
                        sb.Append(match.Result("$1$3"));

                        lastValue = match.Index + match.Length;
                    }
                    else
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$0");
                        sb.Append(match.Result("$0"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }
            return html;
        }

        private string removeFormAttributes(string html)
        {
            //remove method from form tags
            if (RemoveFormAttributes)
            {
                html = formMethodAttrPattern.Replace(html, "$1$3");
            }
            return html;
        }

        private string removeInputAttributes(string html)
        {
            //remove type from input tags
            if (RemoveInputAttributes)
            {
                html = inputTypeAttrPattern.Replace(html, "$1$3");
            }
            return html;
        }

        private string simpleBooleanAttributes(string html)
        {
            //simplify bool attributes
            if (SimpleBooleanAttributes)
            {
                html = booleanAttrPattern.Replace(html, "$1$2$4");
            }
            return html;
        }

        private string removeHttpProtocol(string html)
        {
            //remove http protocol from tag attributes
            if (RemoveHttpProtocol)
            {
                var matcher = httpProtocolPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    //if rel!=external
                    if (!matches(relExternalPattern, match.Groups[0].Value))
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1$2");
                        sb.Append(match.Result("$1$2"));

                        lastValue = match.Index + match.Length;
                    }
                    else
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$0");
                        sb.Append(match.Result("$0"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }
            return html;
        }

        private string removeHttpsProtocol(string html)
        {
            //remove https protocol from tag attributes
            if (RemoveHttpsProtocol)
            {
                var matcher = httpsProtocolPattern.Matches(html);
                var sb = new StringBuilder();
                var lastValue = 0;

                foreach (Match match in matcher)
                {
                    //if rel!=external
                    if (!matches(relExternalPattern, match.Groups[0].Value))
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$1$2");
                        sb.Append(match.Result("$1$2"));

                        lastValue = match.Index + match.Length;
                    }
                    else
                    {
                        sb.Append(html.Substring(lastValue, match.Index - lastValue));
                        //matcher.appendReplacement(sb, "$0");
                        sb.Append(match.Result("$0"));

                        lastValue = match.Index + match.Length;
                    }
                }

                //matcher.appendTail(sb);
                sb.Append(html.Substring(lastValue));

                html = sb.ToString();
            }
            return html;
        }

        private static bool matches(Regex regex, string value)
        {
            // http://stackoverflow.com/questions/4450045/difference-between-matches-and-find-in-java-regex

            var cloneRegex = new Regex(@"^" + regex + @"$", regex.Options);
            return cloneRegex.IsMatch(value);
        }

        private void processPreservedBlocks(List<string> preBlocks, List<string> taBlocks, List<string> scriptBlocks,
                                            List<string> styleBlocks, List<string> eventBlocks, List<string> condCommentBlocks,
                                            List<string> skipBlocks, List<string> lineBreakBlocks,
                                            List<List<string>> userBlocks)
        {
            processPreBlocks(preBlocks);
            processTextAreaBlocks(taBlocks);
            processScriptBlocks(scriptBlocks);
            processStyleBlocks(styleBlocks);
            processEventBlocks(eventBlocks);
            processCondCommentBlocks(condCommentBlocks);
            processSkipBlocks(skipBlocks);
            processUserBlocks(userBlocks);
            processLineBreakBlocks(lineBreakBlocks);
        }

        private void processPreBlocks(List<string> preBlocks)
        {
            if (GenerateStatistics)
            {
                foreach (string block in preBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }
        }

        private void processTextAreaBlocks(List<string> taBlocks)
        {
            if (GenerateStatistics)
            {
                foreach (string block in taBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }
        }

        private void processCondCommentBlocks(List<string> condCommentBlocks)
        {
            if (GenerateStatistics)
            {
                foreach (string block in condCommentBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }
        }

        private void processSkipBlocks(List<string> skipBlocks)
        {
            if (GenerateStatistics)
            {
                foreach (string block in skipBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }
        }

        private void processLineBreakBlocks(List<string> lineBreakBlocks)
        {
            if (GenerateStatistics)
            {
                foreach (string block in lineBreakBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }
        }

        private void processUserBlocks(List<List<string>> userBlocks)
        {
            if (GenerateStatistics)
            {
                foreach (List<string> blockList in userBlocks)
                {
                    foreach (string block in blockList)
                    {
                        Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                    }
                }
            }
        }

        private void processEventBlocks(List<string> eventBlocks)
        {

            if (GenerateStatistics)
            {
                foreach (string block in eventBlocks)
                {
                    Statistics.OriginalMetrics.InlineEventSize = Statistics.OriginalMetrics.InlineEventSize + block.Length;
                }
            }

            if (RemoveJavaScriptProtocol)
            {
                for (int i = 0; i < eventBlocks.Count; i++)
                {
                    eventBlocks[i] = removeJavaScriptProtocol(eventBlocks[i]);
                }
            }
            else if (GenerateStatistics)
            {
                foreach (string block in eventBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }

            if (GenerateStatistics)
            {
                foreach (string block in eventBlocks)
                {
                    Statistics.CompressedMetrics.InlineEventSize = Statistics.CompressedMetrics.InlineEventSize + block.Length;
                }
            }
        }

        private string removeJavaScriptProtocol(string source)
        {
            //remove javascript: from inline events
            string result = source;

            result = eventJsProtocolPattern.Replace(source, @"$1", 1);
            //var matcher = eventJsProtocolPattern.Match(source);
            //if (matcher.Success)
            //{
            //    result = matcher.replaceFirst("$1");
            //}

            if (GenerateStatistics)
            {
                Statistics.PreservedSize = Statistics.PreservedSize + result.Length;
            }

            return result;
        }

        private void processScriptBlocks(List<string> scriptBlocks)
        {
            if (GenerateStatistics)
            {
                foreach (string block in scriptBlocks)
                {
                    Statistics.OriginalMetrics.InlineScriptSize = Statistics.OriginalMetrics.InlineScriptSize + block.Length;
                }
            }

            if (CompressJavaScript)
            {
                for (int i = 0; i < scriptBlocks.Count; i++)
                {
                    scriptBlocks[i] = compressJavaScript(scriptBlocks[i]);
                }
            }
            else if (GenerateStatistics)
            {
                foreach (string block in scriptBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }

            if (GenerateStatistics)
            {
                foreach (string block in scriptBlocks)
                {
                    Statistics.CompressedMetrics.InlineScriptSize = Statistics.CompressedMetrics.InlineScriptSize + block.Length;
                }
            }
        }

        private void processStyleBlocks(List<string> styleBlocks)
        {

            if (GenerateStatistics)
            {
                foreach (string block in styleBlocks)
                {
                    Statistics.OriginalMetrics.InlineStyleSize = Statistics.OriginalMetrics.InlineStyleSize + block.Length;
                }
            }

            if (CompressCss)
            {
                for (int i = 0; i < styleBlocks.Count; i++)
                {
                    styleBlocks[i] = compressCssStyles(styleBlocks[i]);
                }
            }
            else if (GenerateStatistics)
            {
                foreach (string block in styleBlocks)
                {
                    Statistics.PreservedSize = Statistics.PreservedSize + block.Length;
                }
            }

            if (GenerateStatistics)
            {
                foreach (string block in styleBlocks)
                {
                    Statistics.CompressedMetrics.InlineStyleSize = Statistics.CompressedMetrics.InlineStyleSize + block.Length;
                }
            }
        }

        private string compressJavaScript(string source)
        {
            //set default javascript compressor
            if (JavaScriptCompressor == null)
            {
                return source;
            }

            //detect CDATA wrapper
            bool cdataWrapper = false;
            var matcher = cdataPattern.Match(source);
            if (matcher.Success)
            {
                cdataWrapper = true;
                source = matcher.Groups[1].Value;
            }

            string result = JavaScriptCompressor.Compress(source);

            if (cdataWrapper)
            {
                result = string.Format("<![CDATA[{0}]]>", result);
            }

            return result;

        }

        private string compressCssStyles(string source)
        {
            //set default css compressor
            if (CssCompressor == null)
            {
                return source;
            }

            //detect CDATA wrapper
            bool cdataWrapper = false;
            var matcher = cdataPattern.Match(source);
            if (matcher.Success)
            {
                cdataWrapper = true;
                source = matcher.Groups[1].Value;
            }

            string result = CssCompressor.Compress(source);

            if (cdataWrapper)
            {
                result = string.Format("<![CDATA[{0}]]>", result);
            }

            return result;

        }

        private HtmlCompressor createCompressorClone()
        {
            var clone = new HtmlCompressor
            {
                JavaScriptCompressor = JavaScriptCompressor,
                CssCompressor = CssCompressor,
                CompressJavaScript = CompressJavaScript,
                CompressCss = CompressCss,
                RemoveQuotes = RemoveQuotes,
                RemoveComments = RemoveComments,
                RemoveMultiSpaces = RemoveMultiSpaces,
                RemoveIntertagSpaces = RemoveIntertagSpaces,
                PreservePatterns = PreservePatterns,
                SimpleDoctype = SimpleDoctype,
                RemoveScriptAttributes = RemoveScriptAttributes,
                RemoveStyleAttributes = RemoveStyleAttributes,
                RemoveLinkAttributes = RemoveLinkAttributes,
                RemoveFormAttributes = RemoveFormAttributes,
                RemoveInputAttributes = RemoveInputAttributes,
                SimpleBooleanAttributes = SimpleBooleanAttributes,
                RemoveJavaScriptProtocol = RemoveJavaScriptProtocol,
                RemoveHttpProtocol = RemoveHttpProtocol,
                RemoveHttpsProtocol = RemoveHttpsProtocol,
                PreserveLineBreaks = PreserveLineBreaks,
                RemoveSurroundingSpaces = RemoveSurroundingSpaces,
            };

            return clone;
        }
    }

    public sealed class HtmlCompressorStatistics
    {
        /**
         * Returns metrics of an uncompressed document
         * 
         * @return metrics of an uncompressed document
         * @see HtmlMetrics
         */
        public HtmlMetrics OriginalMetrics = new HtmlMetrics();

        /**
         * Returns metrics of a compressed document
         * 
         * @return metrics of a compressed document
         * @see HtmlMetrics
         */
        public HtmlMetrics CompressedMetrics = new HtmlMetrics();

        /**
         * Returns total compression time. 
         * 
         * <p>Please note that compression performance varies very significantly depending on whether it was 
         * a cold run or not (specifics of Java VM), so for accurate real world results it is recommended 
         * to take measurements accordingly.   
         * 
         * @return the compression time, in milliseconds 
         *      
         */
        public long Time = 0;

        /**
         * Returns total size of blocks that were skipped by the compressor 
         * (for example content inside <code>&lt;pre></code> tags or inside   
         * <code>&lt;script></code> tags with disabled javascript compression)
         * 
         * @return the total size of blocks that were skipped by the compressor, in bytes
         */
        public int PreservedSize = 0;

        public override string ToString()
        {
            return String.Format("Time={0}, Preserved={1}, Original={2}, Compressed={3}", 
                Time, PreservedSize, OriginalMetrics, CompressedMetrics);
        }
    }

    public sealed class HtmlMetrics
    {
        /**
         * Returns total filesize of a document
         * 
         * @return total filesize of a document, in bytes
         */
        public int Filesize = 0;

        /**
         * Returns number of empty characters (spaces, tabs, end of lines) in a document
         * 
         * @return number of empty characters in a document
         */
        public int EmptyChars = 0;

        /**
         * Returns total size of inline <code>&lt;script></code> tags
         * 
         * @return total size of inline <code>&lt;script></code> tags, in bytes
         */
        public int InlineScriptSize = 0;

        /**
         * Returns total size of inline <code>&lt;style></code> tags
         * 
         * @return total size of inline <code>&lt;style></code> tags, in bytes
         */
        public int InlineStyleSize = 0;

        /**
         * Returns total size of inline event handlers (<code>onclick</code>, etc)
         * 
         * @return total size of inline event handlers, in bytes
         */
        public int InlineEventSize = 0;

        public override string ToString()
        {
            return string.Format(
                "Filesize={0}, Empty Chars={1}, Script Size={2}, Style Size={3}, Event Handler Size={4}",
                Filesize, EmptyChars, InlineScriptSize, InlineStyleSize, InlineEventSize);
        }
    }
}