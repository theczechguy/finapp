# Bank Import Profiles

YAML definitions in this folder describe how to transform a bank export into FinApp's normalized transaction schema. Each file represents one bank layout.

```yaml
id: kb
version: 1
metadata:
  displayName: "Komerční banka"
  description: "Standard CSV export from KB internet banking"
  sampleFile: "tests/Vypis_1234840660237_20251001_20251112.csv"
parser:
  encoding: "windows-1250"
  delimiter: ";"
  quote: "\""
  decimalSeparator: ","
  dateFormat: "dd.MM.yyyy"
  defaultSkipRows: 15
  trimEmptyPreamble: true
columns:
  - header: "Datum zauctovani"
    target: bookingDate
  - header: "Datum provedeni"
    target: transactionDate
  - header: "Castka"
    target: amount
    transform: signedAmount
  - header: "Mena"
    target: currency
  - header: "Protistrana"
    target: counterparty
  - header: "Nazev protiuctu"
    target: counterpartyAccount
  - header: "Popis pro me"
    target: memo
  - header: "Zprava pro prijemce"
    target: recipientMessage
rules:
  dropRows:
    - startsWith: "Pocatecni zustatek"
    - startsWith: "Konecny zustatek"

expenseFieldMappings:
  - field: name
    sourceHeaders:
      - "Nazev protiuctu"
      - "Protistrana"
    target: counterparty
    fallback: "Imported transaction"
  - field: amount
    sourceHeaders:
      - "Castka"
    target: amount
  - field: currency
    sourceHeaders:
      - "Mena"
    target: currency
  - field: date
    sourceHeaders:
      - "Datum zauctovani"
    target: bookingDate
  - field: expenseType
    fallback: "Family"
```

## Field reference

| Section | Field | Description |
|---------|-------|-------------|
| `id` | string | Stable identifier used internally. |
| `version` | number | Increment when profile structure changes. Enables migrations. |
| `metadata.displayName` | string | Human-friendly name shown in UI. |
| `metadata.description` | string | Short hint for the user. |
| `metadata.sampleFile` | string | Optional path to example export. |
| `parser.encoding` | string | Text encoding passed to file reader. |
| `parser.delimiter` | string | Column delimiter (usually `;` or `,`). |
| `parser.quote` | string | Quote character. |
| `parser.decimalSeparator` | string | Decimal separator for amount parsing. |
| `parser.dateFormat` | string | `DateTime` format string for transaction dates. |
| `parser.defaultSkipRows` | int | Default number of metadata rows to skip. |
| `parser.trimEmptyPreamble` | bool | Remove empty rows before applying skip count. |
| `columns[].header` | string/regex | Header label or regex used to match the column. |
| `columns[].target` | enum | Normalized field name (`transactionDate`, `amount`, etc.). |
| `columns[].transform` | string | Optional transform key for custom logic. |
| `columns[].required` | bool | Fail preview if column missing (default `false`). |
| `columns[].notes` | string | Annotation for maintainers. |
| `rules.dropRows[]` | object | Criteria for filtering out non-transaction rows. |
| `expenseFieldMappings[]` | array | Declares how FinApp-required expense fields are populated from the parsed columns or constants. |
| `expenseFieldMappings[].field` | enum | FinApp expense field identifier (`name`, `amount`, `currency`, `date`, `expenseType`, etc.). |
| `expenseFieldMappings[].sourceHeaders` | array | Ordered list of header labels to probe; the first non-empty column supplies the value. |
| `expenseFieldMappings[].target` | string | Optional reference to a column `target` defined above; when set, the mapped column provides the value. |
| `expenseFieldMappings[].fallback` | string | Optional literal default used when the referenced column is empty or absent. |
| `expenseFieldMappings[].notes` | string | Optional maintainer note about the mapping or downstream expectations. |

Expand this schema as needed—validation will live in the import service.
