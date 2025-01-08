using System;

namespace SnkrBot.Models
{
    public class FilterDetails
    {
        public string Brand { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}