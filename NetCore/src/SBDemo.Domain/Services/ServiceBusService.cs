using Microsoft.Azure.ServiceBus;
using SBDemo.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBDemo.Domain.Services
{
    public class ServiceBusService
    {
        public static async Task SendMessageAsync(string sb, string topicName, OnlineTransaction transaction)
        {
            var topicClient = new TopicClient(sb, topicName);
            
            // Serialize and send messages
            var json = EncodingService.EncodeJson(transaction);

            // Note: Simulate custom serialization
            var message = new Message(EncodingService.Base64Encode(json));

            // Add custom properties
            // Note: To be used in filters
            // Note: Filtering by SO and Invoice
            message.UserProperties.Add("TransactionType", transaction.Type.ToString());
            message.UserProperties.Add("Version", "1.0");

            // Send message
            await topicClient.SendAsync(message);

        }
    }
}
