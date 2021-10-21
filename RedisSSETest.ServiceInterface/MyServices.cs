using System;
using ServiceStack;
using RedisSSETest.ServiceModel;

namespace RedisSSETest.ServiceInterface
{
    public class MyServices : Service
    {
        public IServerEvents ServerEvents { get; set; }
        
        public object Any(Hello request)
        {
            ServerEvents.NotifyAll(request.Name);
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }
    }
}
