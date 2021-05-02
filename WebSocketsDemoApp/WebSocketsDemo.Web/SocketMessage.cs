using Newtonsoft.Json;

namespace WebSocketsDemo.Web
{
    public class SocketMessage<T>
    {
        public string MessageType { get; set; }
        public T Payload { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
