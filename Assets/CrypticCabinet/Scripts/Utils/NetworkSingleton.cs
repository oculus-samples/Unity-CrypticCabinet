// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Fusion;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     Ensures that a specified network object only exists uniquely, following the singleton pattern.
    /// </summary>
    /// <typeparam name="T">The type of the singleton object</typeparam>
    public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
    {
        private static Action<T> s_onAwake;
        public static T Instance { get; private set; }

        protected void Awake()
        {
            if (!enabled)
            {
                return;
            }

            Debug.Assert(Instance == null, $"Singleton {typeof(T).Name} has been instantiated more than once.", this);
            Instance = (T)this;
            s_onAwake?.Invoke(Instance);
            s_onAwake = null;
        }

        protected void OnEnable()
        {
            if (Instance != this)
            {
                Awake();
            }
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void WhenInstantiated(Action<T> action)
        {
            if (Instance != null)
            {
                action(Instance);
            }
            else
            {
                s_onAwake += action;
            }
        }
    }
}