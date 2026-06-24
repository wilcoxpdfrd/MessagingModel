using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace AllVerge.MessagingModel.MessagingApplication.Hosting.Diagnostics
{
    /// <summary>
    /// Aggregation class to collect key processing performance indicators.
    /// </summary>
    public class KPICollector
    {
        private class KPIAccumulator
        {
            public long Successes;
            public TimeSpan Mean;
            public TimeSpan Max;
            public TimeSpan Min;
            internal int accumluateRetryOccurences;
            internal int accumluateRetries;
            internal int maxAccumulateRetries;

            public KPIAccumulator()
            {
                this.Successes = 0;
                this.Mean = TimeSpan.Zero;
                this.Max = TimeSpan.Zero;
                this.Min = TimeSpan.MaxValue;
                this.accumluateRetryOccurences = 0;
                this.accumluateRetries = 0;
                this.maxAccumulateRetries = 0;
            }

            public KPIAccumulator(KPIAccumulator kpisAccumulator)
            {
                this.Successes = kpisAccumulator.Successes;
                this.Mean = kpisAccumulator.Mean;
                this.Max = kpisAccumulator.Max;
                this.Min = kpisAccumulator.Min;
                this.accumluateRetryOccurences = kpisAccumulator.accumluateRetryOccurences;
                this.accumluateRetries = kpisAccumulator.accumluateRetries;
                this.maxAccumulateRetries = kpisAccumulator.maxAccumulateRetries;
            }

            public KPIAccumulator AccumulateDuration(TimeSpan duration, int accumulateRetries)
            {
                KPIAccumulator computedAccumulator = new KPIAccumulator(this);

                computedAccumulator.Successes++;

                if (accumulateRetries > 0)
                {
                    computedAccumulator.accumluateRetryOccurences++;

                    computedAccumulator.accumluateRetries += accumulateRetries;

                    if (accumulateRetries > computedAccumulator.maxAccumulateRetries)

                        computedAccumulator.maxAccumulateRetries = accumulateRetries;
                }

                if (computedAccumulator.Max.CompareTo(duration) < 0)

                    computedAccumulator.Max = duration;

                if (computedAccumulator.Min.CompareTo(duration) > 0)

                    computedAccumulator.Min = duration;

                //XX --> average at iteration n+1
                //n --> iteration count
                //v --> new value (at iteration n+1)
                //X -- average at interation n

                //XX = X + ((v-X)/(n+1))

                computedAccumulator.Mean =
                    computedAccumulator.Mean.Add(TimeSpan.FromTicks(duration.Subtract(computedAccumulator.Mean).Ticks / computedAccumulator.Successes));

                return computedAccumulator;
            }
        }

        private const int BASE_RETRY = 10;
        private const int MAX_RETRY_COUNT = 16;
        private const int MAX_RETRY_CEILING = 10;
        private const int MAX_READ_RETRY_COUNT = 16;

        private string categoryName;
        private string instanceName;

        private DateTime started;
        private KPIAccumulator kpisAccumulator;
        private long failures;

        private int readCount;
        [ThreadStatic]
        private static int threadStaticRetryCount;
        [ThreadStatic]
        private static Random threadStaticRandom;

        /// <summary>
        /// Initializes a new instance of the <see cref="KPICollector"/> class.
        /// </summary>
        protected KPICollector()
        {
            this.started = DateTime.Now;
            this.failures = 0;
            this.readCount = 0;

            this.kpisAccumulator =
                new KPIAccumulator();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KPICollector"/> class.
        /// </summary>
        /// <param name="categoryName">The kpi category name.</param>
        /// <param name="instanceName">The process instance name.</param>
        public KPICollector(string categoryName, string instanceName) : this()
        {
            this.categoryName = categoryName;
            this.instanceName = instanceName;
        }

        /// <summary>
        /// The name of the process instance.
        /// </summary>
        public string InstanceName
        {
            get { return this.instanceName; }
        }

        /// <summary>
        /// The time kpi collection was enabled.
        /// </summary>
        public DateTime Started
        {
            get { return this.started; }
        }

        /// <summary>
        /// The number of successes since kpi collection was enabled.
        /// </summary>
        /// <seealso cref="Started"/>
        public long Successes
        {
            get { return kpisAccumulator.Successes; }
        }

        /// <summary>
        /// The number of failures since kpi collection was enabled.
        /// </summary>
        /// <seealso cref="Started"/>
        public long Failures
        {
            get { return this.failures; }
        }

        /// <summary>
        /// The percentage of failures since kpi collection was enabled.
        /// </summary>
        /// <seealso cref="Started"/>
        public int ErrorRate
        {
            get
            {
                return CalculateErrorRate(this.Successes, this.Failures);
            }
        }

        /// <summary>
        /// The mean time to complete all process executions since kpi collection was enabled.
        /// </summary>
        /// <seealso cref="Started"/>
        public TimeSpan Mean
        {
            get { return kpisAccumulator.Mean; }
        }

        /// <summary>
        /// The maximum time to complete all process executions since kpi collection was enabled.
        /// </summary>
        /// <seealso cref="Started"/>
        public TimeSpan Max
        {
            get { return kpisAccumulator.Max; }
        }

        /// <summary>
        /// The minimum time to complete all process executions since kpi collection was enabled.
        /// </summary>
        /// <seealso cref="Started"/>
        public TimeSpan Min
        {
            get { return kpisAccumulator.Min; }
        }

        /// <summary>
        /// Returns a new instance of <see cref="KPICollector"/> or a sub-class.
        /// </summary>
        /// <returns></returns>
        protected virtual KPICollector New()
        {
            return new KPICollector();
        }

        /// <summary>
        /// Override to return an array of concrete <see cref="IIndicator"/>, reflecting the aggregated KPIs.
        /// </summary>
        protected virtual IIndicator[] ReadIndicators()
        {
            List<IIndicator> indicators = new List<IIndicator>();

            indicators.Add(new DateTimeIndicator(this.categoryName, this.instanceName, "StartTime", "DiagnosticPeriod", this.started));
            indicators.Add(new LongIndicator(this.categoryName, this.instanceName, "Failures", "ItemCount", this.failures));
            indicators.Add(new LongIndicator(this.categoryName, this.instanceName, "Successes", "ItemCount", this.kpisAccumulator.Successes));
            indicators.Add(new TimespanIndicator(this.categoryName, this.instanceName, "MaxResponseTime", "Milliseconds", this.kpisAccumulator.Max));
            indicators.Add(new TimespanIndicator(this.categoryName, this.instanceName, "MeanResponseTime", "Milliseconds", this.kpisAccumulator.Mean));
            indicators.Add(new TimespanIndicator(this.categoryName, this.instanceName, "MinResponseTime", "Milliseconds", this.kpisAccumulator.Min));

            return indicators.ToArray();
        }

        /// <summary>
        /// Called by the process runtime to accumulate successful process executions.
        /// </summary>
        /// <param name="duration">The time to complete process execution.</param>
        public void AccumulateSuccess(TimeSpan duration)
        {
            KPIAccumulator initialValue, computedValue;

            threadStaticRetryCount = 0;

            do
            {
                initialValue = kpisAccumulator;

                computedValue = initialValue.AccumulateDuration(duration, threadStaticRetryCount);

                threadStaticRetryCount++;
            }
            while (AccumulateRetryDelay(initialValue, computedValue, threadStaticRetryCount));
        }

        private bool AccumulateRetryDelay(KPIAccumulator initialValue, KPIAccumulator computedValue, int threadStaticRetryCount)
        {
            if (initialValue != Interlocked.CompareExchange<KPIAccumulator>(ref kpisAccumulator, computedValue, initialValue))
            {
                // wait using exponential backoff based on a 10 ms base delay

                if (threadStaticRetryCount > 0)
                {
                    if (threadStaticRetryCount == MAX_RETRY_COUNT)
                        return false;

                    int retryCount = threadStaticRetryCount;

                    if (retryCount > MAX_RETRY_CEILING)
                        retryCount = MAX_RETRY_CEILING;

                    int wait = GetWaitTime(retryCount);

                    System.Threading.Thread.Sleep(wait);
                }

                return true;
            }

            return false;
        }

        private static int GetWaitTime(int retryCount)
        {
            //http://en.wikipedia.org/wiki/Exponential_backoff

            //http://blog.linqexchange.com/index.php/how-to-generate-a-random-sequence-with-linq/
            //return Enumerable.Range(0, (int)Math.Pow(2, retryCount) - 1).OrderBy(n => Guid.NewGuid()).First() * BASE_RETRY;

            return NextRandom((int)Math.Pow(2, retryCount) - 1) * BASE_RETRY;
        }

        private static int NextRandom(int maxValue)
        {
            if (threadStaticRandom == null)
                threadStaticRandom = new Random(System.Threading.Thread.CurrentThread.ManagedThreadId);

            // Add 1 to maxValue as random.Next excludes MaxValue.
            return threadStaticRandom.Next(maxValue + 1);
        }

        /// <summary>
        /// Called by the process runtime to accumulate failured process executions.
        /// </summary>
        public void AccumulateFailure()
        {
            Interlocked.Increment(ref failures);
        }

        /// <summary>
        /// Attempts to read diagnostic indicators in a thread safe manner.
        /// </summary>
        /// <returns><see cref="Array"/> of <see cref="IIndicator"/>, or null, if the read operation timeed out.</returns>
        public IIndicator[] TryReadIndicators()
        {
            int initialReadCount, computedReadCount, retryCount;

            IIndicator[] indicators = null;

            retryCount = 0;

            do
            {
                if (retryCount == MAX_READ_RETRY_COUNT)

                    break;

                initialReadCount = readCount;

                computedReadCount = readCount + 1;

                indicators = this.ReadIndicators();

                retryCount++;
            }
            while (initialReadCount != Interlocked.CompareExchange(ref readCount, computedReadCount, initialReadCount));

            return indicators;
        }

        /// <summary>
        /// Calculates the error rate given <paramref name="successes"/> and <paramref name="failures"/>.
        /// </summary>
        /// <param name="successes">The number of successful process executions.</param>
        /// <param name="failures">The number of failed process executions.</param>
        /// <returns>The calculated error rate.</returns>
        private static int CalculateErrorRate(long successes, long failures)
        {
            long total = successes + failures;

            if (total > 0)

                return (int)(((double)failures / (double)total) * 100);

            return 0;
        }
    }
}
