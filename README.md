# GhostfolioSidekick

A sidecar (or sidekick) project to automatically import files from several brokers & crypto exchanges.
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

Assuming you configured an account with the name 'Trading 212' and 'Coinbase' in ghostfolio, the following structure should be used.
For example:
* Trading 212
  * Export2023.csv
* Coinbase
  * Export2022.csv
  * Export2023.csv

### Mapping File
A single csv file that contains mapping to convert currencies and symbols to something that can be found in yahoo finance or coin gecko

```
TYPE,SOURCE,TARGET
CURRENCY,GBX,GBp
IDENTIFIER,ATOM-USD,Cosmos USD
```

## Run in Docker
The docker image is: vibenl/ghostfoliosidekick

### Settings
| Envs |Description  |
|--|--|
|**GHOSTFOLIO_URL**  | The endpoint for your ghostfolio instance.   |
|**GHOSTFOLIO_ACCESTOKEN**  | The token as used to 'login' in the UI |
|**MAPPINGFILE**  | (optional) The path to the mapping file containing mapping for identifiers so it can be mapped automatically [Mapping File]() |
|**FileImporterPath**  | The path to the files (see [Import Path]()) |

## Contributing

* Feel free to submit any issue or PR's you think necessary
