# Scraper Utilities Documentation

The Scraper Utilities are tools designed to automate the process of scraping transaction data from various brokers. These utilities can be used to extract transaction data and save it in a CSV format for further processing.

## Supported Brokers
- Scalable Capital
- Trade Republic

## Prerequisites

Before using the scraper utilities, ensure you have the following installed:
- [.NET SDK](https://dotnet.microsoft.com/download)
- [Google Chrome](https://www.google.com/chrome/) (for Playwright)

## Setup

1. Clone the repository:
   ```sh
   git clone https://github.com/VibeNL/GhostfolioSidekick.git
   cd GhostfolioSidekick
   ```

2. Install the required .NET tools:
   ```sh
   dotnet tool restore
   ```

3. Install Playwright:
   ```sh
   dotnet playwright install
   ```

## Usage

To use the scraper utilities, run the following command:

```sh
dotnet run --project Tools/ScraperUtilities/ScraperUtilities.csproj [Broker] [OutputFile] [AdditionalArguments]
```

- `[Broker]`: The name of the broker (e.g., `ScalableCapital`, `TradeRepublic`).
- `[OutputFile]`: The path to the output CSV file.
- `[AdditionalArguments]`: Additional arguments required for the specific broker (e.g., username, password, etc.).

### Example Commands

#### Scalable Capital

```sh
dotnet run --project Tools/ScraperUtilities/ScraperUtilities.csproj ScalableCapital output.csv your_username your_password
```

#### Trade Republic

```sh
dotnet run --project Tools/ScraperUtilities/ScraperUtilities.csproj TradeRepublic output.csv country_code phone_number pin_code
```

## Additional Information

For detailed instructions and examples, refer to the [Scraper Utilities Documentation](Documentation/ScraperUtilities.md).

## Troubleshooting

If you encounter any issues, please check the following:
- Ensure you have the latest version of .NET SDK installed.
- Ensure you have the latest version of Google Chrome installed.
- Ensure you have installed Playwright using the `dotnet playwright install` command.

If the issue persists, feel free to open an issue on the [GitHub repository](https://github.com/VibeNL/GhostfolioSidekick/issues).

## Contributing

Contributions are welcome! If you have any suggestions or improvements, please submit a pull request or open an issue.

