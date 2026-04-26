using Confluent.Kafka;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Aparesk.Eskineria.Core.Logging.Sinks;

public sealed class KafkaLogEventSink : ILogEventSink, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;
    private readonly ITextFormatter _formatter;
    private bool _disposed;

    public KafkaLogEventSink(
        ProducerConfig producerConfig,
        string topic,
        ITextFormatter? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(producerConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        _topic = topic.Trim();
        _formatter = formatter ?? new JsonFormatter(renderMessage: true);
    }

    public void Emit(LogEvent logEvent)
    {
        if (_disposed)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(logEvent);

        string payload;
        using (var writer = new StringWriter())
        {
            _formatter.Format(logEvent, writer);
            payload = writer.ToString();
        }

        try
        {
            _producer.Produce(_topic, new Message<Null, string>
            {
                Value = payload
            });
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Kafka log emit failed. Topic: {0}. Error: {1}", _topic, ex);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Kafka log flush failed. Topic: {0}. Error: {1}", _topic, ex);
        }

        _producer.Dispose();
    }
}
