using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            foreach (Process p in Process.GetProcessesByName("firefox"))
            {
                p.Kill();
            }

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
                    double oldSpeed = speed;
                    double oldMaxSpeed = maxSpeed;
                    double oldSeconds = seconds;
                    double oldFastest = fastest == double.MaxValue ? 0 : fastest;
                    string gameCode = string.Empty;
                    DateTime start = DateTime.Now;
                    DateTime end = DateTime.Now;
                    Game game = ParseXml(ReadXml(ref gameCode, ref oldCount, ref newCount, ref end), gameCode);
                    if (game != null)
                    {
                        TimeSpan diff = end - start;
                        seconds = diff.TotalSeconds;
                        fastest = Math.Min(seconds, fastest);
                        speed = (oldCount + newCount) / diff.TotalSeconds;
                        maxSpeed = Math.Max(maxSpeed, speed);
                        Console.WriteLine("---------------------");
                        Console.WriteLine("Adding game: " + game.Code);
                        Save(game);
                        Console.WriteLine("Speed: " + string.Format("{0:0.00}", speed) + " lps (" +
                                          string.Format("{0:+#.00;-#.00;0}", speed - oldSpeed) + ")");
                        Console.WriteLine("MaxSpeed: " + string.Format("{0:0.00}", maxSpeed) + " lps (" +
                                          string.Format("{0:+#.00;-#.00;0}", maxSpeed - oldMaxSpeed) + ")");
                        Console.WriteLine("Time taken: " + string.Format("{0:0.00}", seconds) + "s (" +
                                          string.Format("{0:+#.00;-#.00;0}", seconds - oldSeconds) + ")");
                        Console.WriteLine("Fastest time: " + string.Format("{0:0.00}", fastest) + "s (" +
                                          string.Format("{0:+#.00;-#.00;0}", fastest - oldFastest) + ")");
                        Console.WriteLine("---------------------");
                    }
                } while (4 * fastest > seconds || 5 * speed > maxSpeed);
                mainDriver.Quit();
            } while (true);
        }

        private static FirefoxDriver StartDriver(string url, string linkText, bool click)
        {
            FirefoxDriver driver = null;
            if (!click && replayDriver != null)
            {
                driver = replayDriver;
            }
            else
            {
                FirefoxOptions ffo = new FirefoxOptions();
                ffo.AddArguments(new[] {
                    "--headless",
                    "--private" });
                ffo.SetPreference("pageLoadStrategy", "eager");
                while (driver == null) {
                    try
                    {
                        driver = new FirefoxDriver(ffo);
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            driver.Url = url;
            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            IWebElement success = null;
            int i = 0;
            do
            {
                try
                {
                    if (i > 0) {
                        driver.Navigate().GoToUrl(url);
                    }
                    i++;
                    success = wait.Until(ExpectedConditions.ElementExists(By.LinkText(linkText)));
                }
                catch (Exception)
                {
                    
                }
            } while (success == null && i < 3);
            if (success == null)
            {
                driver.Quit();
                return null;
            }
            if (click)
            {
                success.Click();
            }
            return driver;
        }

        private static void Save(Game game)
        {
            Game.GameCodes.Add(game.Code);
            HeroClass winner = game.First.IsWinner ? game.First.HeroClass : game.Second.HeroClass;
            HeroClass loser = game.First.IsWinner ? game.Second.HeroClass : game.First.HeroClass;
            Console.WriteLine(winner + " beats " + loser);
            int oldPairsCount = CardPair.CardPairs.Count;
            List<Stats> stats = new List<Stats>();
            foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {
                if (hero == winner || hero == loser)
                {
                    if (stats.Find(x => x.hero == hero) == null)
                    {
                        stats.Add(new Stats(hero));
                    }
                }
            }
            Console.WriteLine("---------------------");
            CardPair.AddGamePairs(game);
            Console.WriteLine("Adding " + game.CardPairs.Count + " new card pairs.");
            CardPair.SavePairs();
            Console.WriteLine("Saving " + 
                               string.Format("{0:n0}", CardPair.CardPairs.Count) + 
                               " (" + string.Format("{0:+#;-#;0}", CardPair.CardPairs.Count - oldPairsCount) + ")" + 
                               " total card pairs.");
            Game.SaveGameCodes();
            Console.WriteLine("Games saved: " + String.Format("{0:n0}",  Game.GameCodes.Count));
            double percentage = 100.0 * Game.GameCodes.Count / CardPair.CardPairs.Count;
            Console.WriteLine("Games/Pairs = " +
                              string.Format("{0:0.00}", percentage) +
                              "% (" + string.Format("{0:+#.00;-#.00;0}", 
                              percentage - 100.0 * (Game.GameCodes.Count - 1) / oldPairsCount) + ")");
            Console.WriteLine("---------------------");
            foreach (HeroClass hero in Enum.GetValues(typeof(HeroClass)))
            {
                if (hero == HeroClass.NONE)
                {
                    continue;
                }
                Stats st = stats.Find(x => x.hero == hero);
                if (st == null)
                {
                    PrintPairs(hero);
                }
                else
                {
                    PrintPairs(hero, st.pairs, st.wins, st.loses);
                }
            }
            Console.WriteLine("---------------------");
        }

        private static void PrintPairs(HeroClass hero, int oldPairs = 0, int oldWins = 0, int oldLoses = 0)
        {
            var pairs = CardPair.CardPairs.FindAll(x => x.Hero == hero);
            int wins = 0;
            int loses = 0;
            foreach(var pair in pairs)
            {
                wins += pair.Wins;
                loses += pair.Losses;
            }
            if (oldPairs == 0)
            {
                Console.WriteLine(hero + ": " +
                    string.Format("{0:n0}", pairs.Count) + " -> " +
                    string.Format("{0:n0}", (wins + loses)) +
                    " : " +
                    (wins + loses > 0 ? string.Format("{0:0.00}", 100.0 * wins / (wins + loses)) : "0") +
                    "%");
            }
            else
            {
                Console.WriteLine(hero + ": " +
                    string.Format("{0:n0}", pairs.Count) +
                    " (" + string.Format("{0:+#;-#;0}", pairs.Count - oldPairs) + ")" + 
                    " -> " +
                    string.Format("{0:n0}", (wins + loses)) +
                    " (" + string.Format("{0:+#;-#;0}", wins + loses - oldWins - oldLoses) + ")" +
                    " : " +
                    (wins + loses > 0 ? string.Format("{0:0.00}", 100.0 * wins / (wins + loses)) : "0") +
                    "% (" + string.Format("{0:+#.00;-#.00;0}", 100.0 * wins / (wins + loses) - 100.0 * oldWins / (oldWins + oldLoses)) + ")");
            }
        }

        private static Game ParseXml(string xml, string gameCode)
        {
            if (xml == null || xml == "")
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

        private static string ReadXml(ref String gameCode, ref int oldCount, ref int newCount, ref DateTime end)
        {
            string link = string.Empty;
            int counter = 0;
            oldCount = 0;
            newCount = 0;
            do
            {
                string oldCode = gameCode;
                if (counter++ > 10000)
                {
                    try
                    {
                        mainDriver.Navigate().Refresh();
                    }
                    catch (Exception)
                    {
                        mainDriver = StartDriver("https://hsreplay.net/", "Full speed", true);
                    }
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

            end = DateTime.Now;
            Console.WriteLine();
            replayDriver = StartDriver(link, "Download Replay XML", false);
            if (replayDriver == null)
            {
                return "";
            }
            IWebElement xmlLink = new WebDriverWait(replayDriver, TimeSpan.FromSeconds(100))
                .Until(ExpectedConditions.ElementToBeClickable(By.LinkText("Download Replay XML")));
            string href = xmlLink.GetAttribute("href");

            replayDriver.Url = "about:blank";

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
