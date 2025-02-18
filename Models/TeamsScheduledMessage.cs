using Azure.Core;
using System;

namespace SnkrBot.Models
{
    public class TeamsScheduledMessage
    {
        public string ServiceUrl { get; set; }
        public string ConversationId { get; set; }
        public string MessageText { get; set; }
        public DateTimeOffset ScheduledTime { get; set; }
    }
}