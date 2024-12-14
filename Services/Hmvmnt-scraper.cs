using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Threading;
using CsvHelper;
using System.Globalization;
using System.IO;

using SnkrBot.Models;

namespace SnkrBot.Services
{

    public static class ScrapingUtility
    {

        public static List<string> GetLinks(string url)
        {
            // Configura le opzioni di Chrome 
            var options = new ChromeOptions();
            options.AddArgument("--headless"); // Rimuovi questa riga per vedere il browser

            using IWebDriver driver = new ChromeDriver(options);

            // Naviga alla pagina
            driver.Navigate().GoToUrl("https://heat-mvmnt.de/releases");

            // Attendi che la pagina venga caricata completamente
            Thread.Sleep(5000); //operazione necessaria per poter prendere tutti prodotti dato che vengono caricati dinamicamente con JS

            // Trova i div con la classe specificata
            var divs = driver.FindElements(By.CssSelector("div.mb-5.sm\\:mb-2"));

            Console.WriteLine($"Trovati {divs.Count} div con classe 'mb-5 sm:mb-2'.");
            var links = new List<string>();

            foreach (var div in divs)
            {
                // Trova i tag <a> con una classe specifica all'interno del div per prendere il link
                var anchors = div.FindElements(By.XPath(".//a[contains(@class, 'rte-ignore') and contains(@class, 'group') and contains(@class, 'block')]"));
                foreach (var anchor in anchors)
                {
                    // Recupera il link (href)
                    string href = anchor.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        links.Add(href);
                    }
                }
            }

            Console.WriteLine($"Trovati {links.Count} link.");
            driver.Quit();
            return links;
        }

        // Funzione che permette di creare la card di ogni scarpa
        public static List<Shoe> GetCards(List<string> urls)
        {
            var scarpeList = new List<Shoe>();
            Console.WriteLine($"Ho ricevuto {urls.Count} links.");

            foreach (var url in urls)
            {
                HtmlDocument doc = GetDocument(url);
                string nameXpath = "/html/body/div[1]/main/div[3]/h1/text()";
                string imageXpath = "/html/body/div[1]/main/div[3]/div[1]/div[1]/div/div[1]/div/div/div/picture/source[1]";
                string releaseXpath = "/html/body/div[1]/main/div[3]/div[3]/div[1]/div[2]";
                string priceXpath = "/html/body/div[1]/main/div[3]/div[3]/div[2]/div/div[1]/div[2]/div/text()";
                
                // Grazie agli xPath si va a prendere la stringa di cui abbiamo bisogno e la si aggiusta con Trim
                var shoe = new Shoe();
                shoe.Name = doc.DocumentNode.SelectSingleNode(nameXpath)?.InnerText.Trim().Replace("&quot;", "\'");
                shoe.Release = doc.DocumentNode.SelectSingleNode(releaseXpath)?.InnerText.Trim();
                shoe.Img = doc.DocumentNode.SelectSingleNode(imageXpath).GetAttributeValue("srcset", "-");
                shoe.Price = doc.DocumentNode.SelectSingleNode(priceXpath)?.InnerText.Trim(); 

                //Per alcuni prodotti non c'è il prezzo, quindi riporta un valore numerico senza il segno dell'euro.
                //se troviamo una stringa del genere inseriamo il -
                shoe.Price = (shoe.Price.IndexOf("€") > -1) ? shoe.Price : "-";

                scarpeList.Add(shoe);
            }

            return scarpeList;
        }

        static HtmlDocument GetDocument(string url)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);
            return doc;
        }

        public static void ExportToCsv(List<Shoe> cards)
        {
            using (var writer = new StreamWriter("./sneakers.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(cards);
            }
        }
    }
}
