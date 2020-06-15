using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace PairReader
{
    class Program
    {
        static FirefoxDriver mainDriver = null;
        static FirefoxDriver replayDriver = null;
        static void Main()
        {
            Cards.Load();
            LoadSaved();
            
            do
            {
                mainDriver = StartDriver("https://hsreplay.net/", "Full speed", true);
                int oldCount = 0;
                int newCount = 0;
                double speed = 0;
                double maxSpeed = 0;
                double seconds = 0;
                double fastest = double.MaxValue;
                do
                {
                    string gameCode = String.Empty;
                    DateTime start = DateTime.Now;
                    Game game = ParseXml(ReadXml(ref gameCode, ref oldCount, ref newCount), gameCode);
                    if (game != null)
                    {
                        TimeSpan diff = DateTime.Now - start;
                        seconds = diff.TotalSeconds;
                        fastest = Math.Min(seconds, fastest);
                        speed = (oldCount + newCount) / diff.TotalSeconds;
                        maxSpeed = Math.Max(maxSpeed, speed);
                        Console.WriteLine("Adding game: " + game.Code);
                        Save(game);
                        Console.WriteLine("Speed: " + string.Format("{0:0.00}", speed) +
                                          ". MaxSpeed: " + string.Format("{0:0.00}", maxSpeed) +
                                          ". Time taken: " + string.Format("{0:0.00}", seconds) + "s" +
                                          ". Fastest time: " + string.Format("{0:0.00}", fastest) + "s");
                    }
                } while (5 * fastest > seconds || 5 * speed > maxSpeed);
                mainDriver.Quit();
            } while (true);
        }

        private static FirefoxDriver StartDriver(string url, string linkText, bool click)
        {
            FirefoxDriver driver;
            if (!click && replayDriver != null)
            {
                driver = replayDriver;
            }
            else
            {
                FirefoxOptions ffo = new FirefoxOptions();
                ffo.AddArguments(new[] { "--headless", "--private" });
                ffo.SetPreference("pageLoadStrategy", "eager");
                driver = new FirefoxDriver(ffo);
            }

            driver.Url = url;
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            IWebElement success = null;
            bool firstAttempt = true;
            do
            {
                try
                {
                    if (!firstAttempt) {
                        driver.Navigate().Refresh();
                    }
                    firstAttempt = false;
                    success = wait.Until(ExpectedConditions.ElementExists(By.LinkText(linkText)));
                }
                catch (Exception)
                {
                    
                }
            } while (success == null);
            if (click)
            {
                success.Click();
            }
            return driver;
        }

        private static void Save(Game game)
        {
            Game.GameCodes.Add(game.Code);
            string winner = game.First.IsWinner ? game.First.HeroClass.ToString() : game.Second.HeroClass.ToString();
            string loser = game.First.IsWinner ? game.Second.HeroClass.ToString() : game.First.HeroClass.ToString();
            Console.WriteLine(winner + " beats " + loser);
            CardPair.AddGamePairs(game);
            Console.WriteLine("Adding " + game.CardPairs.Count + " new card pairs.");
            CardPair.SavePairs();
            Console.WriteLine("Saving " + CardPair.CardPairs.Count + " total card pairs.");
            Game.SaveGameCodes();
            Console.WriteLine("Games saved: " + Game.GameCodes.Count);
            Console.WriteLine("Games/Pairs = " +
                string.Format("{0:0.00}", 100.0 * Game.GameCodes.Count / CardPair.CardPairs.Count) +
                "%");
        }

        private static Game ParseXml(string xml, string gameCode)
        {
            if (xml == null)
            {
                return null;
            }
            string gameXml = xml.Split('\n')[2];
            Game game = new Game
            {
                Code = gameCode
            };
            XElement main = XElement.Parse(gameXml);
            XElement gameElement = main.Element("Game");
            if (!CheckStd(gameElement))
            {
                Console.WriteLine("Non-standard game: " + gameCode);
                return null;
            }
            XElement firstPlayer = gameElement.Element("Player");
            Player.AddPlayers(firstPlayer, game);
            foreach(XElement e in gameElement.Descendants()){
                switch (e.Name.ToString())
                {
                    case "Tag":
                        HandleTag(e, game);
                        break;
                    case "TagChange":
                        HandleTagChange(e, game);
                        break;
                    case "FullEntity":
                    case "ShowEntity":
                        CardEntity.CreateCardEntity(e, game);
                        break;
                }
            }
            game.Summarize();
            return game;
        }

        private static void HandleTagChange(XElement e, Game game)
        {
            string tagId = e.Attribute("tag").Value;
            if (tagId == "49")
            {
                game.RegisterCard(e.Attribute("entity").Value, e.Attribute("value").Value);
            }
            if (tagId == "20")
            {
                game.Turn = int.Parse(e.Attribute("value").Value);
            }
            if (tagId == "17")
            {
                game.RegisterWinner(e);
            }
        }

        private static void HandleTag(XElement e, Game game)
        {
            if (e.Attribute("tag").Value != "49")
            {
                return;
            }
            game.RegisterCard(e.Parent.FirstAttribute.Value, e.Attribute("value").Value);
        }

        private static bool CheckStd(XElement gameElement)
        {
            return gameElement.Attribute("type").Value == "7" &&
                    gameElement.Attribute("format").Value == "2" &&
                    gameElement.Attribute("scenarioID").Value == "2";
        }

        private static string ReadXml(ref String gameCode, ref int oldCount, ref int newCount)
        {
            string link = string.Empty;
            int counter = 0;
            oldCount = 0;
            newCount = 0;
            do
            {
                string oldCode = gameCode;
                if (counter++ == 10000)
                {
                    mainDriver.Navigate().Refresh();
                    new WebDriverWait(mainDriver, TimeSpan.FromSeconds(10))
                        .Until(ExpectedConditions.ElementExists(By.LinkText("Full speed")))
                        .Click();
                    Console.WriteLine("Refresh");
                    counter = 0;
                }
                try 
                {
                    var elements = mainDriver.FindElementsByClassName("replay-feed-item");
                    link = elements[elements.Count - 1].GetAttribute("href");
                    gameCode = link.Split('/')[4];
                    if (gameCode == oldCode)
                    {
                        Console.Write("o");
                        oldCount++;
                        continue;
                    }
                }
                catch (Exception)
                {
                    Console.Write("e");
                    continue;
                }
                Console.Write(".");
                newCount++;
            } while (gameCode == "" || Game.ContainsGame(gameCode));

            Console.WriteLine();
            replayDriver = StartDriver(link, "Download Replay XML", false);
            IWebElement xmlLink = new WebDriverWait(replayDriver, TimeSpan.FromSeconds(10))
                .Until(ExpectedConditions.ElementToBeClickable(By.LinkText("Download Replay XML")));
            string href = xmlLink.GetAttribute("href");
            Stream input;
            try
            {
                input = WebRequest.Create(href).GetResponse().GetResponseStream();
            }
            catch(WebException)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            using (var zip = new GZipStream(input, CompressionMode.Decompress, true))
            {
                byte[] b = new byte[1048576];
                while (true)
                {
                    int count = zip.Read(b, 0, b.Length);
                    if (count != 0)
                        sb.Append(Encoding.Default.GetString(b));
                    if (count != b.Length)
                        break;
                }
            }
            input.Close();
            return sb.ToString();
        }

        private static void LoadSaved()
        {
            CardPair.LoadPairs();
            Game.LoadGameCodes();
        }
    }
}
