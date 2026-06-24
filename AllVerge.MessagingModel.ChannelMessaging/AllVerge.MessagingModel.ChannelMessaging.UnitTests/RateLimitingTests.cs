using System;
using System.IO;
using System.Xml;
using Xunit;

namespace AllVerge.MessagingModel.ChannelMessaging.UnitTests
{
    public class RateLimitingTests
    {
        [Fact]
        public void CalculateIntervalsTest1()
        {
            int pollTimeoutMS = 500;
            int pollCount = 100;

            int maxRatePerSecond = 100;

            CalculateIntervals(pollTimeoutMS, pollCount, maxRatePerSecond, out int handleIntervalMS, out int pollIntervalMS);

            Assert.Equal(10, handleIntervalMS);
            Assert.Equal(500, pollIntervalMS);
        }

        [Fact]
        public void CalculateIntervalsTest2()
        {
            int pollTimeoutMS = 1500;
            int poll = 300;

            int limitRatePerSecond = 100;

            CalculateIntervals(pollTimeoutMS, poll, limitRatePerSecond, out int handleIntervalMS, out int pollIntervalMS);

            Assert.Equal(10, handleIntervalMS);
            Assert.Equal(1500, pollIntervalMS);
        }

        [Fact]
        public void CalculateIntervalsTest3()
        {
            int targetRate = 100;

            int pollCount1 = 100;
            int pollTimeoutMS1 = 1000;
            int pollCount2 = 50;
            int pollTimeoutMS2 = 2000;
            int pollCount3 = 1000;
            int pollTimeoutMS3 = 1500;

            CalculateNormalizedRates(targetRate, pollCount1, pollTimeoutMS1, pollCount2, pollTimeoutMS2, pollCount3, pollTimeoutMS3, out int normalizedRate1, out int normalizedRate2, out int normalizedRate3);

            double totalNormalizedRate = normalizedRate1 + normalizedRate2 + normalizedRate3;

            Assert.Equal(100, totalNormalizedRate);
            Assert.Equal(9, normalizedRate1);
            Assert.Equal(2, normalizedRate2);
            Assert.Equal(89, normalizedRate3);
        }

        [Fact]
        public void CalculateIntervalsTest4()
        {
            int pollTimeoutMS = 500;
            int pollCount = 100;
            int maxRatePerSecond = 100;

            for (int j = 0; j < 100; j++)
            {
                CalculateIntervals(pollTimeoutMS, pollCount, maxRatePerSecond, out int handleIntervalMS, out int pollIntervalMS);

                Assert.True(handleIntervalMS >= 0);
                Assert.True(pollIntervalMS >= 0);
            }
        }

        private static void CalculateNormalizedRates(int targetRate, int pollCount1, int pollTimeoutMS1, int pollCount2, int pollTimeoutMS2, int pollCount3, int pollTimeoutMS3, out int normalizedRate1, out int normalizedRate2, out int normalizedRate3)
        {
            double rate1 = (double)pollCount1 / (pollTimeoutMS1 / 1000);
            double rate2 = (double)pollCount2 / (pollTimeoutMS2 / 1000);
            double rate3 = (double)pollCount3 / (pollTimeoutMS3 / 1000);

            double totalRate = rate1 + rate2 + rate3;

            normalizedRate1 = (int)Math.Round(targetRate * (rate1 / totalRate), MidpointRounding.AwayFromZero);
            normalizedRate2 = (int)Math.Round(targetRate * (rate2 / totalRate), MidpointRounding.AwayFromZero);
            normalizedRate3 = (int)Math.Round(targetRate * (rate3 / totalRate), MidpointRounding.AwayFromZero);
        }

        private static void CalculateIntervals(int pollTimeoutMS, int pollCount, int maxRatePerSecond, out int handleIntervalMS, out int pollIntervalMS)
        {
            double timeoutSec = (double)pollTimeoutMS / 1000;

            double rate = (double)pollCount / timeoutSec;

            double factor;

            if (rate > maxRatePerSecond)
            {
                factor = (double)maxRatePerSecond / (double)rate;
            }
            else
            {
                factor = (double)rate / (double)maxRatePerSecond;
            }

            handleIntervalMS = (int)(1000 / (rate * factor));

            int handleTimeMS = handleIntervalMS * pollCount;

            if (handleTimeMS > pollTimeoutMS)

                pollIntervalMS = handleTimeMS - pollTimeoutMS;

            else

                pollIntervalMS = 0;
        }
    }
}
