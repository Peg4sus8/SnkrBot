using Azure.Core;
using System;

namespace SnkrBot.Models
{
    public class EventDetails
    {
        public string EventId { get; set; } // Identificativo univoco dell'evento
        public string Name { get; set; } // Nome o titolo dell'evento
        public DateTime ReleaseDate { get; set; } // Data di rilascio o inizio evento
    }
}