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
using SeleniumExtras.WaitHelpers; // Ensure you have this NuGet package installed


namespace Scrape
{
    public static class BetfairNavigate
    {
        private static readonly Random Random = new Random();

        public static void RandomSleep(int minMilliseconds, int maxMilliseconds)
        {
            int sleepTime = Random.Next(minMilliseconds, maxMilliseconds);
            Thread.Sleep(sleepTime);
            // Console.WriteLine($"😴 Sleeping for {sleepTime}ms..."); // Optional: log the sleep time
        }

        public static Dictionary<string, decimal> ExtractHorseDataBetfair(IWebDriver driver, WebDriverWait wait, string label)
        {
            var betfairLayOdds = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            IWebElement totalMatchedElement;
            const decimal MinMatchedVolume = 0m; // Define the minimum required matched volume in dollars

            if (driver.FindElements(By.CssSelector("div.suspended-overlay-container.suspended")).Count > 0)
            {
                return new Dictionary<string, decimal>();
            }


            try
            {
                totalMatchedElement = driver.FindElement(By.CssSelector("span.total-matched"));
            }
            catch
            {
                Console.WriteLine($"couldnt find betfair 'span.total-matched' element");
                return betfairLayOdds;
            }

            try
            {
                // 1. Wait for the total matched volume element to be present and visible


                // 2. Extract and parse the total matched volume
                string matchedText = totalMatchedElement.Text.Trim();
                // Remove currency symbols (AUD) and any thousands separators (commas) for parsing
                string numberPart = matchedText.Replace("AUD", "", StringComparison.OrdinalIgnoreCase).Replace(",", "").Trim();

                decimal totalMatchedVolume;

                // Use InvariantCulture to ensure consistent parsing of the decimal
                if (decimal.TryParse(numberPart, NumberStyles.Number, CultureInfo.InvariantCulture, out totalMatchedVolume))
                {
                    // 3. Check if the volume is above the threshold
                    if (totalMatchedVolume < MinMatchedVolume)
                    {
                        Console.WriteLine($"🚫 Matched volume ({totalMatchedVolume:C} {label}) is below minimum threshold of {MinMatchedVolume:C}. Skipping data extraction.");
                        return new Dictionary<string, decimal>(); // Return empty dict to signal skip
                    }
                    // else: Volume is sufficient, continue with data extraction
                }

                // --- Original Horse Data Extraction Logic Starts Here ---

                wait.Until(d => d.FindElements(By.CssSelector("tr.runner-line")).Count > 0);
                // Assuming Program.RandomSleep is available
                // Program.RandomSleep(500, 1000); 

                var runners = driver.FindElements(By.CssSelector("tr.runner-line:not(.removed-runner)"));
                Console.WriteLine($"📊 Found {runners.Count} runners in {label}");



                // ... (Rest of the original loop for extracting horse names and lay odds)
                foreach (var runner in runners)
                {
                    try
                    {
                        var nameElem = runner.FindElement(By.CssSelector("h3.runner-name"));
                        string horseName = nameElem.Text.Trim();
                        decimal layOdds = -1;
                        try
                        {
                            // This selector might need adjusting for the new Betfair site if Zs3u5 changes.
                            var layElem = runner.FindElement(By.CssSelector("td.lay-cell.first-lay-cell label.Zs3u5"));
                            decimal.TryParse(layElem.Text.Trim(), out layOdds);
                        }
                        catch (NoSuchElementException)
                        {
                            Console.WriteLine($"couldnt find element for lay odds for {horseName}");
                        }

                        // Assuming NormalizeName is available and correctly implemented
                        betfairLayOdds[NormalizeName(horseName)] = layOdds;
                        Console.WriteLine($"🐎 Horse: {horseName} | Lay Odds: {layOdds}");
                    }
                    catch (NoSuchElementException)
                    {
                        Console.WriteLine($"⚠️ Could not extract data for a runner in {label}. Skipping this runner.");
                        continue;
                    }
                }
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"⚠️ Necessary element (runners or total matched) not found or page took too long to load for {label}");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine($"⚠️ Total matched element not found for {label}. Aborting extraction.");
                return betfairLayOdds;
            }

            return betfairLayOdds;
        }
        public static int closePopup(WebDriverWait wait, IWebDriver driver)
        {

            // Make the wait lambda resilient: don't allow driver.FindElement to throw out of the Until poll.
            IWebElement closeButton;
            try
            {
                closeButton = driver.FindElement(By.CssSelector("div.tw-rounded-full.tw-cursor-pointer.tw-bg-black"));
                closeButton.Click();
                return 1;
            }
            catch (Exception)
            {
                Console.WriteLine("Couldnt find the close button");
            }
            return 0;
        }
        public static void selectTodaysRacing(IWebDriver driver, WebDriverWait wait)
        {
            try
            {
                // ✅ 2. Click the Today's Races link
                RandomSleep(1000, 1500);
                IWebElement todaysRacesLink = wait.Until(d =>
                {
                    while (true)
                    {
                        try
                        {
                            return driver.FindElement(By.CssSelector("a.navigation-link.TODAYS_CARD[href='en/horse-racing-betting-7/next']"));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Couldnt find ausLink.");
                            driver.Navigate().Refresh();
                            RandomSleep(2000, 2500);
                        }
                    }
                });
                todaysRacesLink.Click();
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine("ℹ️ No Today's Races link found — continuing.");
            }
        }
        public static int getMarkets(IWebDriver driver, WebDriverWait wait)
        {
            try
            {
                return driver.FindElements(By.CssSelector("a[class*='navigation-link toggle-node']:not([class*='Disabled'])")).Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Couldnt find any Markets: {ex.Message}");
                return 0;
            }
        }
        public static IWebElement selectMarket(IWebDriver driver, WebDriverWait wait, int index)
        {
            var marketLinkLocator = By.CssSelector("a[class*='navigation-link toggle-node']:not([class*='Disabled'])");
            wait.Until(ExpectedConditions.PresenceOfAllElementsLocatedBy(marketLinkLocator));
            IReadOnlyCollection<IWebElement> marketLinks = driver.FindElements(marketLinkLocator);


            if (index >= marketLinks.Count)
            {
                Console.WriteLine("✅ Finished clicking all markets. No more markets to process.");
                throw new IndexOutOfRangeException("End of market list reached.");
            }


            IWebElement market = marketLinks.ElementAt(index);
            string marketName = market.Text.Trim();
            Console.WriteLine($"👉 Clicking market {index + 1} of {marketLinks.Count}: {marketName}");

            try
            {
                wait.Until(ExpectedConditions.ElementToBeClickable(market));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", market);

                market.Click();

                return market; // Return the clicked element
            }
            catch (StaleElementReferenceException)
            {

                Console.WriteLine("⚠️ StaleElementReferenceException caught. Market list changed, signaling caller to retry current index.");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ An error occurred while trying to click market: {ex.Message}");
                throw;
            }
        }
        public static int selectTop3Market(IWebDriver driver, WebDriverWait wait)
        {
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
            int found = 0;
            try
            {
                var top3 = driver.FindElement(By.CssSelector("a[title='Top 3 Finish']"));
                top3.Click();
                found = 1;
            }
            catch(Exception)
            {
                Console.WriteLine($"Couldnt find top 3");
                found = 0;
            }
            
            return found;
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