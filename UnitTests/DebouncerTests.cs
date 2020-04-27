﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dorssel.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class DebouncerTests
    {
        static TimeSpan TimingUnit = TimeSpan.FromMilliseconds(50);

        public static IEnumerable<object[]> PositiveTimeSpans
        {
            get
            {
                yield return new object[] { TimeSpan.MaxValue };
                yield return new object[] { TimeSpan.FromDays(1) };
                yield return new object[] { TimeSpan.FromHours(1) };
                yield return new object[] { TimeSpan.FromMinutes(1) };
                yield return new object[] { TimeSpan.FromSeconds(1) };
                yield return new object[] { TimeSpan.FromMilliseconds(1) };
                yield return new object[] { TimeSpan.FromTicks(1) };
            }
        }

        public static IEnumerable<object[]> ZeroTimeSpan
        {
            get
            {
                yield return new object[] { TimeSpan.Zero };
            }
        }

        public static IEnumerable<object[]> InfiniteTimeSpan
        {
            get
            {
                yield return new object[] { Timeout.InfiniteTimeSpan };
            }
        }

        public static IEnumerable<object[]> NegativeTimeSpans
        {
            get
            {
                // NOTE: FromMilliseconds(-1) == InfiniteTimeSpan, a  magic value
                yield return new object[] { TimeSpan.FromTicks(-1) };
                yield return new object[] { TimeSpan.FromMilliseconds(-2) };
                yield return new object[] { TimeSpan.FromSeconds(-1) };
                yield return new object[] { TimeSpan.FromMinutes(-1) };
                yield return new object[] { TimeSpan.FromHours(-1) };
                yield return new object[] { TimeSpan.FromDays(-1) };
                yield return new object[] { TimeSpan.MinValue };
            }
        }

        #region Constructor
        [TestMethod]
        public void ConstructorNoThrow()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            _ = new Debouncer();
#pragma warning restore CA2000 // Dispose objects before losing scope
        }
        #endregion

        #region Dispose
        [TestMethod]
        public void DisposeNoThrow()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
        }

        [TestMethod]
        public void DisposeMultipleNoThrow()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            debouncer.Dispose();
        }

        [TestMethod]
        public void DisposeDuringTimer()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = 2 * TimingUnit
            };
            long callCount = 0;
            debouncer.Debounced += (s, e) =>
            {
                Assert.AreSame(s, debouncer);
                Assert.AreEqual(e.Count, 1L);
                ++callCount;
            };
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            debouncer.Dispose();
            Assert.AreEqual(callCount, 0L);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(callCount, 0L);
        }

        [TestMethod]
        public void DisposeDuringHandler()
        {
            var debouncer = new Debouncer();
            using var done = new ManualResetEventSlim();
            debouncer.Debounced += (s, e) =>
            {
                Thread.Sleep(2 * TimingUnit);
                done.Set();
            };
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            debouncer.Dispose();
            done.Wait();
        }
   
        [TestMethod]
        public void DisposeFromHandler()
        {
            using var debouncer = new Debouncer();
            using var done = new ManualResetEventSlim();
            debouncer.Debounced += (s, e) =>
            {
                debouncer.Dispose();
                done.Set();
            };
            debouncer.Trigger();
            done.Wait();
        }
        #endregion

        #region DebounceInterval
        [TestMethod]
        public void DebounceIntervalDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.Zero);
        }

        [DataTestMethod]
        [DynamicData(nameof(PositiveTimeSpans))]
        [DynamicData(nameof(ZeroTimeSpan))]
        public void DebounceIntervalValid(TimeSpan debounceInterval)
        {
            using var debouncer = new Debouncer
            {
                DebounceInterval = debounceInterval
            };
            Assert.AreEqual(debouncer.DebounceInterval, debounceInterval);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void DebounceIntervalInvalid(TimeSpan debounceInterval)
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceInterval = debounceInterval);
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public void DebounceIntervalUnchanged()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.FromMilliseconds(1));
            debouncer.DebounceInterval = TimeSpan.FromMilliseconds(1);
            Assert.AreEqual(debouncer.DebounceInterval, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public void DebounceIntervalExceedsDebounceTimeout()
        {
            using var debouncer = new Debouncer()
            {
                DebounceTimeout = TimeSpan.FromSeconds(1)
            };
            Assert.ThrowsException<ArgumentException>(() => debouncer.DebounceInterval = TimeSpan.FromSeconds(2));
        }

        [TestMethod]
        public void DebounceIntervalAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceInterval = TimeSpan.Zero);
        }
        #endregion

        #region DebounceTimeout
        [TestMethod]
        public void DebounceTimeoutDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(debouncer.DebounceTimeout, Timeout.InfiniteTimeSpan);
        }

        [DataTestMethod]
        [DynamicData(nameof(PositiveTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        [DynamicData(nameof(ZeroTimeSpan))]
        public void DebounceTimeoutValid(TimeSpan debounceTimeout)
        {
            using var debouncer = new Debouncer
            {
                DebounceTimeout = debounceTimeout
            };
            Assert.AreEqual(debouncer.DebounceTimeout, debounceTimeout);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        public void DebounceTimeoutInvalid(TimeSpan debounceTimeout)
        {
            using var debouncer = new Debouncer()
            {
                DebounceTimeout = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.DebounceTimeout = debounceTimeout);
            Assert.AreEqual(debouncer.DebounceTimeout, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public void DebounceTimeoutUnchanged()
        {
            using var debouncer = new Debouncer()
            {
                DebounceTimeout = TimeSpan.FromMilliseconds(1)
            };
            Assert.AreEqual(debouncer.DebounceTimeout, TimeSpan.FromMilliseconds(1));
            debouncer.DebounceTimeout = TimeSpan.FromMilliseconds(1);
            Assert.AreEqual(debouncer.DebounceTimeout, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public void DebounceTimeoutLessThanDebounceInterval()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromSeconds(2)
            };
            Assert.ThrowsException<ArgumentException>(() => debouncer.DebounceTimeout = TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void DebounceTimeoutAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.DebounceTimeout = TimeSpan.Zero);
        }
        #endregion

        #region Backoff
        [TestMethod]
        public void BackoffIntervalDefault()
        {
            using var debouncer = new Debouncer();
            Assert.AreEqual(debouncer.BackoffInterval, TimeSpan.Zero);
        }

        [DataTestMethod]
        [DynamicData(nameof(PositiveTimeSpans))]
        [DynamicData(nameof(ZeroTimeSpan))]
        public void BackoffIntervalValid(TimeSpan backoffInterval)
        {
            using var debouncer = new Debouncer
            {
                BackoffInterval = backoffInterval
            };
            Assert.AreEqual(debouncer.BackoffInterval, backoffInterval);
        }

        [DataTestMethod]
        [DynamicData(nameof(NegativeTimeSpans))]
        [DynamicData(nameof(InfiniteTimeSpan))]
        public void BackoffIntervalInvalid(TimeSpan backoffInterval)
        {
            using var debouncer = new Debouncer()
            {
                BackoffInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => debouncer.BackoffInterval = backoffInterval);
            Assert.AreEqual(debouncer.BackoffInterval, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public void BackoffIntervalUnchanged()
        {
            using var debouncer = new Debouncer()
            {
                BackoffInterval = TimeSpan.FromMilliseconds(1)
            };
            Assert.AreEqual(debouncer.BackoffInterval, TimeSpan.FromMilliseconds(1));
            debouncer.BackoffInterval = TimeSpan.FromMilliseconds(1);
            Assert.AreEqual(debouncer.BackoffInterval, TimeSpan.FromMilliseconds(1));
        }

        [TestMethod]
        public void BackoffIntervalAfterDispose()
        {
            var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.BackoffInterval = TimeSpan.Zero);
        }
        #endregion

        #region Trigger
        [TestMethod]
        public void TriggerWithoutHandlers()
        {
            {
                using var debouncer = new Debouncer();
                debouncer.Trigger();
            }
            Thread.Sleep(TimingUnit);
        }

        [TestMethod]
        public void TriggerAfterDispose()
        {
            using var debouncer = new Debouncer();
            debouncer.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => debouncer.Trigger());
        }

        [TestMethod]
        public void TriggerSingle()
        {
            using var debouncer = new Debouncer();
            long callCount = 0;
            debouncer.Debounced += (s, e) =>
            {
                Assert.AreSame(s, debouncer);
                Assert.AreEqual(e.Count, 1L);
                ++callCount;
            };
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(callCount, 1L);
        }

        [TestMethod]
        public void TriggerSingleDelay()
        {
            using var debouncer = new Debouncer()
            {
                DebounceInterval = TimeSpan.FromMilliseconds(100)
            };
            long callCount = 0;
            debouncer.Debounced += (s, e) =>
            {
                Assert.AreSame(s, debouncer);
                Assert.AreEqual(e.Count, 1L);
                ++callCount;
            };
            debouncer.Trigger();
            Thread.Sleep(TimingUnit);
            Assert.AreEqual(callCount, 0L);
            Thread.Sleep(2 * TimingUnit);
            Assert.AreEqual(callCount, 1L);
        }
        #endregion
    }
}
