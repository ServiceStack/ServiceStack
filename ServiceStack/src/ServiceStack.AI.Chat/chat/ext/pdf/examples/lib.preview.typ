// Every feature lib.typ styles, on one page. Edit lib.typ, render this, see what changed.
#import "lib.typ": *

#show: theme

#letterhead()

#v(1em)
#title-block("lib.typ preview", subtitle: "Each block below is styled by lib.typ - change a token there and re-render this")

= Headings and text
== A second level heading

Body copy with *bold*, _italic_, #underline[underlined] and #strike[struck out] words, a
#link("https://typst.app/docs")[link to the typst docs], and some `raw text`.

- A bullet list
- with a second item
  - and a nested one

+ A numbered list
+ with a second item

#hrule()

= Tables
Column one is bold via a `show` rule in the theme, so tables never need to repeat it.

#data-table(
  columns: (1fr, auto, auto),
  align: (left, right, right),
  header: ([Item], [Qty], [Amount]),
  [Design retainer], [3], money(1500),
  [API integration], [34], money(165 * 34),
  [Spare kit], [1], money(15),
)

#v(0.6em)
#field("Subtotal", money(1500 + 165 * 34 + 15))
#field("Total due", money(1500 + 165 * 34 + 15), weight: "bold")

#hrule()

#field-table((
	("Header 1","Cell 1"),
  ("Header 2","Cell 2")
))

= Layout
#grid(
  columns: (1fr, 1fr),
  gutter: 1em,
  [
    #muted("Left column")

    Text set in the theme's body font at its base size.
  ],
  [
    #muted("Right column")

    #align(center)[Centred content]
  ],
)

#v(0.6em)
#align(center)[#box(fill: luma(245), inset: 8pt, radius: 3pt)[A boxed, centred note]]

= Maths and raw blocks
$ sum_(i=1)^n i = (n (n + 1)) / 2 $

#v(1fr)
#muted[Rendered from lib.preview.typ]

#hrule()

= Example Usage

```typst
#import "lib.typ": *
#let data = load-data("invoice.json")
#show: theme
```

// Markdown, if you want it - needs the cmarker package, downloaded on first compile:
// #import "@preview/cmarker:0.1.10"
// #cmarker.render(```md
// # From Markdown
// **bold** and _italic_
// ```)
