# GhostfolioSidekick

[![Build & deploy application](https://github.com/VibeNL/GhostfolioSidekick/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/VibeNL/GhostfolioSidekick/actions/workflows/docker-publish.yml)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=VibeNL_GhostfolioSidekick&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=VibeNL_GhostfolioSidekick)

[![Shield: Buy me a coffee](https://img.shields.io/badge/Buy%20me%20a%20coffee-Support-yellow?logo=buymeacoffee)](https://www.buymeacoffee.com/vibenl)

A continuous running Docker container (a sidecar) to automatically import files from several brokers & crypto exchanges. 
The program checks every hour if any new transactions are found and inserts them in [ghostfolio](https://github.com/ghostfolio/ghostfolio). 
It can also correct & remove transactions in case they have changed (for example a different exchange rate) or the source file was deleted.

Additionally, for self-hosted instances, it can maintain symbols automatically.
 - Set trackinsight property of symbols
 - Create manual symbols
 - Delete symbols that are no longer used

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
    "settings" : {
	    "dataprovider.preference.order": "COINGECKO,YAHOO", // default "YAHOO,COINGECKO"
		"use.dust.currency" : "EUR", // default is "USD",
		"use.dust.threshold" : 0.0001 // default is 0.0001,
		"use.crypto.workaround.dust.threshold": 0.01 // default is 0.001,
		"use.crypto.workaround.stakereward.add.to.last.buy" : true // default is false,
		"delete.unused.symbols": false // default is true. Note generated symbols like INTEREST and FEE are always deleted since they can't be reused.
		"use.dividend.workaround.tax.substract.from.amount": true // default is false. If set to true, the tax is substracted from the dividend amount. If set to false, the tax is added as a fee.
	},
	"platforms":[
		{ "name": "De Giro", "url":"https://www.degiro.nl/" }
	],
	"accounts":[
		{ "name": "De Giro", "currency":"EUR", "platform":"De Giro" }
	],
	"mappings":[
		{ "type":"currency", "source":"GBX", "target":"GBp"},
		{ "type":"symbol", "source":"USDC", "target":"usd-coin"},
		{ "type":"symbol", "source":"BTC", "target":"bitcoin"}
	],
	"symbols":[
		{ "symbol": "VDUC.L", "trackinsight": "VUSC" },
		{ "symbol": "VFEM.L", "trackinsight": "VDEM" },
		{ "symbol": "DE0001102333", "manualSymbolConfiguration": { "currency":"EUR", "isin":"DE0001102333","name":"Bond Germany Feb 2024","assetSubClass":"BOND","assetClass":"EQUITY" } },
		{ "symbol": "PhysicalGoldEuroPerKilogram", "manualSymbolConfiguration": { "currency":"EUR", "isin":"PhysicalGoldEuroPerKilogram","name":"Physical Gold EUR/KG","assetSubClass":"PRECIOUS_METAL","assetClass":"COMMODITY", "scraperConfiguration":{ "url": "<url>", "selector":"<selector>", "locale":"<locale>"} } }
	],
	"benchmarks":[
		{ "symbol": "^AEX" },
		{ "symbol": "^SPX" },
	]
}

```

#### Settings

##### use.dust.threshold (Experimental)
This settings does control if a workaround is used for dust (very small amount that cannot be sold or due to rounding errors).
The amount is the total value of the assets (thus quantity times unitprice). Unitprice is converted to the currency defined by *use.dust.currency*.

For crypto specifically, another setting will take presidence. See ***use.crypto.workaround.dust.threshold***. This due to the likelyness of higher values for the dust of cryptocurrencies.

##### use.crypto.workaround.dust.threshold (Experimental)
This settings does control if a workaround is used for dust (very small amount that cannot be sold or due to rounding errors).
The amount is the total value of the assets (thus quantity times unitprice). Unitprice is converted to the currency defined by *use.dust.currency*.

##### use.crypto.workaround.stakereward.add.to.last.buy (Experimental)
This settings does control if a workaround is used for staking rewards. 
If set to true, the staking reward is added to the last buy activity.

#### Platform and Account
Creates platforms and accounts if not yet created

Fields are identical to the UI

#### Benchmarks
Add a symbol as a benchmark

#### Mappings
Change an identifier from the imported files to be compatible with Ghostfolio (for example certain symbols may not be found by Ghostfolio, so we can substituting the identifier with one that is recognized). 

| Fieldname | Type | Description |
|--|--|--|
| type | one of: 'currency', 'symbol' | The type of mapping to be applied |
| source | any string | the name of the symbol as it appears in the csv files |
| target | any string | the name of the symbol to use within Ghostfolio |

#### Symbols
Maintaining symbols in ghostfolio

| Fieldname | Type | Description |
|--|--|--|
| symbol | any string | The name of the symbol|
| trackinsight | any string | The trackinsight key to be set |
| manualSymbolConfiguration | ManualSymbolConfiguration | see ManualSymbolConfiguration. Will be created if it does not exists |

##### ManualSymbolConfiguration

| Fieldname | Type | Description |
|--|--|--|
| currency | any string | The currency of the symbol |
| isin | any string | The ISIN to be set |
| name | any string | The name of the symbol |
| assetSubClass | one of: 'CRYPTOCURRENCY', 'ETF', 'STOCK', 'MUTUALFUND', 'BOND', 'COMMODITY', 'PRECIOUS_METAL', 'PRIVATE_EQUITY'| Same list as Ghostfolio |
| assetClass | one of: 'CASH', 'COMMODITY', 'EQUITY', 'FIXED_INCOME', 'REAL_ESTATE' | Same list as Ghostfolio |
| scraperConfiguration| object with url, selector and optional locale | The scraperconfiguration as used in Ghostfolio (NOTE: no support for headers yet)|

### Supported formats
The goal is to support all platforms as best as possible. Due to the continuous growth of Ghostfolio, new features may be added when possible.

| Platform | Source of the files | Documentation |
|--|--|--|
| Bitvavo (Broken) | Export of transaction history | |
| Bunq (Bank) | Export CSV (Semicolom delimited) | |
| Centraal Beheer Begeleid beleggen (Dutch insurance company) | Export of transaction history via print pdf | [Documentation](./Documentation/Parsers/CentraalBeheer.md) |
| Coinbase (Broken) | Export of transaction history | |
| De Giro | Export of account history (Language dependend, NL and PT supported currently) | [Documentation](./Documentation/Parsers/DeGiro.md) |
| Generic importer | See below | |
| Nexo (Broken) | Export of transaction history | |
| NIBC (Bank) | Export CSV (Semicolom delimited) | |
| Scalable Capital | The CSV files of the Baader bank. Type WUM and RKK | |
| Scalable Capital (Prime only) | The CSV files export via the transaction view | |
| Trading Republic | Montly Statements and individual invoices |  [Documentation](./Documentation/Parsers/TradeRepublic.md) |
| Trading 212 | Export of transaction history | [Documentation](./Documentation/Parsers/Trading212.md) |

#### Generic import format
Beside the supported exchanges and brokers there is also a generic format. 
This format is only usefull for stocks at the moment, not for cryptocurrency:

| Field | Value(s) | 
| ----- | ----- |
| OrderType | BUY ,SELL, DIVIDEND and INTEREST | 
| Symbol | The symbol to search
| Date | The date, yyyy-MM-dd |
| Currency | The currency of the unitprice and fee |
| Quantity | The amount of units |
| UnitPrice | The paid price per unit |
| Fee | The total fee paid for the transaction. Is optional |
| Tax | The total tax on the transaction, is used to adjust the unitprice. Is optional |
| Description | A description, not used in ghostfolio itself. Is optional |
| Id | The transaction id. Is optional |

For stock splits there is a seperate format
| Field | Value(s) | 
| ----- | ----- |
| Symbol | The symbol to search
| Date | The date, yyyy-MM-dd |
| StockSplitFrom | The number of stock in the old situation |
| StockSplitTo | The number of stock in the new situation |

##### Example

File1:
OrderType,Symbol,Date,Currency,Quantity,UnitPrice,Fee
BUY,US67066G1040,2023-08-07,USD,0.0267001000,453.33,0.02

File2:
Symbol,Date,StockSplitFrom,StockSplitTo
US67066G1040,2023-08-07,1,3

## Run in Docker
The docker image is named: vibenl/ghostfoliosidekick

Example docker-compose.yml:
```
ghostfoliosidekick:
	image: vibenl/ghostfoliosidekick:latest
	container_name: Ghostfolio-Ghostfoliosidekick
	hostname: ghostfoliosidekick
	security_opt:
		- no-new-privileges:true
	environment:
		- GHOSTFOLIO_URL=url
		- GHOSTFOLIO_ACCESTOKEN=abc
		- FILEIMPORTER_PATH=/var/lib/data
		- CONFIGURATIONFILE_PATH=/var/lib/data/config.json
	restart: always
	volumes:
		- /volume1/docker/ghostfolio/sidekick:/var/lib/data:r
	depends_on:
	ghostfolio:
		condition: service_started
```

### Settings
| Envs |Description  |
|--|--|
|**GHOSTFOLIO_URL**  | The endpoint for your ghostfolio instance.   |
|**GHOSTFOLIO_ACCESTOKEN**  | The token as used to 'login' in the UI |
|**FILEIMPORTER_PATH**  | The path to the files (see [Import Path]) |
|**CONFIGURATIONFILE_PATH**  | (optional) The path to the config file, for example '/files/config/config.json' |
|**TROTTLE_WAITINSECONDS**  | (optional) The time in seconds between calls to Ghostfolio. Defaults to no waittime. |

## Contributing

* Feel free to submit any issue or PR's you think necessary
