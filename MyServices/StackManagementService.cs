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
    public class StackManagementService : IDisposable
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ISubscriber _subscriber;
        private readonly ILogger<StackManagementService> _logger;
        private readonly string _channel;
        private readonly string _listKey;

        public StackManagementService(IConfiguration configuration, ILogger<StackManagementService> logger)
        {
            _listKey = "StackManagementDemoItems";

            // connect to redis
            _multiplexer = ConnectionMultiplexer.Connect(configuration["REDIS"]);

            // set a private field to a string to identify what type of messages this service should publish and subscribe to in redis
            _channel = "blazor:StackManagementDemo";

            // set up logging see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-5.0
            _logger = logger;

            // this obtains a pub-sub connection to a redis server. see above _multiplexer
            _subscriber = _multiplexer.GetSubscriber();

            // create an event handler in-line for what happens when the channel we are subscribed to receives a message
            // to send messages to this channel, use:
            // publish 'blazor:StackManagementDemo' 'hello world'
            _subscriber.Subscribe(_channel, (channel, message) => {
                _logger.LogInformation("Received on {channel} channel: {notification} ", (string)channel, (string)message);
                StackManagerNotification?.Invoke();
                if (message.ToString().EndsWith("removed"))
                {
                    StackRemovalTriggered?.Invoke((message.ToString().Split(' ').First()));
                }
                if (message.ToString().EndsWith("added"))
                {
                    StackAdditionTriggered?.Invoke((message.ToString().Split(' ').First()));
                }
            });
        }

        /// <summary>
        /// Get all items in a stack
        /// </summary>
        /// <returns>A list of strings, could get expensive growing the list over and over if many items in redis</returns>
        public List<string> GetStackItems()
        {
            List<string> results = new List<string>();
            foreach (var item in _multiplexer.GetDatabase().ListRange(_listKey))
            {
                results.Add(item);
            }
            return results;
        }

        /// <summary>
        /// Check if the key exists.
        /// </summary>
        public bool StackExists()
        {
            return _multiplexer.GetDatabase().KeyExists(_listKey);
        }


        /// <summary>
        /// Adds an item to the stack
        /// </summary>
        /// <param name="itemToAdd">a string representing a stacked item</param>
        public async Task<long> AddItem(string itemToAdd)
        {
            long itemsInList = await _multiplexer.GetDatabase().ListLeftPushAsync(_listKey, itemToAdd);
            string notification = $"{itemToAdd} added";
            _subscriber.Publish(_channel, notification);
            _logger.LogInformation("Published '{notification}' to {channel}", notification, _channel);
            return itemsInList;
        }

        /// <summary>
        /// Pop an item off the beginning of the stack and inform connected subscribers that inventory of our stack has changed.
        /// </summary>
        public void RemoveItem()
        {
            var popped = _multiplexer.GetDatabase().ListLeftPop(_listKey);
            string notification = $"{popped} removed";
            _subscriber.Publish(_channel, notification);
            _logger.LogInformation("Published '{notification}' to {channel}", notification, _channel);
        }

        /// <summary>
        /// This event can be hooked in blazor components.
        /// </summary>
        public event Action StackManagerNotification;

        public event Action<string> StackAdditionTriggered;
        public event Action<string> StackRemovalTriggered;

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
