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
    public class QueueManagementService : IDisposable
    {
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly ISubscriber _subscriber;
        private readonly ILogger<QueueManagementService> _logger;
        private readonly string _channel;
        private readonly string _listKey;

        public QueueManagementService(IConfiguration configuration, ILogger<QueueManagementService> logger)
        {
            _listKey = "QueueManagementDemoItems";

            // connect to redis
            _multiplexer = ConnectionMultiplexer.Connect(configuration["REDIS"]);

            // set a private field to a string to identify what type of messages this service should publish and subscribe to in redis
            _channel = "blazor:QueueManagementDemo";

            // set up logging see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-5.0
            _logger = logger;

            // this obtains a pub-sub connection to a redis server. see above _multiplexer
            _subscriber = _multiplexer.GetSubscriber();

            // create an event handler in-line for what happens when the channel we are subscribed to receives a message
            // to send messages to this channel, use:
            // publish 'blazor:QueueManagementDemo' 'hello world'
            _subscriber.Subscribe(_channel, (channel, message) => {
                _logger.LogInformation("Received on {channel} channel: {notification} ", (string)channel, (string)message);
                QueueManagerNotification?.Invoke();
            });
        }

        /// <summary>
        /// Get all items in a queue
        /// </summary>
        /// <returns>A list of strings, could get expensive growing the list over and over if many items in redis</returns>
        public List<string> GetQueuedItems()
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
        public bool QueueExists()
        {
            return _multiplexer.GetDatabase().KeyExists(_listKey);
        }

        /// <summary>
        /// Notify all connected subscribers that inventory of our queue has changed, after adding an item to the end of it.
        /// </summary>
        /// <param name="notification"></param>
        public async Task<long> AddItem(string itemToAdd)
        {
            long itemsInList = await _multiplexer.GetDatabase().ListRightPushAsync(_listKey, itemToAdd);               
            string notification = "Queue contents changed {item} added to queue.";
            _subscriber.Publish(_channel, notification);
            _logger.LogInformation("Published '{notification}' to {channel}", notification, _channel);
            return itemsInList;
        }

        /// <summary>
        /// Pop an item off the beginning of the queue and inform connected subscribers that inventory of our queue has changed.
        /// </summary>
        public string RemoveItem()
        {
            var popped = _multiplexer.GetDatabase().ListLeftPop(_listKey);
            string notification = "Queue contents changed: {" + popped + "} popped from queue.";
            _subscriber.Publish(_channel, notification);
            _logger.LogInformation("Published '{notification}' to {channel}", notification, _channel);
            return popped;
        }

        /// <summary>
        /// This event can be hooked in blazor components.
        /// </summary>
        public event Action QueueManagerNotification;

        /// <summary>
        /// Unsubscribe and clean up.
        /// </summary>
        public void Dispose()
        {
            _subscriber.UnsubscribeAll();
            _multiplexer.Dispose();
        }

    }
}
