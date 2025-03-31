using System.Threading;
using NetMQ;
using NetMQ.Sockets;

namespace FastRepository {
    internal class EventPublisher {

        private NetMQSocket socket;
        private static readonly object synObj = new object();
        public EventPublisher() {
            const string serviceUri = "tcp://127.0.0.1:8501";
            socket = new PublisherSocket();
            socket.Options.SendHighWatermark = 0;
            socket.Bind(serviceUri);
            Thread.Sleep(200);
        }

        public void Publish(string identifier) {
            lock (synObj)
            {
                socket.SendFrame("ImageStored" + "," + identifier);
            }
            
        }
    }
}
