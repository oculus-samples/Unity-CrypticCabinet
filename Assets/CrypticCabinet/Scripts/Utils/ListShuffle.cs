// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Threading;
using Meta.XR.Samples;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Provides functionality to shuffle a IList.
    ///     Based on post here: https://stackoverflow.com/a/1262619
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    internal static class ListShuffle
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }

    /// <summary>
    ///     A thread safe source of random.
    /// </summary>
    [MetaCodeSample("CrypticCabinet")]
    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random s_local;
        public static Random ThisThreadsRandom => s_local ??= new Random(unchecked(Environment.TickCount * Thread.CurrentThread.ManagedThreadId));
    }
}