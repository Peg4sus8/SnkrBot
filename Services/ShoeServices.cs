using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;
using SnkrBot.Models;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace SnkrBot.Services
{
    public class ShoeService
    {
        private readonly string connectionString;

        public ShoeService(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");

        }

        // Da modificare facendo query a db SELECT *
        public async Task<List<Shoe>> GetAllShoesAsync()
        {
            List<Shoe> shoes = new List<Shoe>();
            
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

            return shoes;
        }

        // Da modificare facendo chiamata in base a se filtrare in base a BRAND o PREZZO oppure TUTTE
        public async Task<List<Shoe>> GetFilteredShoesAsync(string brand = null, double? price = null)
        {
            if (!string.IsNullOrEmpty(brand))
                return await GetShoesByBrandAsync(brand);
            else if (!price.HasValue)
                return await GetShoesByPriceAsync((double)price);
            else
                return await GetAllShoesAsync();
        }
        // GetShoesByBrand
        public async Task<List<Shoe>> GetShoesByBrandAsync(string brand)
        {
            List<Shoe> shoes = new List<Shoe>();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();
                Console.WriteLine("Connessione al DB avvenuta");

                string query = "SELECT name, image_url, release, price FROM dbo.Snkr WHERE name LIKE '@Filter%'";

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

            return shoes;
        }

        // GetShoesByPrice
        public async Task<List<Shoe>> GetShoesByPriceAsync(double price)
        {
            List<Shoe> shoes = new List<Shoe>();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync();
                Console.WriteLine("Connessione al DB avvenuta");

                string query = "SELECT name, image_url, release, price FROM dbo.Snkr WHERE price < '@Filter'";

                using (SqlCommand command = new SqlCommand(query, sqlConnection))
                {
                    command.Parameters.AddWithValue("@Filter", $"%{price}%");
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

            return shoes;
        }
    }
}