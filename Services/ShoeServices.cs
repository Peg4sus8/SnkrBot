﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using SnkrBot.Models;

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

        public async Task<List<Shoe>> GetFilteredShoesAsync(string brand = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var shoes = await GetShoesAsync();
            return FilterShoes(shoes, brand, minPrice, maxPrice);
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

        private List<Shoe> FilterShoes(List<Shoe> shoes, string brand, decimal? minPrice, decimal? maxPrice)
        {
            // Converti la lista in IQueryable per operazioni LINQ
            IEnumerable<Shoe> query = shoes;

            // Filtra per brand
            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(s => s.Name.Contains(brand, StringComparison.OrdinalIgnoreCase));
            }

            // Filtra per prezzo minimo
            if (minPrice.HasValue)
            {
                query = query.Where(s =>
                {
                    var price = ParsePrice(s.Price);
                    return price > 0 && price >= minPrice.Value;
                });
            }

            // Filtra per prezzo massimo
            if (maxPrice.HasValue)
            {
                query = query.Where(s =>
                {
                    var price = ParsePrice(s.Price);
                    return price > 0 && price <= maxPrice.Value;
                });
            }

            return query.ToList();
        }

        private decimal ParsePrice(string price)
        {
            if (string.IsNullOrEmpty(price) || price == "-" || price == "N/A")
                return 0;

            // Rimuovi tutto tranne numeri, punti e virgole
            string numericPrice = new string(price.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());

            // Gestisci sia punti che virgole come separatori decimali
            numericPrice = numericPrice.Replace(",", ".");

            if (decimal.TryParse(numericPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;

            return 0;
        }

    }

}