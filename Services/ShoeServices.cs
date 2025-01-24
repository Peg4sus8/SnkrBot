using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SnkrBot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace SnkrBot.Services
{
    public class ShoeService
    {
        private readonly string connectionString;
        private readonly ILogger<ShoeService> _logger;

        public ShoeService(IConfiguration configuration, ILogger<ShoeService> logger)
        {
            connectionString = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
            _logger = logger;   
        }


        // Da modificare facendo query a db SELECT *
        public async Task<List<Shoe>> GetAllShoesAsync()
        {
            List<Shoe> shoes = new List<Shoe>();
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    await sqlConnection.OpenAsync();
                    Console.WriteLine("Connessione al DB avvenuta");

                    string query = "SELECT name, image_url, release, price FROM dbo.Snkr";
                
                    using (SqlCommand command = new SqlCommand(query, sqlConnection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Shoe record = new Shoe
                                {
                                    Name = reader.GetString(0), 
                                    Img = reader.GetString(1),
                                    Release = reader.GetString(2),
                                    Price = reader.GetString(3)
                                };
                                shoes.Add(record);
                            }
                        }

                        Console.WriteLine($"--- Query executed successfully. Rows affected {shoes.Count()}");
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError($"SQL error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"General error: {ex.Message}");
                throw;
            }
            return shoes;
        }

        // Da modificare facendo chiamata in base a se filtrare in base a BRAND o PREZZO oppure TUTTE
        public async Task<List<Shoe>> GetFilteredShoesAsync(string brand = null, double? price = null)
        {
            if (!string.IsNullOrEmpty(brand))
                return await GetShoesByBrandAsync(brand);
            else if (price.HasValue)
                return await GetShoesByPriceAsync((double)price);
            else
                return await GetAllShoesAsync();
        }
        // GetShoesByBrand
        public async Task<List<Shoe>> GetShoesByBrandAsync(string brand)
        {
            List<Shoe> shoes = new List<Shoe>();
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    await sqlConnection.OpenAsync();
                    Console.WriteLine("Connessione al DB avvenuta");

                    string query = "SELECT name, image_url, release, price FROM dbo.Snkr WHERE name LIKE @Filter";

                    using (SqlCommand command = new SqlCommand(query, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@Filter", $"%{brand}%");
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Shoe record = new Shoe
                                {
                                    Name = reader.GetString(0),
                                    Img = reader.GetString(1),
                                    Release = reader.GetString(2),
                                    Price = reader.GetString(3)
                                };
                                shoes.Add(record);
                            }
                        }

                        Console.WriteLine($"--- Query executed successfully. Rows affected {shoes.Count()}");
                    }
                }
            }
            catch (SqlException ex) 
            { 
                _logger.LogError($"SQL error: {ex.Message}"); 
                throw; 
            }
            catch (Exception ex) {
                _logger.LogError($"General error: {ex.Message}"); 
                throw; 
            }

            return shoes;
        }

        // GetShoesByPrice
        public async Task<List<Shoe>> GetShoesByPriceAsync(double price)
        {
            List<Shoe> shoes = new List<Shoe>();
            try
            { 
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    await sqlConnection.OpenAsync();
                    Console.WriteLine("Connessione al DB avvenuta");

                    string query = "SELECT name, image_url, release, price FROM dbo.Snkr WHERE price <= @Filter";

                    using (SqlCommand command = new SqlCommand(query, sqlConnection))
                    {
                        command.Parameters.AddWithValue("@Filter", $"{price}");
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Shoe record = new Shoe
                                {
                                    Name = reader.GetString(0),
                                    Img = reader.GetString(1),
                                    Release = reader.GetString(2),
                                    Price = reader.GetString(3)
                                };
                                shoes.Add(record);
                            }
                        }

                        Console.WriteLine($"--- Query executed successfully. Rows affected {shoes.Count()}");
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError($"SQL error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"General error: {ex.Message}");
                throw;
            }
            return shoes;
        }
    }
}