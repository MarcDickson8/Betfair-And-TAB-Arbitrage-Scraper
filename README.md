# Betfair-And-TAB-Arbitrage-Scraper
This C# program searches for arbitrage opportunities between Betfair and TAB Horse Racing using selenium for web scraping and Pushover API for mobile phone notifications.


This project is a sophisticated automated web scraping bot designed to identify and report arbitrage opportunities in horse racing markets. It continuously monitors two major betting websites, **Betfair Australia** and **TAB Australia**, comparing their odds to find profitable scenarios.

## Features

*   **Automated Arbitrage Detection**: Automatically compares TAB's "Win" odds against Betfair's "Lay" odds. An arbitrage opportunity is flagged when `TAB Win Odds > Betfair Lay Odds`.
*   **Multi-Market Scraping**: Scrapes both "Win" and "Place" (Top 3) markets to maximize opportunities.
*   **Real-time Notifications**: Uses the **Pushover** service to send instant push notifications to your phone when an arbitrage opportunity is found, when the bot starts, and when it restarts after a crash.
*   **Resilient Scraping**: Built with `SeleniumUndetectedChromeDriver` to mimic human behavior and reduce the risk of being blocked. It includes randomized delays and retry logic to handle dynamic web content gracefully.
*   **Persistent Logging**: All identified arbitrage opportunities are saved to a daily `arbitrage_results_YYYY-MM-DD.csv` file for later analysis.
*   **Dual-Window Management**: Efficiently manages and switches between browser tabs for Betfair and TAB to perform concurrent scraping tasks.

## How It Works

1.  **Initialization**: The bot launches an undetected Chrome browser and opens two tabs: one for Betfair and one for TAB.
2.  **Navigation**: It navigates to the "Next to Go" or "Today's Racing" sections on both websites.
3.  **Market Iteration**: The bot loops through the available race markets on Betfair.
4.  **Data Extraction**: For each market, it extracts the horse names and their corresponding "Win" and "Place/Top 3" odds from both Betfair and TAB.
5.  **Comparison**: It normalizes the horse names and compares the odds. If a profitable arbitrage scenario is found (after accounting for Betfair's commission), it flags it.
6.  **Notification & Logging**: For every arbitrage opportunity found, it sends a detailed Pushover notification and appends the data to a CSV log file.
7.  **Loop & Repeat**: The process repeats, refreshing the markets periodically to catch the latest odds.

## Technologies Used

*   **Language**: C# (.NET)
*   **Web Automation**: Selenium WebDriver
*   **Browser Driver**: SeleniumUndetectedChromeDriver
*   **Notifications**: Pushover API

---

## Getting Started

Follow these instructions to get a copy of the project up and running on your local machine.

### Prerequisites

1.  **.NET 6.0 SDK** or later.
2.  **Google Chrome** browser installed.
3.  **ChromeDriver**: You must download the ChromeDriver executable that matches your installed version of Google Chrome.
    *   Download from: Chrome for Testing downloads

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/ScrapeNextToGo.git
    cd ScrapeNextToGo
    ```

2.  **Configure ChromeDriver Path:**
    Open the `Program.cs` file and update the `ChromeDriverPath` variable to point to the location where you saved `chromedriver.exe`.
    ```csharp
    // in Program.cs
    const string ChromeDriverPath = @"C:\path\to\your\chromedriver.exe";
    ```

3.  **Configure Pushover API Keys:**
    Open the `PushoverAPI.cs` file and replace the placeholder values with your own Pushover User Key and Application Token.
    ```csharp
    // in PushoverAPI.cs
    private readonly string _userKey = "YOUR_PUSHOVER_USER_KEY";
    private readonly string _applicationToken = "YOUR_PUSHOVER_APP_TOKEN";
    ```

4.  **Restore Dependencies:**
    Run the following command in your terminal to install the required NuGet packages (Selenium, etc.).
    ```bash
    dotnet restore
    ```

### Running the Application

Once the setup is complete, you can run the bot from your terminal:

```bash
dotnet run
```

The application will start, open a Chrome window, and begin the scraping process. All activities, findings, and errors will be logged in the console.

## License

This project is open-source. Feel free to fork, modify, and distribute it. Please see the `LICENSE` file for more details.

## Disclaimer

This tool is for educational and research purposes only. Betting involves high risk. The creator is not responsible for any financial losses incurred from using this software. Ensure you comply with the terms of service of the websites you are scraping.
