// Shared styles and helpers for every template in this folder.
//
//   #import "lib.typ": *
//   #let data = load-data("invoice.json")
//   #show: theme
//
// Change something here and open lib.preview.typ to see it applied to every feature at once.

#let header-title = "Acme"

#let header-logo = "<svg width='457' height='400' viewBox='271 300 457 400' fill='none' xmlns='http://www.w3.org/2000/svg'>
  <path d='M726.708 621.101L544.791 305.046C542.962 301.835 539.306 300 535.649 300H462.973C459.317 300 455.66 301.835 453.832 305.046L272.371 621.101C270.543 624.312 270.543 628.44 272.371 631.651L308.938 694.954C310.766 698.165 314.423 700 318.079 700H681.457C685.114 700 688.771 698.165 690.599 694.954L727.165 631.651C728.537 628.44 728.537 624.312 726.708 621.101ZM469.373 321.101H517.823L381.613 557.798C379.785 561.009 379.785 565.138 381.613 568.349C383.442 571.56 387.098 573.395 390.755 573.395H530.164L554.389 615.596H299.796L469.373 321.101ZM499.54 521.101L517.823 552.752H481.257L499.54 521.101ZM299.796 636.697H572.215C575.872 636.697 579.529 634.862 581.357 631.651C583.185 628.44 583.185 624.312 581.357 621.101L511.881 500L536.106 457.798L663.174 678.899H324.021L299.796 636.697ZM681.457 668.349L544.791 431.651C542.962 428.44 539.306 426.606 535.649 426.606C531.992 426.606 528.336 428.44 526.507 431.651L457.031 552.752H408.581L536.106 331.651L705.683 626.606L681.457 668.349Z' fill='black'/>
</svg>"

#let header-right = (
	"[acme.org](https://acme.org)",
  "(555) 123-456"
)

// ---------------------------------------------------------------- tokens
#let body-font = ("Adwaita Sans", "DejaVu Sans", "Liberation Sans")
#let mono-font = ("Adwaita Mono", "DejaVu Sans Mono", "Liberation Mono")
#let brand-font = ("Adwaita Sans", "DejaVu Sans", "Liberation Sans")

#let accent = rgb("#1d4ed8")
#let muted-fill = luma(110)
#let rule-fill = luma(200)
#let header-fill = luma(240) // light to stay legible in greyscale print
#let rule = 0.5pt + black
#let inset-x = 4mm // table indent from the page margin

// ---------------------------------------------------------------- theme
#let theme(doc) = {
  set page(paper: "us-letter", margin: (x: 2cm, y: 2cm))
  set text(font: body-font, size: 10pt)
  set par(leading: 0.65em)
  show raw: set text(font: mono-font, size: 0.9em)
  show heading: set block(above: 1.2em, below: 0.6em)
  show table.cell.where(x: 0): strong
  // Coloured rather than underlined: reads as a link on screen, stays clean in print.
  show link: set text(fill: accent)
  doc
}

// ---------------------------------------------------------------- data
// Data arrives inline via `--input data=<json>` in production.
// Falls back to the template's .json so `typst watch` still works while editing.
#let load-data(fallback) = if "data" in sys.inputs {
  json(bytes(sys.inputs.data))
} else {
  json(fallback)
}

// ---------------------------------------------------------------- helpers
/// A horizontal rule matching the theme.
#let hrule(stroke: 0.5pt) = line(length: 100%, stroke: stroke + rule-fill)

/// Small grey caption text.
#let muted(body) = text(size: 0.85em, fill: muted-fill, body)

// Values arrive as plain JSON strings, so we honour a deliberately tiny subset
// of Markdown: `**bold**` and `[label](url)`. Anything unmatched — including an
// unclosed `**` or a stray bracket — is left as literal text rather than
// guessed at, which keeps addresses like "# 636" safe from the parser.
#let md-inline = regex(`\*\*(.+?)\*\*|\[(.+?)\]\((\S+?)\)`.text)

#let rich(value) = {
  if type(value) != str { return value }
  let out = []
  let pos = 0
  for m in value.matches(md-inline) {
    // Recursing on the captured text lets the two forms nest either way round.
    let (bold, label, url) = m.captures
    out += value.slice(pos, m.start)
    out += if bold != none { strong(rich(bold)) } else { link(url, rich(label)) }
    pos = m.end
  }
  out + value.slice(pos)
}

/// Document title, with an optional subtitle underneath.
#let title-block(title, subtitle: none) = {
  block(width: 100%)[
    #text(size: 1.8em, weight: "bold", title)
    #if subtitle != none [ 
    	#linebreak() 
      #v(0.05em)
      #muted(subtitle) 
    ]
  ]
  v(0.4em)
  hrule()
  v(0.8em)
}

/// A right aligned label/value pair, for headers and totals.
#let field(label, value, weight: "regular") = grid(
  columns: (1fr, auto),
  align: (left, right),
  muted(label), text(weight: weight, value),
)

/// 1234.5 -> "$1,234.50"
#let money(n, symbol: "$") = {
  let cents = calc.round(n * 100)
  let whole = str(calc.quo(cents, 100))
  let rem = calc.rem(cents, 100)
  let grouped = whole
    .clusters()
    .rev()
    .enumerate()
    .map(((i, c)) => if calc.rem(i, 3) == 0 and i > 0 { c + "," } else { c })
    .rev()
    .join()
  symbol + grouped + "." + (if rem < 10 { "0" } else { "" }) + str(rem)
}

/// A table with the theme's header styling applied.
#let data-table(columns: (), align: auto, header: (), ..rows) = table(
  columns: columns,
  align: align,
  stroke: (x, y) => (bottom: 0.5pt + rule-fill),
  table.header(..header.map(h => text(weight: "bold", h))),
  ..rows,
)


// Two-column label/value table, indented like the original.
// `title` adds a full-width shaded header row naming the table; inset-y is adjustable
// for templates that need to fit more rows on a page.
#let field-table(rows, title: none, inset-y: 7pt) = {
  // A header repeats itself if the table ever breaks across pages
  let head = if title == none { () } else {
    (table.header(table.cell(colspan: 2, fill: header-fill,
      text(tracking: 0.08em, upper(title)))),)
  }
  pad(left: inset-x, right: inset-x, table(
    columns: (38%, 62%),
    stroke: rule,
    inset: (x: 8pt, y: inset-y),
    ..head,
    ..rows.flatten().map(rich),
  ))
}

// ---------------------------------------------------------------- logo
// SVG embedded inline so the templates need no external assets.
// Attributes use single quotes so the Typst string needs no escaping.
// viewBox is cropped to the artwork bounds, so `height` sets the visible mark exactly.

#let logo(..args) = image(bytes(header-logo), format: "svg", ..args,)

// Icon + wordmark locked together, so the pair scales as one unit.
// mark-scale is relative to the text size, so the mark rises a touch above the
// cap line — the usual optical overshoot that keeps a solid mark from reading small.
#let wordmark(size: 24pt, mark-scale: 0.85) = box(
  box(logo(height: size * mark-scale), baseline: 1pt)
    + h(size * 0.20)
    + text(font: brand-font, size: size, weight: 800)[#header-title]
)

#let letterhead(header-right:header-right) = grid(
  columns: (1fr, auto),
  align: (left + horizon, right + horizon),
  wordmark(),
  text(size: 9pt, header-right.map(rich).join(linebreak())),
)
