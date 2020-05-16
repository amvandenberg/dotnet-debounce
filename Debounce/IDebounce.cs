﻿using System;

namespace Dorssel.Utility
{
    public interface IDebounce : IDisposable
    {
        event EventHandler<IDebouncedEventArgs>? Debounced;

        void Trigger();

        public TimeSpan DebounceWindow { get; set; }
        public TimeSpan DebounceTimeout { get; set; }
        public TimeSpan EventSpacing { get; set; }
        public TimeSpan HandlerSpacing { get; set; }
        public TimeSpan TimingGranularity { get; set; }
    }
}
