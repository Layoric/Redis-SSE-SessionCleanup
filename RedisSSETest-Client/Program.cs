using ServiceStack;
using System;
using System.Linq;
using System.Threading;
using RedisSSETest.ServiceModel;
using ServiceStack.Redis;

namespace RedisSSETest_Client
{
    class Program
    {
        private static ServerEventsClient sseClient1 = null;

        private static int redisSessionCount = 0;

        static void Main(string[] args)
        {
            var redisManager = new RedisManagerPool("localhost:6379");

            UpdateRedisSessionCount(redisManager, "Initial Redis sessions");
            // Generate a bunch of sessions and kill each client instance.
            Console.WriteLine("Generating client sessions..");
            for (int i = 0; i < 50; i++)
            {
                sseClient1 = new ServerEventsClient("https://localhost:5001/", new string[] { "channel1" });
                sseClient1.Start();
                Thread.Sleep(100);
                sseClient1.Stop();
                sseClient1.Dispose();
                sseClient1 = null;
            }

            UpdateRedisSessionCount(redisManager, "After generated sessions");
            
            Console.WriteLine("Generate persisted client and send 30 messages");
            var sseClient2 = new ServerEventsClient("https://localhost:5001/", new string[] { "channel1" });
            sseClient2.OnMessage = message =>
            {
                Console.WriteLine($"Message received: {message.Data}");
                UpdateRedisSessionCount(redisManager, "Redis sessions");
            };
            sseClient2.Start();
            
            Thread.Sleep(100);
            UpdateRedisSessionCount(redisManager,"Before messages Redis sessions");

            // SSE server periodically checks Redis for expired sessions and cleans them up.
            var jsonServiceClient = new JsonServiceClient("https://localhost:5001");
            for (int i = 0; i < 30; i++)
            {
                {
                    Thread.Sleep(1000);
                    jsonServiceClient.Post(new Hello { Name = $"World"});
                }
            }
            
            UpdateRedisSessionCount(redisManager,"Final Redis session count");
        }
        
        /*Output:
         
Initial Redis sessions: 0
Generating client sessions..
After generated sessions: 50
Generate persisted client and send 30 messages
Before messages Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 51
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 1
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 2
Message received: cmd.String "World"
Redis sessions: 1
Final Redis session count: 1

         */

        private static void UpdateRedisSessionCount(IRedisClientsManager redisManager, string message)
        {
            using var redis = redisManager.GetClient();
            var result = redis.ScanAllKeys("sse:id:*");
            redisSessionCount = result.Count();
            Console.WriteLine($"{message}: {redisSessionCount}");
        }
    }
}
