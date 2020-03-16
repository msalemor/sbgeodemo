using SBDemo.Domain.Models;
using System.Data.SqlClient;

namespace SBDemo.Worker
{
    namespace CoreReceiverApp
    {
        using Microsoft.Azure.ServiceBus;
        using Microsoft.Azure.ServiceBus.Core;
        using SBDemo.Domain.Services;
        using System;
        using System.Diagnostics;
        using System.Threading;
        using System.Threading.Tasks;
        using System.Transactions;

        class Program
        {
            const string ServiceBusConnectionString = "Endpoint=sb://alemor-ns-alias.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=RM80la7v4YGYZnkB9Im+xslxuyO7lIv+jV1SCJdmwYo=";
            const string TopicName = "transaction-topic";
            const string SOSubscription = "so-subscription";
            const string InvoiceSubscription = "invoice-subscription";
            private static string connStr = "Data Source=alemor-azcp-db.database.windows.net;Initial Catalog=telemetry;User ID=dbadmin;Password=Fuerte#123456";
            private static ITopicClient topicClient;
            private static ISubscriptionClient poSubscriptionClient;
            private static ISubscriptionClient invoiceSubscriptionClient;

            public static void Main(string[] args)
            {
                MainAsync().Wait();
            }

            public static async Task MainAsync()
            {
                topicClient = new TopicClient(ServiceBusConnectionString, TopicName);

                poSubscriptionClient = new SubscriptionClient(ServiceBusConnectionString, TopicName, SOSubscription);
                invoiceSubscriptionClient = new SubscriptionClient(ServiceBusConnectionString, TopicName, InvoiceSubscription);
                
                // Register subscription message handler and receive messages in a loop
                RegisterOnMessageHandlerAndReceiveMessages();

                Console.WriteLine("======================================================");
                Console.WriteLine("Press ENTER key to exit after receiving all the messages.");
                Console.WriteLine("======================================================");
                Console.ReadKey();

                // Close the subscriptions
                await poSubscriptionClient.CloseAsync();
                await invoiceSubscriptionClient.CloseAsync();
            }

            static void RegisterOnMessageHandlerAndReceiveMessages()
            {
                // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
                var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
                {
                    // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                    // Set it according to how many messages the application wants to process in parallel.
                    MaxConcurrentCalls = 10,

                    // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                    // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                    AutoComplete = false
                };

                // Process SOs
                poSubscriptionClient.RegisterMessageHandler(ProcessSOMessageAsync, messageHandlerOptions);

                // Configure the message handler options in terms of exception handling, number of concurrent messages to deliver, etc.
                var messageHandlerOptions1 = new MessageHandlerOptions(ExceptionReceivedHandler)
                {
                    // Maximum number of concurrent calls to the callback ProcessMessagesAsync(), set to 1 for simplicity.
                    // Set it according to how many messages the application wants to process in parallel.
                    MaxConcurrentCalls = 1,

                    // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                    // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                    AutoComplete = false
                };
                // Process Invoices
                invoiceSubscriptionClient.RegisterMessageHandler(ProcessInvoiceMessagesAsync, messageHandlerOptions1);
            }

            static async Task ProcessSOMessageAsync(Message message, CancellationToken token)
            {
                var json = EncodingService.Base64Decode(message.Body);
                var trans = EncodingService.DecodeJson<OnlineTransaction>(json);

                // Process the message.
                //Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{json}");
                Console.WriteLine(DateTime.Now.ToString() + ": SO Received");

                // Persist the message to SQL
                trans.ProcessedTimeUTC = DateTime.UtcNow;
                trans.Processed = true;
                await SaveTransactionAsync(trans, json);

                // Create an Invoce message from the orginal SO message
                var invoice = EncodingService.DeepClone<OnlineTransaction>(trans);
                invoice.Processed = false;
                invoice.ProcessedTimeUTC = null;
                invoice.Type = TransactionType.Invoice;

                json = EncodingService.EncodeJson(invoice);

                // Note: Simulate custom serialization
                var invoiceMessage = new Message(EncodingService.Base64Encode(json));

                // Add custom properties
                // Note: To be used in filters
                // Note: Filtering by SO and Invoice
                invoiceMessage.UserProperties.Add("TransactionType", invoice.Type.ToString());
                invoiceMessage.UserProperties.Add("Version", "1.0");

                // Send the invoice message
                await topicClient.SendAsync(invoiceMessage);

                await poSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
            }

            static async Task ProcessInvoiceMessagesAsync(Message message, CancellationToken token)
            {
                var json = EncodingService.Base64Decode(message.Body);
                var trans = EncodingService.DecodeJson<OnlineTransaction>(json);

                // Random delay of at least a second
                var rnd = new Random(Environment.TickCount);
                await Task.Delay((int)(1000 + 1000 * rnd.NextDouble()));

                // Process the message.
                //Console.WriteLine($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{json}");
                Console.WriteLine(DateTime.Now.ToString() + ": Invoice Received");

                // Process the invoice
                trans.ProcessedTimeUTC = DateTime.UtcNow;
                trans.Processed = true;
                // Persist to SQL
                await SaveTransactionAsync(trans, json);

                // Complete the message so that it is not received again.
                // This can be done only if the subscriptionClient is created in ReceiveMode.PeekLock mode (which is the default).
                await invoiceSubscriptionClient.CompleteAsync(message.SystemProperties.LockToken);

                // Note: Use the cancellationToken passed as necessary to determine if the subscriptionClient has already been closed.
                // If subscriptionClient has already been closed, you can choose to not call CompleteAsync() or AbandonAsync() etc.
                // to avoid unnecessary exceptions.
            }

            static Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
            {
                Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
                var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
                Console.WriteLine("Exception context for troubleshooting:");
                Console.WriteLine($"- Endpoint: {context.Endpoint}");
                Console.WriteLine($"- Entity Path: {context.EntityPath}");
                Console.WriteLine($"- Executing Action: {context.Action}");
                return Task.CompletedTask;
            }

            private static async Task SaveTransactionAsync(OnlineTransaction transaction, string json)
            {
                try
                {
                    using (var con = new SqlConnection(connStr))
                    {
                        await con.OpenAsync();
                        using (var cmd = new SqlCommand("insert into OnlineTransaction values (@Type,@No,@CreatedTimeUTC,@CustomerId,@Total,@ProcessedTimeUTC,@Processed,@Json,DEFAULT)", con))
                        {
                            cmd.Parameters.AddWithValue("@Type", transaction.Type);
                            cmd.Parameters.AddWithValue("@No", transaction.No);
                            cmd.Parameters.AddWithValue("@CreatedTimeUTC", transaction.CreatedTimeUTC);
                            cmd.Parameters.AddWithValue("@CustomerId", transaction.CustomerId);
                            cmd.Parameters.AddWithValue("@Total", transaction.Total);
                            if (transaction.ProcessedTimeUTC.HasValue)
                                cmd.Parameters.AddWithValue("@ProcessedTimeUTC", transaction.ProcessedTimeUTC.Value);
                            else
                                cmd.Parameters.AddWithValue("@ProcessedTimeUTC", DBNull.Value);
                            cmd.Parameters.AddWithValue("@Processed", transaction.Processed);
                            if (string.IsNullOrEmpty(json))
                                cmd.Parameters.AddWithValue("@Json", DBNull.Value);
                            else
                                cmd.Parameters.AddWithValue("@Json", json);
                            //int modified = (int)(await cmd.ExecuteScalarAsync());
                            await cmd.ExecuteNonQueryAsync();
                            //transaction.Id = modified;
                        }
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw;
                }
                //return transaction;
            }
        }
    }
}
