
using ContactMessages.Request;
using Library;
using Library.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReportEntities;
using ReportServices.Services;
using System.Text;

namespace ReportAPI.Helper
{
    public class ReportBackgroundService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IQueueConsumer _queueConsumer;
        private readonly ReportService reportService;

        public ReportBackgroundService(ILoggerFactory loggerFactory, IServiceProvider service, IQueueConsumer consumer)
        {
            _queueConsumer = consumer;

            this._logger = loggerFactory.CreateLogger<ReportBackgroundService>();

        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var consumer = new EventingBasicConsumer(_queueConsumer.Channel);
            consumer.Received += (ch, message) =>
            {
                // received body
                var content = Encoding.UTF8.GetString(message.Body.ToArray());
                // handled the received Message
                HandleReceivedMessage(content);
                // acknowledge the message
                _queueConsumer.Channel.BasicAck(message.DeliveryTag, false);
            };

            consumer.Shutdown += Consumer_Shutdown;
            consumer.Registered += Consumer_Registered;
            consumer.Unregistered += Consumer_Unregistered;
            consumer.ConsumerCancelled += Consumer_ConsumerCancelled;
            _queueConsumer.Channel.BasicConsume("demo.queue.log", false, consumer);

            return Task.CompletedTask;
        }

        private void Consumer_ConsumerCancelled(object? sender, ConsumerEventArgs e)
        {

        }

        private void Consumer_Unregistered(object? sender, ConsumerEventArgs e)
        {

        }

        private void Consumer_Registered(object? sender, ConsumerEventArgs e)
        {

        }

        private void Consumer_Shutdown(object? sender, ShutdownEventArgs e)
        {

        }

        public override void Dispose()
        {
            _queueConsumer.Channel.Close();
            _queueConsumer.Connection.Close();
            base.Dispose();
        }

        private void HandleReceivedMessage(string content)
        {


            ReportDto reportDto = content.DeSerialize<ReportDto>();
            string path = reportDto.ReportId + ".xls";
            File.WriteAllText(path, content); // fake excel
            var report = reportService.Get(reportDto.ReportId).Result;
            report.Data.TaskStatus = Report.TaskStatusType.Completed;
            reportService.Edit(report.Data).Wait();
            Console.WriteLine($"######    Report Completed {reportDto.ReportId}");
        }
    }
}
