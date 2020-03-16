using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using SBDemo.Domain.Models;
using SBDemo.Domain.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SBDemo.HttpService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private IConfiguration Configuration;
        private string connStr;
        private string topicName;
        private string sbConnectionString;
        ITopicClient topicClient;

        public OrdersController(IConfiguration configuration)
        {
            Configuration = configuration;
            connStr = Configuration["DbConnectionString"];
            // Note 1: change from sb to https
            sbConnectionString = Configuration["SBConnectionString"];
            topicName = Configuration["SBTopic"];
            topicClient = new TopicClient(sbConnectionString, topicName);
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OnlineTransaction>>> Get()
        {
            return Ok(await GetOrdersAsync(null));
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OnlineTransaction>> Get(int id)
        {
            var list = await GetOrdersAsync(id);
            if (list is null || list.Count() == 0)
            {
                return BadRequest();
            }
            else
            {
                return Ok(list.FirstOrDefault(c => c.Id == id));
            }
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] OnlineTransaction transaction)
        {
            //Save order to database and marked as processed
            if (string.IsNullOrEmpty(transaction.CustomerId))
            {
                return BadRequest("Customer information missing");
            }
            // Note: Set Default Values
            transaction.No = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            transaction.CreatedTimeUTC = DateTime.UtcNow;
            transaction.Processed = false;
            transaction.Type = TransactionType.SO;

            // Add custom Random item and calculate total
            var rnd = new Random(Environment.TickCount);
            var items = rnd.Next(1, 10);
            var listItems = new List<Item>(items);
            var total = 0.0m;
            for (var i = 0; i < items; i++)
            {
                var qty = rnd.Next(1, 10);
                var price = (decimal)(10 + 90 * rnd.NextDouble());
                listItems.Add(new Item { Id = i + 1, Code = ((i + 1) * 100).ToString(), Qty = qty, Price = price });
                total += qty * price;
            }
            transaction.Total = total;
            transaction.Items = listItems.ToArray();

            // Note: Order received and transmitted to ServiceBus
            await SendMessageAsync(transaction);
            return Ok(transaction);
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private async Task SendMessageAsync(OnlineTransaction transaction)
        {
            try
            {
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
            catch (Exception exception)
            {
                Debug.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }


        private async Task<IEnumerable<OnlineTransaction>> GetOrdersAsync(int? id)
        {
            var list = new List<OnlineTransaction>();
            using (var con = new SqlConnection(connStr))
            {
                await con.OpenAsync();

                var sqlCmd = "select * from [OnlineTransaction] where datediff(s, ProcessedTimeUTC,GETUTCDATE()) <= @Seconds";

                if (id.HasValue)
                    sqlCmd += " and Id=@Id";

                using (var cmd = new SqlCommand(sqlCmd, con))
                {
                    cmd.Parameters.AddWithValue("@Seconds", 30);
                    if (id.HasValue)
                        cmd.Parameters.AddWithValue("@Id", id.Value);


                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var transaction = new OnlineTransaction
                            {
                                Id = (int)reader[0],
                                Type = (TransactionType)Enum.Parse(typeof(TransactionType), reader[1].ToString()),
                                No = reader[2].ToString(),
                                CreatedTimeUTC = (DateTimeOffset)reader[3],
                                CustomerId = reader[4].ToString(),
                                Total = (decimal)reader[5],
                                ProcessedTimeUTC = ColToOffset(reader[6]),
                                Processed = (bool)reader[7],
                                // reader[8].toString() -> json
                                Version =reader[9].ToString(),
                            };
                            list.Add(transaction);
                        }
                    }
                }
            }
            return list;
        }

        private DateTimeOffset? ColToOffset(object obj)
        {
            if (obj is null || obj.GetType() == typeof(DBNull))
                return null;
            else
                return (DateTimeOffset)obj;
        }
    }
}

