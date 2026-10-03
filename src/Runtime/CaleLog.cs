using System;

namespace CaleHenituseAbnormality
{
    internal static class CaleLog
    {
        public static void Info(string message)
        {
            try
            {
                UnityEngine.Debug.Log("[CaleHenituse] " + message);
            }
            catch
            {
            }
        }

        public static void Exception(string message, Exception exception)
        {
            try
            {
                UnityEngine.Debug.LogError(
                    "[CaleHenituse] " + message + Environment.NewLine + exception);
            }
            catch
            {
            }
        }
    }
}
