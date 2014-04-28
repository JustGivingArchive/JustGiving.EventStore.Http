using System;
using log4net;

namespace JustGiving.EventStore.Http.Client.Common.Utils
{
    public static class Log
    {
        public static void Info(ILog log, string format, params object[] @params)
        {
            if (log == null)
            {
                return;
            }

            log.InfoFormat(format, @params);
        }

        public static void Warning(ILog log, string format, params object[] @params)
        {
            if (log == null)
            {
                return;
            }

            log.WarnFormat(format, @params);
        }

        public static void Error(ILog log, string format, params object[] @params)
        {
            if (log == null)
            {
                return;
            }

            log.ErrorFormat(format, @params);
        }

        public static void Error(ILog log, Exception exception, string format, params object[] @params)
        {
            if (log == null)
            {
                return;
            }

            log.Error(string.Format(format, @params), exception);
        }
    }
}