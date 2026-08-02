// Data lives in the sidecar invoice.json - edit either side and the preview re-renders
#import "lib.typ": *
#let data = load-data("invoice.json")

#show: theme

#letterhead()
#v(1em)

#let currency = data.invoice.at("currency", default: "$")
#let money(n) = {
  if n == 0 {
    currency + "000"
  } else {
    let cents = calc.round(n * 100)
    let whole = str(calc.quo(cents, 100))
    let rem = calc.rem(cents, 100)
    let grouped = whole.clusters().rev().enumerate().map(((i, c)) => if calc.rem(i, 3) == 0 and i > 0 { c + "," } else { c }).rev().join()
    currency + grouped + "." + (if rem < 10 { "0" } else { "" }) + str(rem)
  }
}

#let subtotal = data.items.map(i => i.qty * i.rate).sum(default: 0)
#let tax = subtotal * data.at("taxRate", default: 0)
#let total = subtotal + tax

// Header - same shape as letterhead(): the identity on the left, small details right
#let head-label(s) = muted(text(tracking: 0.08em, upper(s)))

#grid(
  columns: (1fr, auto),
  align: (left + horizon, right + horizon),
  // subordinate to the 24pt wordmark above it, but in the same brand voice
  text(size: 14pt)[
    #text(weight: "bold")[#data.invoice.number]
    #linebreak()
    #text(size: 9pt)[#data.invoice.date]
  ],
  text(font: brand-font, size: 24pt, weight: 800)[INVOICE]
)

#v(0.8em)
#hrule()

// Recipient
#v(1.2em)
#head-label("Invoice to")
#v(0.5em)
#text(weight: "bold")[#data.to.name]
#linebreak()
#data.to.lines.map(rich).join(linebreak())

// Line items
#v(7em)
#table(
  columns: (1fr, 4em, 8em, 7em),
  align: (left, right, right, right),
  stroke: none,
  inset: (x: 4pt, y: 8pt),
  table.header(
    ..([Item], [Qty], [Unit Price], [Total]).map(h => text(
      weight: "bold",
      size: 10pt,
    )[#upper(h)]),
  ),
  table.hline(stroke: 0.7pt + luma(90)),
  ..data.items
    .map(item => (
      [#item.description],
      [#item.qty],
      [#item.unitPrice],
      [#money(item.qty * item.rate)],
    ))
    .flatten(),
  table.hline(stroke: 0.7pt + luma(90)),
)

// Totals
#align(right)[
  #table(
    columns: (auto, 8em),
    align: (right, right),
    stroke: none,
    inset: (x: 4pt, y: 5pt),
    text(weight: "bold", size: 9pt)[SUBTOTAL], text(weight: "bold")[#money(subtotal)],
    [Tax], [#calc.round(data.at("taxRate", default: 0) * 100, digits: 0)%],
    table.hline(stroke: 0.7pt + luma(90)),
    text(size: 13pt, weight: "bold")[Total], text(size: 13pt, weight: "bold")[#money(total)],
  )
]

#v(1.2em)
#text(size: 20pt)[Thank you!]

// Footer
#v(7.1em)
#grid(
  columns: (1fr, 1fr),
  align: (left, right),
  [
    #text(weight: "bold")[Payment Information]
    #v(0.7em)
    #data.paymentBank
    #linebreak()
    Account Name: #data.paymentAccountName
    #linebreak()
    Account no. : #data.paymentAccountNumber
    #linebreak()
    Pay by : #data.paymentDue
  ],
  [
    #text(weight: "bold")[#data.from.name]
    #v(0.7em)
    #data.from.lines.at(3)
    #linebreak()
    #data.from.lines.at(0)
    #linebreak()
    #data.from.lines.at(1)
  ],
)
