using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace FastRepository {
    internal class EventSubscriber {

        private SubscriberSocket socket;
        
        private const int FetchRetryInterval = 1;
        internal delegate void ImageStoredEventHandler(object sender, ImageStoredEventArgs args);
        internal event ImageStoredEventHandler ImageReceived;
        private static readonly object SyncReceiveMessages = new object();
        public EventSubscriber()
        {
            const string topic = "ImageStored";
            const string serviceUri = "tcp://127.0.0.1:8501";
            socket = new SubscriberSocket();
            socket.Options.ReceiveHighWatermark = 100000;
            socket.Connect(serviceUri);
            socket.Subscribe(topic);
            
            Thread.Sleep(100);
            Task.Factory.StartNew(FetchMessage);
        }

        private void FetchMessage() {
            while (true) {
                while (socket.TryReceiveFrameString(out var messageReceived)) {
                    lock (SyncReceiveMessages) {
                        ImageReceived?.Invoke(this, new ImageStoredEventArgs(messageReceived));
                    }
                }
                Thread.Sleep(FetchRetryInterval);
            }
        }
    }
}

