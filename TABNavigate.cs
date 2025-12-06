using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumUndetectedChromeDriver;


namespace Scrape
{
    public static class TABNavigate
    {
        // Static Random object is better for performance and thread-safety (though not strictly required for a simple script)
        private static readonly Random Random = new Random();

        public static void RandomSleep(int minMilliseconds, int maxMilliseconds)
        {
            int sleepTime = Random.Next(minMilliseconds, maxMilliseconds);
            Thread.Sleep(sleepTime);
            // Console.WriteLine($"😴 Sleeping for {sleepTime}ms..."); // Optional: log the sleep time
        }


        public static int selectMarket(IWebDriver driver, WebDriverWait wait, string marketName/*, string raceId*/)
        {
            string ContainerCssSelector = "div._1d28rxt";
            string RaceLinkCssSelector = "[data-testid='next-to-go-page-close-to-jump-race'], [data-testid='next-to-go-page-race']";
            string MeetingNameCssSelector = "[data-testid='next-to-go-page-meeting-name']";

            IList<IWebElement> allContainers = driver.FindElements(By.CssSelector(ContainerCssSelector));

            // 2. Select only the containers at index 0 and index 2
            List<IWebElement> targetContainers = new List<IWebElement>();

            // Target Index 0 (The first group of races)
            if (allContainers.Count > 0)
            {
                System.Console.WriteLine("Found and targeting container at index 0.");
                targetContainers.Add(allContainers[0]);
            }

            // Target Index 2 (The third group of races, if it exists)
            if (allContainers.Count > 2)
            {
                System.Console.WriteLine("Found and targeting container at index 2.");
                targetContainers.Add(allContainers[2]);
            }
            else
            {
                System.Console.WriteLine("Note: Container at index 2 not found. Only using index 0.");
            }

            List<string> meetingNames = new List<string>();

            // 3. Iterate through the target containers and extract race links
            foreach (IWebElement container in targetContainers)
            {
                // Find all race links inside the current container using the CSS selector
                IList<IWebElement> raceLinks = container.FindElements(By.CssSelector(RaceLinkCssSelector));

                // 4. Extract the meeting name from each race link
                foreach (IWebElement link in raceLinks)
                {
                    // Find the meeting name element nested inside the link
                    IWebElement nameElement = link.FindElement(By.CssSelector(MeetingNameCssSelector));
                    string name = nameElement.Text.Trim();
                    marketName = NormalizeName(marketName);
                    name = NormalizeName(name);
                    Console.WriteLine($"comparing {marketName} and {name}");
                    if (marketName == name)
                    {
                        nameElement.Click();
                        Console.WriteLine($"✅ Clicked on TAB market: {marketName}");
                        return 1; // Success;
                    }
                }
            }
            return 0;
        }


        public static Dictionary<string, decimal> ExtractHorseDataTAB(IWebDriver driver, string meetingName/*, string raceNum*/)
        {
            var tabPlaceOdds = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // Explicitly set wait for element checks
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            // Temporarily reduce implicit waits for quick element checks
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
            try
            {
                IReadOnlyCollection<IWebElement> placeExists = driver.FindElements(By.CssSelector("div[ng-if*='raceRunners.showFixedOddsWin']"));

                if (placeExists.Count == 0)
                {
                    // If the element indicating fixed place odds is not found, we assume they are unavailable.
                    // This is safer than waiting for 10s and getting an exception.
                    Console.WriteLine($"No Fixed WIN Odds available for this race ({meetingName})");
                    // Restore implicit wait before exiting
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3); // Restore to a reasonable default
                    return new Dictionary<string, decimal>();
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Cant find WIN Odds: div[ng-if*='raceRunners.showFixedOddsWin']");
                return new Dictionary<string, decimal>();
            }


            // --- Wait for Runner Data to Load ---
            // Wait for at least one runner row to appear before scraping
            Console.WriteLine($"TAB waiting for runners to appear...");
            try
            {
                wait.Until(d => d.FindElements(By.CssSelector("div.row[data-testid^='runner-number-']")).Count > 0);
            }
            catch (Exception)
            {
                Console.WriteLine($"Couldn't find runners in TAB: div.row[data-testid^='runner-number-']");
                return new Dictionary<string, decimal>();
            }


            // --- Extract Runner Data ---
            var runnerRows = driver.FindElements(By.CssSelector("div.row[data-testid^='runner-number-']"));
            Console.WriteLine($"TAB found {runnerRows.Count} runners for race {/*raceNum*/meetingName}:");

            foreach (var row in runnerRows)
            {
                string runnerName = "(No Name Found)";
                decimal placeOdds = -1;

                // Find name element (FindElements returns empty list if not found, no exception)
                var nameElems = row.FindElements(By.CssSelector(".runner-name"));
                if (nameElems.Count > 0)
                {
                    runnerName = nameElems.First().Text.Trim();
                }

                // Find place odds element
                var placeElems = row.FindElements(By.CssSelector("div[data-id*='fixed-odds-price']"));
                if (placeElems.Count > 0)
                {
                    // Attempt to parse text. If Parse fails, placeOdds remains the default value (0 if decimal.TryParse is used 
                    // but since we initialize it to -1, we'll use TryParse to update it).
                    decimal.TryParse(placeElems.First().Text.Trim(), out placeOdds);
                }

                // Only add to dictionary if a name was found (or at least normalize the name)
                if (runnerName != "(No Name Found)")
                {
                    tabPlaceOdds[NormalizeName(runnerName)] = placeOdds;
                    Console.WriteLine($"  {runnerName} | WIN: {placeOdds}");
                }
            }

            return tabPlaceOdds;
        }
        
        public static Dictionary<string, decimal> ExtractHorseDataTABPlace(IWebDriver driver, string meetingName/*, string raceNum*/)
        {
            var tabPlaceOdds = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            // Explicitly set wait for element checks
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            // Temporarily reduce implicit waits for quick element checks
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);
            try
            {
                IReadOnlyCollection<IWebElement> placeExists = driver.FindElements(By.CssSelector("div[ng-if*='raceRunners.showFixedOddsPlace']"));

                if (placeExists.Count == 0)
                {
                    // If the element indicating fixed place odds is not found, we assume they are unavailable.
                    // This is safer than waiting for 10s and getting an exception.
                    Console.WriteLine($"No Fixed place Odds available for this race ({meetingName})");
                    // Restore implicit wait before exiting
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3); // Restore to a reasonable default
                    return new Dictionary<string, decimal>();
                }
            }
            catch(Exception)
            {
                Console.WriteLine($"Cant find place Odds: div[ng-if*='raceRunners.showFixedOddsPlace']");
            }


            // --- Wait for Runner Data to Load ---
            // Wait for at least one runner row to appear before scraping
            Console.WriteLine($"TAB waiting for runners to appear...");
            try
            {
                wait.Until(d => d.FindElements(By.CssSelector("div.row[data-testid^='runner-number-']")).Count > 0);
            }
            catch(Exception)
            {
                Console.WriteLine($"Couldn't find runners in TAB: div.row[data-testid^='runner-number-']");
                return new Dictionary<string, decimal>();
            }
            

            // --- Extract Runner Data ---
            var runnerRows = driver.FindElements(By.CssSelector("div.row[data-testid^='runner-number-']"));
            Console.WriteLine($"TAB found {runnerRows.Count} runners for race {/*raceNum*/meetingName}:");

            foreach (var row in runnerRows)
            {
                string runnerName = "(No Name Found)";
                decimal placeOdds = -1;

                // Find name element (FindElements returns empty list if not found, no exception)
                var nameElems = row.FindElements(By.CssSelector(".runner-name"));
                if (nameElems.Count > 0)
                {
                    runnerName = nameElems.First().Text.Trim();
                }

                // Find place odds element
                var placeElems = row.FindElements(By.CssSelector("div[ng-if*='raceRunners.showFixedOddsPlace']"));
                if (placeElems.Count > 0)
                {
                    // Attempt to parse text. If Parse fails, placeOdds remains the default value (0 if decimal.TryParse is used 
                    // but since we initialize it to -1, we'll use TryParse to update it).
                    decimal.TryParse(placeElems.First().Text.Trim(), out placeOdds);
                }

                // Only add to dictionary if a name was found (or at least normalize the name)
                if (runnerName != "(No Name Found)")
                {
                    tabPlaceOdds[NormalizeName(runnerName)] = placeOdds;
                    Console.WriteLine($"  {runnerName} | Place: {placeOdds}");
                }
            }

            return tabPlaceOdds;
        }
        private static string NormalizeName(string input)
        {
            // 1. REMOVE ALL CHARACTERS AFTER THE OPEN PARENTHESIS '('
            int parenIndex = input.IndexOf('(');
            if (parenIndex >= 0)
            {
                input = input.Substring(0, parenIndex);
            }
            
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            
            // 2. Convert to lowercase and trim whitespace from the ends
            string s = input.Trim().ToLowerInvariant(); 
            var normalizedFormD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            
            foreach (var ch in normalizedFormD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            
            s = sb.ToString().Normalize(NormalizationForm.FormC);
            
            // Remove characters that are not lowercase letters or spaces (including numbers and punctuation)
            s = Regex.Replace(s, "[^a-z ]+", "");
            
            // Collapse multiple spaces into a single space and trim
            s = Regex.Replace(s, "\\s+", " ").Trim();
            
            //Console.WriteLine($"Normalized input '{input.Trim()}' to result '{s}'");
            return s;
        }
    }
}