namespace datopus.Core.Exceptions
{
    public class UnsupportedMetricException : BaseException
    {
        public string MetricKey { get; }

        public UnsupportedMetricException(string metricKey)
            : base($"Unsupported metric type: {metricKey}")
        {
            MetricKey = metricKey;
        }
    }
}
