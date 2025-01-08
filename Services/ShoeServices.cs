using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using SnkrBot.Models;
using System.Text.RegularExpressions;

namespace SnkrBot.Services
{
    public class ShoeService
    {
        private readonly string _csvPath;
        private List<Shoe> _cachedShoes;
        private DateTime _lastUpdate;
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1); // Aggiorna ogni ora

        public ShoeService(string csvPath = "shoes.csv")
        {
            _csvPath = csvPath;
            _cachedShoes = new List<Shoe>();
        }

        public async Task<List<Shoe>> GetShoesAsync()
        {
            if (NeedsUpdate())
            {
                await UpdateShoesDataAsync();
            }
            return _cachedShoes;
        }

        private bool NeedsUpdate()
        {
            return _cachedShoes.Count == 0 || DateTime.Now - _lastUpdate > _updateInterval;
        }

        private async Task UpdateShoesDataAsync()
        {
            try
            {
                // Esegui lo scraping solo se necessario
                var links = ScrapingUtility.GetLinks("https://heat-mvmnt.de/releases");
                var shoes = ScrapingUtility.GetCards(links);

                // Salva i dati nel CSV
                await SaveToCsvAsync(shoes);

                // Aggiorna la cache
                _cachedShoes = shoes.ToList();
                _lastUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                // Se lo scraping fallisce, prova a caricare dal CSV
                _cachedShoes = await LoadFromCsvAsync();
                if (!_cachedShoes.Any())
                {
                    throw new Exception($"Error updating shoes data and no backup data available: {ex.Message}");
                }
            }
        }

        private async Task SaveToCsvAsync(IEnumerable<Shoe> shoes)
        {
            using var writer = new StreamWriter(_csvPath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            await csv.WriteRecordsAsync(shoes);
        }

        private async Task<List<Shoe>> LoadFromCsvAsync()
        {
            if (!File.Exists(_csvPath))
                return new List<Shoe>();

            using var reader = new StreamReader(_csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            // Leggi i record in modo sincrono ma wrappa in Task per mantenere l'interfaccia asincrona
            return await Task.FromResult(csv.GetRecords<Shoe>().ToList());
        }

        public async Task<List<Shoe>> GetFilteredShoesAsync(string brand = null, double? maxPrice = null)
        {
           var shoes = await GetShoesAsync();

            return shoes.Where(shoe => string.IsNullOrEmpty(brand) || shoe.Name.ToLower().Contains(brand.ToLower())) // Filtra per brand
                .Where(shoe => !maxPrice.HasValue || (ParsePrice(shoe.Price) is double shoePrice && shoePrice <= maxPrice)
                    ).ToList();
        }
        
        double? ParsePrice(string price)
        {
            if (string.IsNullOrWhiteSpace(price)) return null;

            // Rimuovi tutti i caratteri non numerici (incluso €)
            var cleanedPrice = Regex.Replace(price, @"[^\d.,]", "").Trim();

            // Converti il prezzo, gestendo virgole come separatori decimali
            return double.TryParse(cleanedPrice.Replace(",", "."), out var result) ? result : null;
        }

    }

}