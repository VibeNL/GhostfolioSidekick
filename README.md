# GhostfolioSidekick

A continuous running Docker container (a sidecar) to automatically import files from several brokers & crypto exchanges. 
The program checks every hour if any new transactions are found and inserts them in [ghostfolio](https://github.com/ghostfolio/ghostfolio). 
It can also correct & remove transactions in case they have changed (for example a different exchange rate) or the source file was deleted.

Additionally, for self-hosted instances, it can maintain symbols automatically.

( more to come? Help is always welcome! )

## Setup

### Ghostfolio
* Create an account in ghostfolio using anonymous option save your **KEY**.
* Create the relevant accounts in ghostfolio.

### Folder structure

FileImporterPath should point to a folder with the following structure:
 * Account Name
   * File1.csv
 * Account Name 2
   * File2.csv
   * File3.csv

Assuming you configured an account with the name 'Trading 212' and an account with 'De Giro' in ghostfolio, the following structure should be used.
For example:
* Trading 212
  * Export2023.csv
* De Giro
  * Export2022.csv
  * Export2023.csv

### Configuration File
A single json file csv file that contains mapping to convert currencies and symbols to a symbol that can be found via ghostfolio.
Also allows the following sybol settings
  - Setting Trackinsight on symbols
  - Adding / Updating Manual symbols 

```
{
	"mappings":[
		{ "type":"currency", "source":"GBX", "target":"GBp"},
		{ "type":"symbol", "source":"USDC", "target":"usd-coin"},
		{ "type":"symbol", "source":"BTC", "target":"bitcoin"}
	],
	"symbols":[
		{ "symbol": "VDUC.L", "trackinsight": "VUSC" },
		{ "symbol": "VFEM.L", "trackinsight": "VDEM" },
		{ "symbol": "DE0001102333", "manualSymbolConfiguration": { "currency":"EUR", "isin":"DE0001102333","name":"Bond Germany Feb 2024","assetSubClass":"BOND","assetClass":"EQUITY" } },
	]
}

```

#### Mappings
Change an identifier from the imported files to be compatible with Ghostfolio (for example certain symbols may not be found by Ghostfolio, so we can substituting the identifier with one that is recognized). 

| Fieldname | Type | Description |
|--|--|--|
| type | enum, 'currency' or 'symbol' | The type of mapping to be applied |
| source | any string | the name of the symbol as it appears in the csv files |
| target | any string | the name of the symbol to use within Ghostfolio |

#### Symbols
Maintaining symbols in ghostfolio

| Fieldname | Type | Description |
|--|--|--|
| symbol | any string | The name of the symbol|
| trackinsight | any string | the trackinsight key to be set |
| manualSymbolConfiguration | ManualSymbolConfiguration | see ManualSymbolConfiguration |

##### ManualSymbolConfiguration



### Supported formats
| Platform | Source of the files | Buy | Sell | Dividend | Interest & Cash balance |
|--|--|--|--|--|--|
| Generic importer | See below | X | X | X | X |
| Trading 212 | Export of transaction history | X | X | X | X |
| De Giro | Export of transaction history | X | X | X | X |
| Scalable Capital | The CSV files of the Baader bank. Type WUM and RKK | X | X | X | X |
| Bunq (bank) | Export CSV (Semicolom delimited) | - | - | - | X |
| Nexo (Experimental) | Export of transaction history | X | - | - | X |

#### Generic import format
Beside the supported exchanges and brokers there is also a generic format. This format is only usefull for stocks at the moment, not for cryptocurrency:

| Field | Value(s) | 
| ----- | ----- |
| OrderType | BUY ,SELL, DIVIDEND and INTEREST | 
| Symbol | The symbol to search
| Date | The date, yyyy-MM-dd |
| Currency | The currency of the unitprice and fee |
| Quantity | The amount of units |
| UnitPrice | The paid price per unit |
| Fee | The total fee paid for the transaction |

##### Example

OrderType,Symbol,Date,Currency,Quantity,UnitPrice,Fee
BUY,US67066G1040,2023-08-07,USD,0.0267001000,453.33,0.02

## Run in Docker
The docker image is named: vibenl/ghostfoliosidekick

### Settings
| Envs |Description  |
|--|--|
|**GHOSTFOLIO_URL**  | The endpoint for your ghostfolio instance.   |
|**GHOSTFOLIO_ACCESTOKEN**  | The token as used to 'login' in the UI |
|**FILEIMPORTER_PATH**  | The path to the files (see [Import Path]) |
|**CONFIGURATIONFILE_PATH**  | (optional) The path to the config file, see above |


## Contributing

* Feel free to submit any issue or PR's you think necessary
