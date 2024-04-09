﻿// SPDX-FileCopyrightText: 2024 Alain van den Berg
//
// SPDX-License-Identifier: MIT

namespace Dorssel.Utilities;

/// <summary>
/// Provides data for the <see cref="IBufferEvents{TEvent}.Buffered"/> event.
/// </summary>
public class BufferedEventArgs<TEvent> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BufferedEventArgs{TEvent}"/> class.
    /// </summary>
    /// <param name="events">The original events accumulated since the previous buffered event was sent.
    /// <para>Must be greater than 0.</para>
    /// </param>
    public BufferedEventArgs(IReadOnlyList<TEvent> events)
    {
        Events = events;
    }

    /// <summary>
    /// Original events accumulated in this buffered event.
    /// </summary>
    public IReadOnlyList<TEvent> Events { get; }
}