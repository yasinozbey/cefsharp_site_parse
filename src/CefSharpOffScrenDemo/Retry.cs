using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CefSharpOffScrenDemo
{
    public static class Retry
    {
        public static void Do(Action action, TimeSpan retryInterval, int maxAttemptCount = 10)
        {
            int attempted = 0;
            while (true)
            {
                if (maxAttemptCount == attempted)
                    break;

                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }

                    attempted++;
                    action();
                    break;
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
