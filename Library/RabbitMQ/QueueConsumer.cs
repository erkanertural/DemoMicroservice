using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Library.RabbitMQ
{
    /// <summary>
    /// This class contains logic to write approved message in the queue
    /// </summary>
    public interface IQueueConsumer
    {
        public IConnection Connection { get; set; }
        public IModel Channel { get; set; }
        void InitRabbitMQ();

    }
    public class QueueConsumer : IQueueConsumer
    {

        public QueueConsumer()
        {
            InitRabbitMQ();
        }
        IModel channel;
        IConnection connection;
        public IModel Channel { get => channel; set => channel = value; }

        public IConnection Connection { get => connection; set => connection = value; }

        public void InitRabbitMQ()
        {
            if (Channel == null)
            {
                // creating a connection factory
                var factory = new ConnectionFactory() { HostName = "localhost", UserName = "guest", Password = "guest" };

                // create a connection (we are using the default one)
                connection = factory.CreateConnection();
                // create a channel
                channel = connection.CreateModel();

                // time to live message 
                var ttl = new Dictionary<string, object> { { "x-message-ttl", 600000 } }; // message will live for 60 seconds

                //                      Exchange name           ,   Exchange type   , optional argument
                channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic, arguments: ttl);
                channel.QueueDeclare("demo.queue.log", false, false, false, null);

                channel.QueueBind("demo.queue.log", "demo.exchange", "demo.queue.*", null);
                channel.BasicQos(0, 1, false);
            }

        }

    }
}