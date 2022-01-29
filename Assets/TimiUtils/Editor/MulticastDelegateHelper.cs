using System;
using System.Collections.Generic;

namespace Utils
{
    public static class MulticastDelegateHelper
    {
        private static readonly Dictionary<string, Delegate> delegateStore = new Dictionary<string, Delegate>();

        /// <remarks>
        /// `InitializeOnLoadMethod` has no opposite one for uninitializing. When mucking around with code that registers
        /// event handlers, older versions of the event handlers stay registered. A workaround is to remove prior ones
        /// when adding.
        /// </remarks>
        public static void Register<T>(ref T targetMulticastDelegate, string key, T targetDelegate) where T:Delegate
        {
            if (delegateStore.TryGetValue(key, out Delegate current))
            {
                targetMulticastDelegate = (T)Delegate.Remove(targetMulticastDelegate, current);
            }

            targetMulticastDelegate = (T)Delegate.Combine(targetMulticastDelegate, targetDelegate);
            delegateStore[key] = targetDelegate;
        }
    }
}
