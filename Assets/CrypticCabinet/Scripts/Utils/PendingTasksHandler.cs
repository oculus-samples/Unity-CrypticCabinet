// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace CrypticCabinet.Utils
{
    /// <summary>
    ///     An utility class that can execute a sequence of pending actions at a later time.
    ///     It offers a mechanism to perform an action immediately if it can be executed, or to
    ///     delay its execution to a future time once it can be executed.
    /// </summary>
    public class PendingTasksHandler
    {
        /// <summary>
        ///     Defines a void pending action that can be executed immediately or as a pending action in the future.
        /// </summary>
        public delegate void PendingAction();

        private readonly List<PendingAction> m_actionList = new();
        private bool m_isExecutingActions;

        /// <summary>
        ///     Try to execute the function if canExecuteNow is true, otherwise add it
        ///     to the queue of pending actions that can then be sequentially executed
        ///     when ExecutePendingActions() is called.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="canExecuteNow">True if we can execute the action now, false otherwise.</param>
        public void TryExecuteAction(PendingAction action, bool canExecuteNow)
        {
            if (canExecuteNow)
            {
                action.Invoke();
            }
            else
            {
                AddAction(action);
            }
        }

        /// <summary>
        ///     If canExecuteAll is true, executes all pending actions sequentially.
        ///     If canExecuteAll is false, remove all existing pending actions from the list.
        /// </summary>
        /// <param name="canExecuteAll">True to execute all, false to clear all.</param>
        public void ExecuteAllOrClear(bool canExecuteAll)
        {
            if (canExecuteAll)
            {
                // Trigger pending actions
                Debug.Log("Executing all pending actions.");
                ExecutePendingActions();
            }
            else if (m_actionList.Count > 0)
            {
                // Clear existing pending actions
                Debug.Log("Clearing all existing pending actions.");
                ClearActions();
            }
        }

        /// <summary>
        ///     Clears all pending actions, if they are not running at the moment.
        /// </summary>
        public void ClearActions()
        {
            if (m_isExecutingActions)
            {
                Debug.LogError("ClearActions failed: task handler is still executing tasks.");
                return;
            }
            m_actionList.Clear();
        }

        /// <summary>
        ///     Sequentially executes all the pending actions.
        /// </summary>
        public void ExecutePendingActions()
        {
            m_isExecutingActions = true;
            foreach (var action in m_actionList)
            {
                action.Invoke();
            }
            m_isExecutingActions = false;
            ClearActions();
        }

        private void AddAction(PendingAction action)
        {
            m_actionList.Add(action);
        }
    }
}
