using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

/***
 * Executer relies on thread.Abort which does not work in dot net crore
 */
namespace Test
{
    class TimeoutExecute2
    {
        public event EventHandler OnErrorOccurred = delegate { };
        private Thread _curThread;

        public void Execute(Action action, int timeout)
        {
            for (int i = 0; i < 3; i++)
            {
                _curThread = new Thread(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Log.LogVerbose("aborting " + e.GetType().Name + "  " + e.Message);
                    }

                });
                _curThread.Name = "ExecWTimeout";

                _curThread.Start();
                if (!_curThread.Join(timeout))
                {
                    Log.LogVerbose("aborting...");
                    _curThread.Abort();
                    OnErrorOccurred(this, null);
                }
                else
                    return;
            }

            throw new Exception("Failed after 3 times");
        }

    }
}
