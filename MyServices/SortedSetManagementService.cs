using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyServices
{
    public class SortedSetManagementService
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ISubscriber _subscriber;
        private readonly ILogger<SortedSetManagementService> _logger;
        private readonly string _channel;
        private readonly string _sortedSetKey;

        public SortedSetManagementService(IConfiguration configuration, ILogger<SortedSetManagementService> logger)
        {
            _sortedSetKey = "SortedSetDemoItems";

            // connect to redis
            _multiplexer = ConnectionMultiplexer.Connect(configuration["REDIS"]);

            // set a private field to a string to identify what type of messages this service should publish and subscribe to in redis
            _channel = "blazor:SortedSetManagementDemo";

            // set up logging see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-5.0
            _logger = logger;

            // this obtains a pub-sub connection to a redis server. see above _multiplexer
            _subscriber = _multiplexer.GetSubscriber();

            // create an event handler in-line for what happens when the channel we are subscribed to receives a message
            // to send messages to this channel, use:
            // publish 'blazor:SortedSetManagementDemo' 'hello'
            _subscriber.Subscribe(_channel, (channel, message) => {
                _logger.LogInformation("Received on {channel} channel: {notification} ", (string)channel, (string)message);
                SortedSetManagementDemoNotification?.Invoke();
                if (message.ToString().EndsWith("added"))
                {
                    SortedSetAdditionTriggered?.Invoke((message.ToString().Split(' ').First()));
                }
            });
        }

        /// <summary>
        /// Get all items in a stack
        /// </summary>
        /// <returns>A list of strings, could get expensive growing the list over and over if many items in redis</returns>
        public List<string> GetTopTenSetItems()
        {
            List<string> results = new List<string>();
            int pos = 0;
            foreach (var item in _multiplexer.GetDatabase().SortedSetRangeByRankWithScores(_sortedSetKey, 0, 9, Order.Ascending))
            {
                pos++;
                results.Add($"{item} at position {pos}, score {item.Score}");
            }
            return results;
        }

        /// <summary>
        /// Check if the key exists.
        /// </summary>
        public bool SetExists()
        {
            return _multiplexer.GetDatabase().KeyExists(_sortedSetKey);
        }

        public async Task Delete()
        {
            var deleted = await _multiplexer.GetDatabase().KeyDeleteAsync(_sortedSetKey);
            string notification = $"Deletion: " + deleted;
            _subscriber.Publish(_channel, notification);
            _logger.LogInformation("Published '{notification}' to {channel}", notification, _channel);
        }

        /// <summary>
        /// Adds an item to the sorted set
        /// </summary>
        /// <param name="itemToAdd">a string representing the item</param>
        /// <param name="score">a score</param>
        public async Task AddItem(string itemToAdd, double score)
        {
            // here is one way to add an entry:
            
            var entry = new SortedSetEntry(itemToAdd, score);
            SortedSetEntry[] sortedSetEntries = new SortedSetEntry[] { entry };
            /*long res = await _multiplexer.GetDatabase().SortedSetAddAsync(_sortedSetKey, sortedSetEntries);
            */

            // here is a way to always add an entry:
            bool res = await _multiplexer.GetDatabase().SortedSetAddAsync(_sortedSetKey, itemToAdd, score, When.NotExists);

            string notification = $"{itemToAdd} added result {res}";
            _subscriber.Publish(_channel, notification);
            _logger.LogInformation("Published '{notification}' to {channel}", notification, _channel);
        }

        /// <summary>
        /// This event can be hooked in blazor components.
        /// </summary>
        public event Action SortedSetManagementDemoNotification;

        public event Action<string> SortedSetAdditionTriggered;

        /// <summary>
        /// Unsubscribe and clean up.
        /// </summary>
        public void Dispose()
        {
            _subscriber.UnsubscribeAll();
            _multiplexer.Close();
        }
    }
}
