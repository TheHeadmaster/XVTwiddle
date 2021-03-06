#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XVTwiddle.Parsing
{
    public class Markdown : DependencyObject
    {
        private const string markerOL = @"\d+[.]";

        private const string markerUL = @"[*+-]";

        /// <summary>
        /// maximum nested depth of [] and () supported by the transform; implementation detail
        /// </summary>
        private const int nestDepth = 6;

        /// <summary>
        /// Tabs are automatically converted to spaces as part of the transform this constant
        /// determines how "wide" those tabs become in spaces
        /// </summary>
        private const int tabWidth = 4;

        private static readonly Regex anchorInline = new Regex(
                    string.Format(CultureInfo.InvariantCulture, @"
                (                           # wrap whole match in $1
                    \[
                        ({0})               # link text = $2
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({1})               # href = $3
                        [ ]*
                        (                   # $4
                        (['""])           # quote char = $5
                        (.*?)               # title = $6
                        \5                  # matching quote
                        [ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                )", GetNestedBracketsPattern(), GetNestedParensPattern()),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex bold = new Regex(@"(\*\*|__) (?=\S) (.+?[*_]*) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex codeSpan = new Regex(@"
                    (?<!\\)   # Character before opening ` can't be a backslash
                    (`+)      # $1 = Opening run of `
                    (.+?)     # $2 = The code block
                    (?<!`)
                    \1
                    (?!`)", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex eoln = new Regex("\\s+");

        private static readonly Regex headerAtx = new Regex(@"
                ^(\#{1,6})  # $1 = string of #'s
                [ ]*
                (.+?)       # $2 = Header text
                [ ]*
                \#*         # optional closing #'s (not counted)
                \n+",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex headerSetext = new Regex(@"
                ^(.+?)
                [ ]*
                \n
                (=+|-+)     # $1 = string of ='s or -'s
                [ ]*
                \n+",
    RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex horizontalRules = new Regex(@"
            ^[ ]{0,3}         # Leading space
                ([-*_])       # $1: First marker
                (?>           # Repeated marker group
                    [ ]{0,2}  # Zero, one, or two spaces.
                    \1        # Marker character
                ){2,}         # Group repeated at least twice
                [ ]*          # Trailing spaces
                $             # End of line.
            ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex imageInline = new Regex(
            string.Format(CultureInfo.InvariantCulture, @"
                (                           # wrap whole match in $1
                    !\[
                        ({0})               # link text = $2
                    \]
                    \(                      # literal paren
                        [ ]*
                        ({1})               # href = $3
                        [ ]*
                        (                   # $4
                        (['""])           # quote char = $5
                        (.*?)               # title = $6
                        \5                  # matching quote
                        #[ ]*                # ignore any spaces between closing quote and )
                        )?                  # title is optional
                    \)
                )",
            GetNestedBracketsPattern(),
            GetNestedParensPatternWithWhiteSpace()),
                  RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex italic = new Regex(@"(\*|_) (?=\S) (.+?) (?<=\S) \1",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex lbrk = new Regex(@"\ {2,}\n");

        private static readonly Regex leadingWhitespace = new Regex(@"^[ ]*", RegexOptions.Compiled);

        private static readonly Regex listNested = new Regex(@"^" + wholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex listTopLevel = new Regex(@"(?:(?<=\n\n)|\A\n?)" + wholeList,
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly Regex newlinesLeadingTrailing = new Regex(@"^\n+|\n+\z", RegexOptions.Compiled);

        private static readonly Regex newlinesMultiple = new Regex(@"\n{1,}", RegexOptions.Compiled);

        private static readonly Regex outDent = new Regex(@"^[ ]{1," + tabWidth + @"}", RegexOptions.Multiline | RegexOptions.Compiled);

        private static readonly Regex strictBold = new Regex(@"([\W_]|^) (\*\*|__) (?=\S) ([^\r]*?\S[\*_]*) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex strictItalic = new Regex(@"([\W_]|^) (\*|_) (?=\S) ([^\r\*_]*?\S) \2 ([\W_]|$)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex table = new Regex(@"
            (                               # $1 = whole table
                [ \r\n]*
                (                           # $2 = table header
                    \|([^|\r\n]*\|)+        # $3
                )
                [ ]*\r?\n[ ]*
                (                           # $4 = column style
                    \|(:?-+:?\|)+           # $5
                )
                (                           # $6 = table row
                    (                       # $7
                        [ ]*\r?\n[ ]*
                        \|([^|\r\n]*\|)+    # $8
                    )+
                )
            )",
            RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

        private static readonly string wholeList
                                                                                                                                                                    = string.Format(CultureInfo.InvariantCulture, @"
            (                               # $1 = whole list
              (                             # $2
                [ ]{{0,{1}}}
                ({0})                       # $3 = first list item marker
                [ ]+
              )
              (?s:.+?)
              (                             # $4
                  \z
                |
                  \n{{2,}}
                  (?=\S)
                  (?!                       # Negative lookahead for another list item marker
                    [ ]*
                    {0}[ ]+
                  )
              )
            )", string.Format(CultureInfo.InvariantCulture, "(?:{0}|{1})", markerUL, markerOL), tabWidth - 1);

        private static string nestedBracketsPattern = null!;

        private static string nestedParensPattern = null!;

        private static string nestedParensPatternWithWhiteSpace = null!;

        private int listLevel;

        public static readonly DependencyProperty AssetPathRootProperty =
                    DependencyProperty.Register("AssetPathRootRoot", typeof(string), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for CodeStyle. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty CodeStyleProperty =
            DependencyProperty.Register("CodeStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for DocumentStyle. This enables
        // animation, styling, binding, etc...
        public static readonly DependencyProperty DocumentStyleProperty =
            DependencyProperty.Register("DocumentStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Heading1Style. This enables
        // animation, styling, binding, etc...
        public static readonly DependencyProperty Heading1StyleProperty =
            DependencyProperty.Register("Heading1Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Heading2Style. This enables
        // animation, styling, binding, etc...
        public static readonly DependencyProperty Heading2StyleProperty =
            DependencyProperty.Register("Heading2Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Heading3Style. This enables
        // animation, styling, binding, etc...
        public static readonly DependencyProperty Heading3StyleProperty =
            DependencyProperty.Register("Heading3Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for Heading4Style. This enables
        // animation, styling, binding, etc...
        public static readonly DependencyProperty Heading4StyleProperty =
            DependencyProperty.Register("Heading4Style", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for ImageStyle. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty ImageStyleProperty =
            DependencyProperty.Register("ImageStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for LinkStyle. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty LinkStyleProperty =
            DependencyProperty.Register("LinkStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for NormalParagraphStyle. This enables
        // animation, styling, binding, etc...
        public static readonly DependencyProperty NormalParagraphStyleProperty =
            DependencyProperty.Register("NormalParagraphStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for SeparatorStyle. This enables
        // animation, styling, binding, etc...
        public static readonly DependencyProperty SeparatorStyleProperty =
            DependencyProperty.Register("SeparatorStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public static readonly DependencyProperty TableBodyStyleProperty =
                    DependencyProperty.Register("TableBodyStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public static readonly DependencyProperty TableHeaderStyleProperty =
                    DependencyProperty.Register("TableHeaderStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public static readonly DependencyProperty TableStyleProperty =
                    DependencyProperty.Register("TableStyle", typeof(Style), typeof(Markdown), new PropertyMetadata(null));

        public string AssetPathRoot
        {
            get => (string)this.GetValue(AssetPathRootProperty);
            set => this.SetValue(AssetPathRootProperty, value);
        }

        public Style CodeStyle
        {
            get => (Style)this.GetValue(CodeStyleProperty);
            set => this.SetValue(CodeStyleProperty, value);
        }

        public Style DocumentStyle
        {
            get => (Style)this.GetValue(DocumentStyleProperty);
            set => this.SetValue(DocumentStyleProperty, value);
        }

        public Style Heading1Style
        {
            get => (Style)this.GetValue(Heading1StyleProperty);
            set => this.SetValue(Heading1StyleProperty, value);
        }

        public Style Heading2Style
        {
            get => (Style)this.GetValue(Heading2StyleProperty);
            set => this.SetValue(Heading2StyleProperty, value);
        }

        public Style Heading3Style
        {
            get => (Style)this.GetValue(Heading3StyleProperty);
            set => this.SetValue(Heading3StyleProperty, value);
        }

        public Style Heading4Style
        {
            get => (Style)this.GetValue(Heading4StyleProperty);
            set => this.SetValue(Heading4StyleProperty, value);
        }

        public ICommand HyperlinkCommand { get; set; }

        public Style ImageStyle
        {
            get => (Style)this.GetValue(ImageStyleProperty);
            set => this.SetValue(ImageStyleProperty, value);
        }

        public Style LinkStyle
        {
            get => (Style)this.GetValue(LinkStyleProperty);
            set => this.SetValue(LinkStyleProperty, value);
        }

        public Style NormalParagraphStyle
        {
            get => (Style)this.GetValue(NormalParagraphStyleProperty);
            set => this.SetValue(NormalParagraphStyleProperty, value);
        }

        public Style SeparatorStyle
        {
            get => (Style)this.GetValue(SeparatorStyleProperty);
            set => this.SetValue(SeparatorStyleProperty, value);
        }

        /// <summary>
        /// when true, bold and italic require non-word characters on either side
        /// WARNING: this is a significant deviation from the markdown spec
        /// </summary>
        public bool StrictBoldItalic { get; set; }

        public Style TableBodyStyle
        {
            get => (Style)this.GetValue(TableBodyStyleProperty);
            set => this.SetValue(TableBodyStyleProperty, value);
        }

        public Style TableHeaderStyle
        {
            get => (Style)this.GetValue(TableHeaderStyleProperty);
            set => this.SetValue(TableHeaderStyleProperty, value);
        }

        public Style TableStyle
        {
            get => (Style)this.GetValue(TableStyleProperty);
            set => this.SetValue(TableStyleProperty, value);
        }

        public Markdown()
        {
            this.HyperlinkCommand = NavigationCommands.GoToPage;
        }

        /// <summary>
        /// Reusable pattern to match balanced [brackets]. See Friedl's "Mastering Regular
        /// Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedBracketsPattern()
        {
            // in other words [this] and [this[also]] and [this[also[too]]] up to _nestDepth
            if (nestedBracketsPattern is null)
            {
                nestedBracketsPattern =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^\[\]]+      # Anything other than brackets
                     |
                       \[
                           ", nestDepth) + RepeatString(
                    @" \]
                    )*"
                    , nestDepth);
            }

            return nestedBracketsPattern;
        }

        /// <summary>
        /// Reusable pattern to match balanced (parens). See Friedl's "Mastering Regular
        /// Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedParensPattern()
        {
            // in other words (this) and (this(also)) and (this(also(too))) up to _nestDepth
            if (nestedParensPattern is null)
            {
                nestedParensPattern =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^()\s]+      # Anything other than parens or whitespace
                     |
                       \(
                           ", nestDepth) + RepeatString(
                    @" \)
                    )*"
                    , nestDepth);
            }

            return nestedParensPattern;
        }

        /// <summary>
        /// Reusable pattern to match balanced (parens), including whitespace. See Friedl's
        /// "Mastering Regular Expressions", 2nd Ed., pp. 328-331.
        /// </summary>
        private static string GetNestedParensPatternWithWhiteSpace()
        {
            // in other words (this) and (this(also)) and (this(also(too))) up to _nestDepth
            if (nestedParensPatternWithWhiteSpace is null)
            {
                nestedParensPatternWithWhiteSpace =
                    RepeatString(@"
                    (?>              # Atomic matching
                       [^()]+      # Anything other than parens
                     |
                       \(
                           ", nestDepth) + RepeatString(
                    @" \)
                    )*"
                    , nestDepth);
            }

            return nestedParensPatternWithWhiteSpace;
        }

        /// <summary>
        /// this is to emulate what's available in PHP
        /// </summary>
        private static string RepeatString(string value, int count)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            StringBuilder sb = new StringBuilder(value.Length * count);
            for (int i = 0; i < count; i++)
            {
                sb.Append(value);
            }

            return sb.ToString();
        }

        private Inline AnchorInlineEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string linkText = match.Groups[2].Value;
            string url = match.Groups[3].Value;
            string title = match.Groups[6].Value;

            Hyperlink result = this.Create<Hyperlink, Inline>(this.RunSpanGamut(linkText));
            result.Command = this.HyperlinkCommand;
            result.CommandParameter = url;
            if (!string.IsNullOrWhiteSpace(title))
            {
                result.ToolTip = title;
            }
            if (this.LinkStyle != null)
            {
                result.Style = this.LinkStyle;
            }

            return result;
        }

        private Block AtxHeaderEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string header = match.Groups[2].Value;
            int level = match.Groups[1].Value.Length;
            return this.CreateHeader(level, this.RunSpanGamut(header));
        }

        private Inline BoldEvaluator(Match match, int contentGroup)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string content = match.Groups[contentGroup].Value;
            return this.Create<Bold, Inline>(this.RunSpanGamut(content));
        }

        private Inline CodeSpanEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string span = match.Groups[2].Value;
            span = Regex.Replace(span, @"^[ ]*", ""); // leading whitespace
            span = Regex.Replace(span, @"[ ]*$", ""); // trailing whitespace

            Run result = new Run(span);
            if (this.CodeStyle != null)
            {
                result.Style = this.CodeStyle;
            }

            return result;
        }

        private TResult Create<TResult, TContent>(IEnumerable<TContent> content)
            where TResult : IAddChild, new()
        {
            TResult result = new TResult();
            foreach (TContent c in content)
            {
                result.AddChild(c);
            }

            return result;
        }

        private TableRow CreateTableRow(string[] txts, List<TextAlignment?> aligns)
        {
            TableRow tableRow = new TableRow();

            foreach (int idx in Enumerable.Range(0, txts.Length))
            {
                string txt = txts[idx];
                TextAlignment? align = aligns[idx];

                Paragraph paragraph = this.Create<Paragraph, Inline>(this.RunSpanGamut(txt));
                TableCell cell = new TableCell(paragraph);

                if (align.HasValue)
                {
                    cell.TextAlignment = align.Value;
                }

                tableRow.Cells.Add(cell);
            }

            while (tableRow.Cells.Count < aligns.Count)
            {
                tableRow.Cells.Add(new TableCell());
            }

            return tableRow;
        }

        /// <summary>
        /// Turn Markdown link shortcuts into hyperlinks
        /// </summary>
        /// <remarks>
        /// [link text](url "title")
        /// </remarks>
        private IEnumerable<Inline> DoAnchors(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // Next, inline-style links: [link text](url "optional title") or [link text](url
            // "optional title")
            return this.Evaluate(text, anchorInline, this.AnchorInlineEvaluator, defaultHandler);
        }

        /// <summary>
        /// Turn Markdown `code spans` into HTML code tags
        /// </summary>
        private IEnumerable<Inline> DoCodeSpans(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // * You can use multiple backticks as the delimiters if you want to include literal
            // backticks in the code span. So, this input:
            //
            // Just type ``foo `bar` baz`` at the prompt.
            //
            // Will translate to:
            // <p>Just type
            // <code>foo `bar` baz</code>
            // at the prompt.
            // </p>
            // There's no arbitrary limit to the number of backticks you can use as delimters. If
            // you need three consecutive backticks in your code, use four for delimiters, etc.
            //
            // * You can use spaces to get literal backticks at the edges:
            //
            // ... type `` `bar` `` ...
            //
            // Turns to:
            //
            // ... type
            // <code>`bar`</code>
            // ...

            return this.Evaluate(text, codeSpan, this.CodeSpanEvaluator, defaultHandler);
        }

        /// <summary>
        /// Turn Markdown headers into HTML header tags
        /// </summary>
        /// <remarks>
        /// Header 1 ========
        ///
        /// Header 2 --------
        ///
        /// # Header 1 ## Header 2 ## Header 2 with closing hashes ## ... ###### Header 6
        /// </remarks>
        private IEnumerable<Block> DoHeaders(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.Evaluate(text, headerSetext, m => this.SetextHeaderEvaluator(m),
                s => this.Evaluate(s, headerAtx, m => this.AtxHeaderEvaluator(m), defaultHandler));
        }

        /// <summary>
        /// Turn Markdown horizontal rules into HTML hr tags
        /// </summary>
        /// <remarks>
        /// ***
        /// * * * ---
        /// - - -
        /// </remarks>
        private IEnumerable<Block> DoHorizontalRules(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.Evaluate(text, horizontalRules, this.RuleEvaluator, defaultHandler);
        }

        /// <summary>
        /// Turn Markdown images into images
        /// </summary>
        /// <remarks>
        /// ![image alt](url)
        /// </remarks>
        private IEnumerable<Inline> DoImages(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.Evaluate(text, imageInline, this.ImageInlineEvaluator, defaultHandler);
        }

        /// <summary>
        /// Turn Markdown *italics* and **bold** into HTML strong and em tags
        /// </summary>
        private IEnumerable<Inline> DoItalicsAndBold(string text, Func<string, IEnumerable<Inline>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // <strong> must go first, then <em>
            return this.StrictBoldItalic
                ? this.Evaluate(text, strictBold, m => this.BoldEvaluator(m, 3),
                    s1 => this.Evaluate(s1, strictItalic, m => this.ItalicEvaluator(m, 3),
                    s2 => defaultHandler(s2)))
                : this.Evaluate(text, bold, m => this.BoldEvaluator(m, 2),
                   s1 => this.Evaluate(s1, italic, m => this.ItalicEvaluator(m, 2),
                   s2 => defaultHandler(s2)));
        }

        /// <summary>
        /// Turn Markdown lists into HTML ul and ol and li tags
        /// </summary>
        private IEnumerable<Block> DoLists(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // We use a different prefix before nested lists than top-level lists. See extended
            // comment in _ProcessListItems().
            return this.listLevel > 0
                ? this.Evaluate(text, listNested, this.ListEvaluator, defaultHandler)
                : this.Evaluate(text, listTopLevel, this.ListEvaluator, defaultHandler);
        }

        private IEnumerable<T> Evaluate<T>(string text, Regex expression, Func<Match, T> build, Func<string, IEnumerable<T>> rest)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            MatchCollection matches = expression.Matches(text);
            int index = 0;
            foreach (Match? m in matches)
            {
                if (m is null) { continue; }
                if (m.Index > index)
                {
                    string prefix = text[index..m.Index];
                    foreach (T t in rest(prefix))
                    {
                        yield return t;
                    }
                }

                yield return build(m);

                index = m.Index + m.Length;
            }

            if (index < text.Length)
            {
                string suffix = text[index..];
                foreach (T t in rest(suffix))
                {
                    yield return t;
                }
            }
        }

        /// <summary>
        /// splits on two or more newlines, to form "paragraphs";
        /// </summary>
        private IEnumerable<Block> FormParagraphs(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // split on two or more newlines
            string[] grafs = newlinesMultiple.Split(newlinesLeadingTrailing.Replace(text, ""));

            foreach (string g in grafs)
            {
                Paragraph block = this.Create<Paragraph, Inline>(this.RunSpanGamut(g));
                block.Style = this.NormalParagraphStyle;
                yield return block;
            }
        }

        private Inline ImageInlineEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string linkText = match.Groups[2].Value;
            string url = match.Groups[3].Value;
            BitmapImage? imgSource = null;
            try
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && !System.IO.Path.IsPathRooted(url))
                {
                    url = System.IO.Path.Combine(this.AssetPathRoot ?? string.Empty, url);
                }

                imgSource = new BitmapImage();
                imgSource.BeginInit();
                imgSource.CacheOption = BitmapCacheOption.None;
                imgSource.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
                imgSource.CacheOption = BitmapCacheOption.OnLoad;
                imgSource.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                imgSource.UriSource = new Uri(url);
                imgSource.EndInit();
            }
            catch (Exception)
            {
                return new Run("!" + url) { Foreground = Brushes.Red };
            }

            Image image = new Image { Source = imgSource, Tag = linkText };
            if (this.ImageStyle is null)
            {
                image.Margin = new Thickness(0);
            }
            else
            {
                image.Style = this.ImageStyle;
            }

            // Bind size so document is updated when image is downloaded
            if (imgSource.IsDownloading)
            {
                Binding binding = new Binding(nameof(BitmapImage.Width))
                {
                    Source = imgSource,
                    Mode = BindingMode.OneWay
                };

                BindingExpressionBase bindingExpression = BindingOperations.SetBinding(image, Image.WidthProperty, binding);

                void downloadCompletedHandler(object? sender, EventArgs args)
                {
                    imgSource.DownloadCompleted -= downloadCompletedHandler;
                    bindingExpression.UpdateTarget();
                }

                imgSource.DownloadCompleted += downloadCompletedHandler;
            }
            else
            {
                image.Width = imgSource.Width;
            }

            return new InlineUIContainer(image);
        }

        private Inline ItalicEvaluator(Match match, int contentGroup)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string content = match.Groups[contentGroup].Value;
            return this.Create<Italic, Inline>(this.RunSpanGamut(content));
        }

        private Block ListEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string list = match.Groups[1].Value;
            string listType = Regex.IsMatch(match.Groups[3].Value, markerUL) ? "ul" : "ol";

            // Turn double returns into triple returns, so that we can make a paragraph for the last
            // item in a list, if necessary:
            list = Regex.Replace(list, @"\n{2,}", "\n\n\n");

            List resultList = this.Create<List, ListItem>(this.ProcessListItems(list, listType == "ul" ? markerUL : markerOL));

            resultList.MarkerStyle = listType == "ul" ? TextMarkerStyle.Disc : TextMarkerStyle.Decimal;

            return resultList;
        }

        private ListItem ListItemEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string item = match.Groups[4].Value;
            string leadingLine = match.Groups[1].Value;

            if (!string.IsNullOrEmpty(leadingLine) || Regex.IsMatch(item, @"\n{2,}"))
            {
                // we could correct any bad indentation here..
                return this.Create<ListItem, Block>(this.RunBlockGamut(item));
            }
            else
            {
                // recursion for sub-lists
                return this.Create<ListItem, Block>(this.RunBlockGamut(item));
            }
        }

        /// <summary>
        /// convert all tabs to _tabWidth spaces; standardizes line endings from DOS (CR LF) or Mac
        /// (CR) to UNIX (LF); makes sure text ends with a couple of newlines; removes any blank
        /// lines (only spaces) in the text
        /// </summary>
        private string Normalize(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            StringBuilder output = new StringBuilder(text.Length);
            StringBuilder line = new StringBuilder();
            bool valid = false;

            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case '\n':
                        if (valid)
                        {
                            output.Append(line);
                        }

                        output.Append('\n');
                        line.Length = 0;
                        valid = false;
                        break;

                    case '\r':
                        if ((i < text.Length - 1) && (text[i + 1] != '\n'))
                        {
                            if (valid)
                            {
                                output.Append(line);
                            }

                            output.Append('\n');
                            line.Length = 0;
                            valid = false;
                        }
                        break;

                    case '\t':
                        int width = tabWidth - (line.Length % tabWidth);
                        for (int k = 0; k < width; k++)
                        {
                            line.Append(' ');
                        }

                        break;

                    case '\x1A':
                        break;

                    default:
                        if (!valid && text[i] != ' ')
                        {
                            valid = true;
                        }

                        line.Append(text[i]);
                        break;
                }
            }

            if (valid)
            {
                output.Append(line);
            }

            output.Append('\n');

            // add two newlines to the end before return
            return output.Append("\n\n").ToString();
        }

        /// <summary>
        /// Remove one level of line-leading spaces
        /// </summary>
        private string Outdent(string block) => outDent.Replace(block, "");

        /// <summary>
        /// Process the contents of a single ordered or unordered list, splitting it into individual
        /// list items.
        /// </summary>
        private IEnumerable<ListItem> ProcessListItems(string list, string marker)
        {
            // The listLevel global keeps track of when we're inside a list. Each time we enter a
            // list, we increment it; when we leave a list, we decrement. If it's zero, we're not in
            // a list anymore.

            // We do this because when we're not inside a list, we want to treat something like this:

            // I recommend upgrading to version
            // 8. Oops, now this line is treated as a sub-list.

            // As a single paragraph, despite the fact that the second line starts with a
            // digit-period-space sequence.

            // Whereas when we're inside a list (or sub-list), that line will be treated as the
            // start of a sub-list. What a kludge, huh? This is an aspect of Markdown's syntax
            // that's hard to parse perfectly without resorting to mind-reading. Perhaps the
            // solution is to change the syntax rules such that sub-lists must start with a starting
            // cardinal number; e.g. "1." or "a.".

            this.listLevel++;
            try
            {
                // Trim trailing blank lines:
                list = Regex.Replace(list, @"\n{2,}\z", "\n");

                string pattern = string.Format(CultureInfo.InvariantCulture,
                  @"(\n)?                      # leading line = $1
                (^[ ]*)                    # leading whitespace = $2
                ({0}) [ ]+                 # list marker = $3
                ((?s:.+?)                  # list item text = $4
                (\n{{1,2}}))
                (?= \n* (\z | \2 ({0}) [ ]+))", marker);

                Regex regex = new Regex(pattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
                MatchCollection matches = regex.Matches(list);
                foreach (Match? m in matches)
                {
                    if (m is { })
                    {
                        yield return this.ListItemEvaluator(m);
                    }
                }
            }
            finally
            {
                this.listLevel--;
            }
        }

        private Block RuleEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            Separator separator = new Separator();
            if (this.SeparatorStyle != null)
            {
                separator.Style = this.SeparatorStyle;
            }

            BlockUIContainer container = new BlockUIContainer(separator);
            return container;
        }

        /// <summary>
        /// Perform transformations that form block-level tags like paragraphs, headers, and list items.
        /// </summary>
        private IEnumerable<Block> RunBlockGamut(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.DoHeaders(text,
                s1 => this.DoHorizontalRules(s1,
                    s2 => this.DoLists(s2,
                    s3 => this.DoTable(s3,
                    sn => this.FormParagraphs(sn)))));

            //text = DoCodeBlocks(text);
            //text = DoBlockQuotes(text);

            //// We already ran HashHTMLBlocks() before, in Markdown(), but that
            //// was to escape raw HTML in the original Markdown source. This time,
            //// we're escaping the markup we've just created, so that we don't wrap
            //// <p> tags around block-level tags.
            //text = HashHTMLBlocks(text);

            //text = FormParagraphs(text);

            //return text;
        }

        /// <summary>
        /// Perform transformations that occur *within* block-level tags like paragraphs, headers,
        /// and list items.
        /// </summary>
        private IEnumerable<Inline> RunSpanGamut(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.DoCodeSpans(text,
                s0 => this.DoImages(s0,
                s1 => this.DoAnchors(s1,
                s2 => this.DoItalicsAndBold(s2,
                s3 => this.DoText(s3)))));

            //text = EscapeSpecialCharsWithinTagAttributes(text);
            //text = EscapeBackslashes(text);

            //// Images must come first, because ![foo][f] looks like an anchor.
            //text = DoImages(text);
            //text = DoAnchors(text);

            //// Must come after DoAnchors(), because you can use < and >
            //// delimiters in inline links like [this](<url>).
            //text = DoAutoLinks(text);

            //text = EncodeAmpsAndAngles(text);
            //text = DoItalicsAndBold(text);
            //text = DoHardBreaks(text);

            //return text;
        }

        private Block SetextHeaderEvaluator(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string header = match.Groups[1].Value;
            int level = match.Groups[2].Value.StartsWith("=", StringComparison.Ordinal) ? 1 : 2;

            //TODO: Style the paragraph based on the header level
            return this.CreateHeader(level, this.RunSpanGamut(header.Trim()));
        }

        private Block TableEvalutor(Match match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            string wholeTable = match.Groups[1].Value;
            string header = match.Groups[2].Value.Trim();
            string style = match.Groups[4].Value.Trim();
            string row = match.Groups[6].Value.Trim();

            string[] styles = style[1..^1].Split('|');
            string[] headers = header[1..^1].Split('|');
            List<string[]> rowList = row.Split('\n').Select(ritm =>
            {
                string trimRitm = ritm.Trim();
                return trimRitm[1..^1].Split('|');
            }).ToList();

            int maxColCount =
                Math.Max(
                    Math.Max(styles.Length, headers.Length),
                    rowList.Select(ritm => ritm.Length).Max()
                );

            // table style
            List<TextAlignment?> aligns = new List<TextAlignment?>();
            foreach (string colStyleTxt in styles)
            {
                char firstChar = colStyleTxt.First();
                char lastChar = colStyleTxt.Last();

                // center
                if (firstChar == ':' && lastChar == ':')
                {
                    aligns.Add(TextAlignment.Center);
                }

                // right
                else if (lastChar == ':')
                {
                    aligns.Add(TextAlignment.Right);
                }

                // left
                else if (firstChar == ':')
                {
                    aligns.Add(TextAlignment.Left);
                }

                // default
                else
                {
                    aligns.Add(null);
                }
            }
            while (aligns.Count < maxColCount)
            {
                aligns.Add(null);
            }

            // table
            Table table = new Table();
            if (this.TableStyle != null)
            {
                table.Style = this.TableStyle;
            }

            // table columns
            while (table.Columns.Count < maxColCount)
            {
                table.Columns.Add(new TableColumn());
            }

            // table header
            TableRowGroup tableHeaderRG = new TableRowGroup();
            if (this.TableHeaderStyle != null)
            {
                tableHeaderRG.Style = this.TableHeaderStyle;
            }

            TableRow tableHeader = this.CreateTableRow(headers, aligns);
            tableHeaderRG.Rows.Add(tableHeader);
            table.RowGroups.Add(tableHeaderRG);

            // row
            TableRowGroup tableBodyRG = new TableRowGroup();
            if (this.TableBodyStyle != null)
            {
                tableBodyRG.Style = this.TableBodyStyle;
            }
            foreach (string[] rowAry in rowList)
            {
                TableRow tableBody = this.CreateTableRow(rowAry, aligns);
                tableBodyRG.Rows.Add(tableBody);
            }
            table.RowGroups.Add(tableBodyRG);

            return table;
        }

        public Block CreateHeader(int level, IEnumerable<Inline> content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Paragraph block = this.Create<Paragraph, Inline>(content);

            switch (level)
            {
                case 1:
                    if (this.Heading1Style != null)
                    {
                        block.Style = this.Heading1Style;
                    }
                    break;

                case 2:
                    if (this.Heading2Style != null)
                    {
                        block.Style = this.Heading2Style;
                    }
                    break;

                case 3:
                    if (this.Heading3Style != null)
                    {
                        block.Style = this.Heading3Style;
                    }
                    break;

                case 4:
                    if (this.Heading4Style != null)
                    {
                        block.Style = this.Heading4Style;
                    }
                    break;
            }

            return block;
        }

        public IEnumerable<Block> DoTable(string text, Func<string, IEnumerable<Block>> defaultHandler)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return this.Evaluate(text, table, this.TableEvalutor, defaultHandler);
        }

        public IEnumerable<Inline> DoText(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            string[] lines = lbrk.Split(text);
            bool first = true;
            foreach (string line in lines)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    yield return new LineBreak();
                }

                string t = eoln.Replace(line, " ");
                yield return new Run(t);
            }
        }

        public FlowDocument Transform(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            text = this.Normalize(text);
            FlowDocument document = this.Create<FlowDocument, Block>(this.RunBlockGamut(text));

            if (this.DocumentStyle != null)
            {
                document.Style = this.DocumentStyle;
            }
            else
            {
                document.PagePadding = new Thickness(12);
            }

            return document;
        }
    }
}

#pragma warning restore 1591