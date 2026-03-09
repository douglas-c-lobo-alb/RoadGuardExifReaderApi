#set document(title: "RoadView — Arquitetura de Dados", author: "ExifApi")
#set page(
  paper: "a4",
  margin: (top: 2.5cm, bottom: 2.5cm, left: 3cm, right: 3cm),
  header: context {
    if counter(page).get().first() > 1 [
      #set text(size: 8pt, fill: luma(140))
      #grid(
        columns: (1fr, 1fr),
        align(left)[RoadView — Arquitetura de Dados],
        align(right)[Página #counter(page).display()]
      )
      #line(length: 100%, stroke: 0.4pt + luma(200))
    ]
  }
)

#set text(font: "Linux Libertine", size: 11pt, lang: "pt")
#set par(justify: true, leading: 0.75em)
#set heading(numbering: "1.")

#show heading.where(level: 1): it => {
  v(1.2em)
  block(
    fill: rgb("#1a1a2e"),
    radius: 4pt,
    inset: (x: 12pt, y: 8pt),
    width: 100%,
    text(fill: white, weight: "bold", size: 13pt, it)
  )
  v(0.5em)
}

#show heading.where(level: 2): it => {
  v(0.8em)
  text(fill: rgb("#16213e"), weight: "bold", size: 12pt, it)
  v(0.3em)
  line(length: 100%, stroke: 1.5pt + rgb("#0f3460"))
  v(0.3em)
}

#align(center)[
  #block(
    fill: gradient.linear(rgb("#0f3460"), rgb("#16213e")),
    radius: 8pt,
    inset: (x: 2cm, y: 1.2cm),
    width: 100%,
  )[
    #text(fill: white, size: 22pt, weight: "bold")[RoadView]
    #v(0.3em)
    #text(fill: rgb("#e0e0e0"), size: 13pt)[Arquitetura de Dados — Análise de Estradas]
    #v(0.8em)
    #text(fill: rgb("#a0b4c8"), size: 9pt)[Versão 1.0 · Março 2026]
  ]
]

#v(1.5em)

= Visão Geral

Na base da solução, temos um carro a andar pelas estradas, a analisar a turbulência com um acelerómetro e a tirar fotos e a detetar anomalias.

Para cada hexágono H3 por onde o carro passa, é registado o grau de turbulência da estrada. Para cada foto é aplicado um modelo de deteção de problemas (por exemplo buracos), posicionando-os em relação ao carro.

#v(0.5em)
#block(
  fill: rgb("#eef4fb"),
  stroke: (left: 3pt + rgb("#0f3460")),
  radius: (right: 4pt),
  inset: (x: 14pt, y: 10pt),
  width: 100%,
)[
  #text(weight: "semibold")[Componentes do sistema:]
  #v(0.3em)
  #grid(
    columns: (auto, 1fr),
    gutter: 6pt,
    [📍], [Localização por hexágonos H3 de resolução 15],
    [📷], [Captura fotográfica com metadata de posição e orientação],
    [📈], [Índice de turbulência por acelerómetro],
    [🤖], [Deteção automática de anomalias visuais com votação],
  )
]

= Entidades de Dados

== RoadView
#text(fill: luma(100), size: 9.5pt)[_Referência para uma foto tirada, e a posição e orientação de quando foi tirada_]

#v(0.5em)
#table(
  columns: (auto, auto, 1fr),
  stroke: (x, y) => if y == 0 { none } else { (top: 0.5pt + luma(220)) },
  fill: (_, y) => if y == 0 { rgb("#0f3460") } else if calc.odd(y) { rgb("#f7f9fc") } else { white },
  inset: (x: 10pt, y: 7pt),
  align: (left, left, left),

  table.header(
    text(fill: white, weight: "bold", size: 9.5pt)[Campo],
    text(fill: white, weight: "bold", size: 9.5pt)[Tipo],
    text(fill: white, weight: "bold", size: 9.5pt)[Descrição],
  ),
  [`H3_r15`],    [`H3Index`],    [Hexágono H3 de resolução 15 onde a foto foi tirada],
  [`Heading`],   [`float`],      [Orientação do carro em graus (0–360°) no momento da captura],
  [`Timestamp`], [`datetime`],   [Data e hora da captura],
  [`Photo`],     [`string`],     [Referência para o ficheiro de imagem no disco],
)

== RoadTurbulence
#text(fill: luma(100), size: 9.5pt)[_Grau de turbulência da estrada. O pior grau num H3\_r15 é o apresentado no frontend._]

#v(0.5em)
#table(
  columns: (auto, auto, 1fr),
  stroke: (x, y) => if y == 0 { none } else { (top: 0.5pt + luma(220)) },
  fill: (_, y) => if y == 0 { rgb("#0f3460") } else if calc.odd(y) { rgb("#f7f9fc") } else { white },
  inset: (x: 10pt, y: 7pt),
  align: (left, left, left),

  table.header(
    text(fill: white, weight: "bold", size: 9.5pt)[Campo],
    text(fill: white, weight: "bold", size: 9.5pt)[Tipo],
    text(fill: white, weight: "bold", size: 9.5pt)[Descrição],
  ),
  [`H3_r15`],          [`H3Index`], [Hexágono H3 de resolução 15],
  [`Timestamp`],       [`datetime`],[Data e hora da medição],
  [`TurbulenceIndex`], [`int`],     [Índice de turbulência percetível (0 = liso, 10 = muito turbulento)],
  [`HasSpeedBump`],    [`bool`],    [Indica a presença de lomba detetada],
  [`HasPothole`],      [`bool`],    [Indica a presença de buraco detetado],
)

== RoadVisualAnomaly
#text(fill: luma(100), size: 9.5pt)[_Anomalias detetadas nas fotos. Podem cair em hexágonos fora da estrada. Um H3\_r15 pode ter 0 ou mais anomalias._]

#v(0.5em)
#table(
  columns: (auto, auto, 1fr),
  stroke: (x, y) => if y == 0 { none } else { (top: 0.5pt + luma(220)) },
  fill: (_, y) => if y == 0 { rgb("#0f3460") } else if calc.odd(y) { rgb("#f7f9fc") } else { white },
  inset: (x: 10pt, y: 7pt),
  align: (left, left, left),

  table.header(
    text(fill: white, weight: "bold", size: 9.5pt)[Campo],
    text(fill: white, weight: "bold", size: 9.5pt)[Tipo],
    text(fill: white, weight: "bold", size: 9.5pt)[Descrição],
  ),
  [`H3_r15`],       [`H3Index`],   [Hexágono H3 de resolução 15],
  [`Anomaly`],      [`enum`],      [Tipo: `none` · `pothole` · `crack` · `missing_road_sign`],
  [`RoadView_FK`],  [`FK`],        [Referência para a tabela `RoadView`],
  [`Rectangle`],    [`Rect`],      [Coordenadas em píxeis (x,y) do canto sup. esq. e inf. dir. na foto],
  [`CreatedAt`],    [`datetime`],  [Criado quando a maioria dos votos concorda pela primeira vez],
  [`LastUpdate`],   [`datetime`],  [Atualizado sempre que a maioria continua a concordar],
  [`ResolvedAt`],   [`datetime?`], [Preenchido quando resolvido (maioria deixa de detetar a anomalia)],
)

== RoadVisualAnomalyVoting
#text(fill: luma(100), size: 9.5pt)[_Sistema de votação para evitar falsos positivos. A lógica de votação pode variar consoante o tipo de anomalia._]

#v(0.5em)
#block(
  fill: rgb("#fff8e7"),
  stroke: (left: 3pt + rgb("#e6a817")),
  radius: (right: 4pt),
  inset: (x: 14pt, y: 10pt),
  width: 100%,
)[
  #text(weight: "semibold", size: 9.5pt)[Sobre o sistema de votação:]
  #v(0.3em)
  #text(size: 9.5pt)[
    Quando um número suficiente de anomalias for detetado para um hexágono, verifica-se se a maioria está de acordo — só então é criado o respetivo registo em `RoadVisualAnomaly`. Tipos com mais falsos positivos podem exigir um limiar de votos mais elevado.
  ]
]

#v(0.5em)
#table(
  columns: (auto, auto, 1fr),
  stroke: (x, y) => if y == 0 { none } else { (top: 0.5pt + luma(220)) },
  fill: (_, y) => if y == 0 { rgb("#0f3460") } else if calc.odd(y) { rgb("#f7f9fc") } else { white },
  inset: (x: 10pt, y: 7pt),
  align: (left, left, left),

  table.header(
    text(fill: white, weight: "bold", size: 9.5pt)[Campo],
    text(fill: white, weight: "bold", size: 9.5pt)[Tipo],
    text(fill: white, weight: "bold", size: 9.5pt)[Descrição],
  ),
  [`Car_ID`],      [`GUID`],    [Identificador único do carro que submeteu o voto],
  [`H3_r15`],      [`H3Index`], [Hexágono H3 de resolução 15],
  [`Timestamp`],   [`datetime`],[Data e hora da deteção],
  [`Anomaly`],     [`enum`],    [Tipo: `none` · `pothole` · `crack` · `missing_road_sign`],
  [`Confidence`],  [`float`],   [Confiança do algoritmo (0.0 = nenhuma, 1.0 = máxima)],
  [`RoadView_FK`], [`FK`],      [Referência para a tabela `RoadView`],
  [`Rectangle`],   [`Rect`],    [Coordenadas em píxeis (x,y) do canto sup. esq. e inf. dir. na foto],
)
