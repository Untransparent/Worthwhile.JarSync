
using Microsoft.Extensions.Logging;

namespace Worthwhile.JarSync.Core.Source
{
    public interface IEventLogger
    {
        void LogInformation(string aMsg);
        void LogError(string aMsg);
        void LogDebug(string aMsg);
    }

    public class ConcurrentEventLogger : IEventLogger
    {
        private ILogger<ConcurrentEventLogger> mLogger = null!;

        public ConcurrentEventLogger(ILogger<ConcurrentEventLogger> aLogger)
        {
            mLogger = aLogger;
        }

        private object mLock = new object();

        public void LogInformation(string aMsg)
        {
            lock (mLock)
            {
                mLogger.LogInformation(aMsg);
            }
        }
        public void LogDebug(string aMsg)
        {
            lock (mLock)
            {
                mLogger.LogDebug(aMsg);
            }
        }
        public void LogError(string aMsg)
        {
            lock (mLock)
            {
                mLogger.LogError(aMsg);
            }
        }
    }
}