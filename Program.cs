using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
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
            mainDriver = StartDriver("https://hsreplay.net/", "Full speed", true);
            while (true)
            {
                string gameCode = String.Empty;
                Game game = ParseXml(ReadXml(ref gameCode), gameCode);
                Console.WriteLine("Adding game: " + game.Code);
                Save(game);
            }
        }

        private static FirefoxDriver StartDriver(string url, string linkText, bool click)
        {
            FirefoxDriver driver = null;
            if (replayDriver == null)
            {
                FirefoxOptions ffo = new FirefoxOptions();
                ffo.AddArgument("--headless");
                ffo.SetPreference("pageLoadStrategy", "eager");
                driver = new FirefoxDriver(ffo);
            }
            else
            {
                driver = replayDriver;
            }
            driver.Url = url;

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
            IWebElement success = null;
            do
            {
                try
                {
                    driver.Navigate().Refresh();
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
        }

        private static Game ParseXml(string xml, string gameCode)
        {
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

        private static string ReadXml(ref String gameCode)
        {
            string link = string.Empty;
            do
            {
                Console.Write(".");
                try 
                {
                    link = mainDriver.FindElementByClassName("replay-feed-item").GetAttribute("href");
                    gameCode = link.Split('/')[4];
                }
                catch (StaleElementReferenceException e)
                {
                    continue;
                }
            } while (link == "" || Game.ContainsGame(gameCode));

            Console.WriteLine();
            replayDriver = StartDriver(link, "Download Replay XML", false);
            IWebElement xmlLink = new WebDriverWait(replayDriver, TimeSpan.FromSeconds(10))
                .Until(ExpectedConditions.ElementToBeClickable(By.LinkText("Download Replay XML")));
            string href = xmlLink.GetAttribute("href");

            Stream input = WebRequest.Create(href).GetResponse().GetResponseStream();
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
            Console.WriteLine(sb.ToString());
            return sb.ToString();
        }

        private static void LoadSaved()
        {
            CardPair.LoadPairs();
            Game.LoadGameCodes();
        }
    }
}
