using System;
using System.Collections.Generic;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumUndetectedChromeDriver;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Scrape
{
    public static class Program
    {
        // Static Random object is better for performance and thread-safety (though not strictly required for a simple script)
        private static readonly Random Random = new Random();
        private static int maxTime = 4000;
        private static int minTime = 1000;

        public static void RandomSleep(int minMilliseconds, int maxMilliseconds)
        {
            int sleepTime = Random.Next(minMilliseconds, maxMilliseconds);
            Thread.Sleep(sleepTime);
            // Console.WriteLine($"😴 Sleeping for {sleepTime}ms..."); // Optional: log the sleep time
        }

        static async Task Main(string[] args)
        {
            await bootMessage();
            while(true)
            {
                const string ChromeDriverPath = @"C:\SeleniumDrivers\chromedriver.exe"; // <-- Make sure this is the correct path!
                var options = new ChromeOptions();
                options.AddArgument("--window-size=1920,1080");
                //options.AddArgument("--headless=new");
                //options.AddArgument("--user-agent='Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36'");
                options.AddArgument("--disable-gpu");
                using (IWebDriver driver = UndetectedChromeDriver.Create(
                    options: options,
                    driverExecutablePath: ChromeDriverPath, // The program fails here if the file is missing
                    hideCommandPromptWindow: false
                ))
                // ---------------------------------------------
                {
                    try
                    {
                        Console.WriteLine("🚀 Initializing Undetected ChromeDriver...");
                        driver.Navigate().GoToUrl("https://www.betfair.com.au/exchange/plus/en/horse-racing-betting-7");

                        // Store the handle of the ORIGINAL window immediately
                        string betfairWindowHandle = driver.CurrentWindowHandle;
                        string TABWindowHandle = "";
                        Console.WriteLine($"Original Betfair Window Handle: {betfairWindowHandle}");

                        RandomSleep(2000, 2500);

                        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

                        string baseUrl = "https://www.tab.com.au";

                        // --- MODIFIED: Use driver.SwitchTo().NewWindow(WindowType.Tab) ---
                        Console.WriteLine("✅ Opening new tab using Selenium API...");

                        // 1. Create a new tab and automatically switch focus to it.

                        if (driver.SwitchTo().NewWindow(WindowType.Tab).ToString() != null)
                        {
                            TABWindowHandle = driver.CurrentWindowHandle;
                        }
                        else
                        {
                            throw new Exception("Failed to create and switch to new TAB window.");
                        }

                        // 2. Navigate to the desired URL in the newly created tab.
                        driver.Navigate().GoToUrl(baseUrl);

                        RandomSleep(3000, 3500);


                        IWebElement ausLink = wait.Until(d =>
                        {
                            while (true)
                            {
                                try
                                {
                                    return driver.FindElement(By.CssSelector("div[class='toggle-menu-link']"));
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Couldnt find ausLink.");
                                    driver.Navigate().Refresh();
                                    RandomSleep(2000, 2500);
                                }
                            }
                        });
                        ausLink.Click();
                        RandomSleep(1000, 1500);

                        driver.FindElement(By.CssSelector("div[data-id='racing']")).Click();

                        RandomSleep(1000, 1500);

                        driver.FindElement(By.CssSelector("a[data-testid='next-to-go']")).Click();



                        Console.WriteLine($"Successfully navigated new TAB window to: {baseUrl}");
                        // ------------------------------------------------------------------

                        RandomSleep(2000, 2500); // Wait for the TAB page to load before switching back

                        // Now switch back to Betfair to continue the original flow
                        driver.SwitchTo().Window(betfairWindowHandle);
                        Console.WriteLine("Switched back to Betfair window.");
                        // -------------------------------------------------
                        RandomSleep(2000, 2500);
                        BetfairNavigate.closePopup(wait, driver);
                        RandomSleep(2000, 2500); // Allow browser a moment to render the page
                        BetfairNavigate.selectTodaysRacing(driver, wait);
                        //((ITakesScreenshot)driver).GetScreenshot().SaveAsFile("clickedTodaysRacing.png");
                        RandomSleep(3000, 3500);
                        //((ITakesScreenshot)driver).GetScreenshot().SaveAsFile("waited3Seconds.png");
                        Console.WriteLine("number of markets found: " + BetfairNavigate.getMarkets(driver, wait));
                        RandomSleep(1000, 1500);
                        string marketName;
                        while (true)
                        {
                            driver.SwitchTo().Window(betfairWindowHandle);
                            RandomSleep(minTime, maxTime);
                            driver.Navigate().Refresh();
                            RandomSleep(2000, 2500);
                            int Found = 0;
                            int attempts = 0;
                            while (Found != 1 && attempts < 3)
                            {
                                RandomSleep(1000, 1100);
                                Found = BetfairNavigate.closePopup(wait, driver);
                                attempts++;
                            }
                            

                            for (int i = 0; i < 6; i++)
                            {
                                driver.SwitchTo().Window(betfairWindowHandle);

                                IWebElement market;

                                RandomSleep(minTime, maxTime);
                                try
                                {
                                    market = BetfairNavigate.selectMarket(driver, wait, i);
                                    marketName = market.Text.Trim();
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    Console.WriteLine("✅ All markets processed or couldnt access the market. Exiting loop.");
                                    break; // Exit the loop when all markets have been processed
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Couldnt run BetfairNavigate.selectMarket().");
                                    break;
                                }
                                RandomSleep(minTime, maxTime);

                                //Match match = Regex.Match(marketName, @"R\d+");
                                //((ITakesScreenshot)driver).GetScreenshot().SaveAsFile("screenshot" + i + ".png");

                                var betfairOdds = BetfairNavigate.ExtractHorseDataBetfair(driver, wait, marketName);
                                RandomSleep(minTime, maxTime);

                                driver.SwitchTo().Window(TABWindowHandle);
                                RandomSleep(minTime, maxTime);
                                int foundMatchingMarket = 0;
                                try
                                {
                                    foundMatchingMarket = TABNavigate.selectMarket(driver, wait, marketName/*, match.Value*/);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("✅ All markets processed or couldnt access the market. Exiting loop.");
                                    driver.Navigate().Refresh();
                                    break;
                                }

                                if (foundMatchingMarket == 1)
                                {
                                    RandomSleep(minTime, maxTime);
                                    var tabOdds = TABNavigate.ExtractHorseDataTAB(driver, marketName/*, match.Value*/);
                                    RandomSleep(minTime, maxTime);
                                    int matchingHorseNames = await CompareAndWriteOdds("Win", marketName, tabOdds, betfairOdds, "arbitrage_results.csv");

                                    //If win odds comparison has matching horse names, compare top3/place odds
                                    if (matchingHorseNames == 1)
                                    {
                                        driver.SwitchTo().Window(betfairWindowHandle);
                                        RandomSleep(minTime, maxTime);
                                        if (BetfairNavigate.selectTop3Market(driver, wait) == 1)
                                        {
                                            RandomSleep(minTime, maxTime);
                                            var betfairOddsTop3 = BetfairNavigate.ExtractHorseDataBetfair(driver, wait, marketName);
                                            RandomSleep(minTime, maxTime);
                                            driver.SwitchTo().Window(TABWindowHandle);
                                            var tabOddsPlace = TABNavigate.ExtractHorseDataTABPlace(driver, marketName/*, match.Value*/);
                                            string dateString = DateTime.Now.ToString("yyyy-MM-dd");
                                            string filename = $"arbitrage_results{dateString}.csv";
                                            await CompareAndWriteOdds("Place", marketName, tabOddsPlace, betfairOddsTop3, filename);
                                            RandomSleep(minTime, maxTime);

                                        }
                                        driver.SwitchTo().Window(TABWindowHandle);
                                    }
                                    driver.Navigate().Back();
                                }
                            }
                            Console.WriteLine($"\n--- sleeping for 60 seconds... ---");
                            RandomSleep(60000, 61000);
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"\n--- Crashed, restarting... ---");
                        await resetNotify();
                        driver.Quit();
                        RandomSleep(10000, 10500);
                    }
                }
            }
        }
        public static async Task<int> CompareAndWriteOdds(string type, string marketName, Dictionary<string, decimal> tabWinOdds, Dictionary<string, decimal> betfairLayOdds, string outputFile)
        {
            // 1. Print the total count of input data
            Console.WriteLine($"\n--- Starting Comparison Process ---");
            Console.WriteLine($"TAB {type} Odds Horses Count: {tabWinOdds.Count}");
            Console.WriteLine($"Betfair {type} Lay Odds Horses Count: {betfairLayOdds.Count}");
            Console.WriteLine($"Output File: {outputFile}");
            Console.WriteLine($"-----------------------------------");

            int matchingHorseNames = 0;
            var lines = new List<string>();
            lines.Add("type, market Name, Horse,tab Win Back Odds,Betfair Win Lay Odds,backStake,layStake, profit(%)");

            // 2. Iterate through each horse in tabWinOdds
            foreach (var kvp in tabWinOdds)
            {
                var horse = kvp.Key;
                var tabOdds = kvp.Value;

                // Print the current horse and its TAB odds
                Console.WriteLine($"\n[INFO] Processing Horse: {horse}, TAB {type} Odds: {tabOdds}");

                // 3. Check if the horse exists in betfairLayOdds
                if (betfairLayOdds.TryGetValue(horse, out var layOdds))
                {
                    matchingHorseNames = 1;
                    // Print the matching Betfair Lay Odds
                    Console.WriteLine($"[INFO] Found match. Betfair {type} Lay Odds: {layOdds}");

                    // The core comparison logic
                    if (layOdds > 0 && tabOdds > 0 && layOdds < tabOdds)
                    {
                        // 4. Print a CLEAR message when an arbitrage is found
                        var effectiveLayOdds = layOdds - (layOdds - 1) * 0.05m; // Assuming 5% commission
                        var backStake = 20;
                        var layStake = (tabOdds * backStake) / effectiveLayOdds;
                        layStake = Math.Round(layStake, 2);
                        var percentageProfit = CalculateArbitragePercentageProfit(tabOdds, layOdds, 5);
                        string csvLine = $"{type}, {marketName}, {horse},{tabOdds},{layOdds}, ${backStake}, ${layStake}, {percentageProfit}%";
                        string notifyLine = $"type: {type}  |  market:  {marketName}  |  horse:  {horse}  |  TAB:  {tabOdds}  |  BF:  {layOdds}  |  BackStake:  ${backStake}  |  laystake: ${layStake}  |  Profit(%):  {percentageProfit}%";
                        lines.Add(csvLine);
                        await arbOddsNotify(notifyLine);
                        Console.WriteLine($"\n[✨ ARBITRAGE FOUND! 🤑] {horse}: TAB {type} {tabOdds} > Betfair {type} Lay {layOdds}");
                        Console.WriteLine($"[ACTION] Added to lines list.");
                    }
                    else
                    {
                        // 5. Print why a match was NOT considered an arbitrage (optional, but very helpful)
                        Console.WriteLine($"[INFO] No arbitrage. Comparison for {type}: Lay ({layOdds}) < Tab ({tabOdds}) is FALSE.");
                    }
                }
                else
                {
                    // 6. Print when a horse is missing from the Betfair data
                    Console.WriteLine($"[WARN] Horse '{horse}' from TAB data not found in Betfair {type} Lay Odds.");
                }
            }

            // 7. Print the final result summary
            Console.WriteLine($"\n--- Comparison Complete ---");
            Console.WriteLine($"Total Arbitrage Lines Found (including header): {lines.Count}");

            if (lines.Count > 1)
            {
                // Print the lines being written (preview)
                Console.WriteLine($"[ACTION] Writing {lines.Count - 1} arbitrage opportunities to {outputFile}:");
                foreach (var line in lines.Skip(1)) // Skip the header for the preview
                {
                    Console.WriteLine($"[PREVIEW] {line}");
                }

                System.IO.File.AppendAllLines(outputFile, lines);
                Console.WriteLine($"[SUCCESS] Results appended to {outputFile}.");
            }
            else
            {
                // 8. Print a message if no arbitrage was found
                Console.WriteLine("[INFO] No arbitrage opportunities found.");
            }
            Console.WriteLine($"-----------------------------------\n");
            return matchingHorseNames;
        }

        public static decimal CalculateArbitragePercentageProfit(decimal backOdds, decimal layOdds, decimal commissionPercentage)
        {
            // The type has been changed to decimal for financial precision.
            const decimal backStake = 100.0m;     // Reference stake for the Back bet (the percentage is independent of this value)
            decimal commissionRate = commissionPercentage / 100.0m;

            // Basic validation: Odds must be greater than 1.0
            if (backOdds <= 1.0m || layOdds <= 1.0m)
            {
                return 0.0m;
            }
            decimal optimalLayStake = (backOdds * backStake) / (layOdds - commissionRate);
            decimal guaranteedProfit = (optimalLayStake * (1.0m - commissionRate)) - backStake;
            decimal layLiability = optimalLayStake * (layOdds - 1.0m);
            decimal totalStaked = backStake + layLiability;
            decimal percentageProfit = (guaranteedProfit / totalStaked) * 100.0m;

            return Math.Round(percentageProfit, 2);
        }

        public static async Task bootMessage()
        {
            var notifier = new PushoverClient();

            // 2. Define your notification content
            string notificationTitle = "Booting Arb Scraper";
            string notificationMessage = $"Starting Betfair & Tab scrape operation: {DateTime.Now}";

            Console.WriteLine($"Attempting to send notification: '{notificationTitle}'");

            // 3. Call the async method using await
            bool success = await notifier.SendNotification(notificationTitle, notificationMessage);

            if (success)
            {
                Console.WriteLine("Notification delivered.");
            }
            else
            {
                Console.WriteLine("Notification delivery failed.");
            }
        }
        public static async Task arbOddsNotify(string arb)
        {
            var notifier = new PushoverClient();

            // 2. Define your notification content
            string notificationTitle = "Arbitrage Found";
            string notificationMessage = $"{arb}";

            Console.WriteLine($"Attempting to send notification: '{notificationTitle}'");

            // 3. Call the async method using await
            bool success = await notifier.SendNotification(notificationTitle, notificationMessage);

            if (success)
            {
                Console.WriteLine("Notification delivered.");
            }
            else
            {
                Console.WriteLine("Notification delivery failed.");
            }
        }
        public static async Task resetNotify()
        {
            var notifier = new PushoverClient();
        
            // 2. Define your notification content
            string notificationTitle = "Arbitrage bot Resetting";
            string notificationMessage = $"Arbitrage bot Resetting: {DateTime.Now}";
            
            Console.WriteLine($"Attempting to send notification: '{notificationTitle}'");

            // 3. Call the async method using await
            bool success = await notifier.SendNotification(notificationTitle, notificationMessage);

            if (success)
            {
                Console.WriteLine("Notification delivered.");
            }
            else
            {
                Console.WriteLine("Notification delivery failed.");
            }
        }
    }
}