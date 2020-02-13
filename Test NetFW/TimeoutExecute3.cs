using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

/***
 * Executer relies on Task.Wait(timeout) which does not work in dot net crore
 */
namespace Test
{
    class TimeoutExecute3
    {
        public event EventHandler OnErrorOccurred = delegate { };

        private readonly System.Timers.Timer _watchdogTimer = new System.Timers.Timer() { Interval = 1000, AutoReset = false };
        private readonly AutoResetEvent _watchdogReset = new AutoResetEvent(false);
        private readonly object _watchdogLock = new object();
        private readonly int _watchdogSettleMillis = 45000;
        private long _watchdogBeginTicks = 0;
        private int _watchdogTimeoutMillis = 0;
        private ulong _watchdogCount = 0;

        private void StartWatchdog(int timeout)
        {
            lock (_watchdogLock)
            {
                _watchdogBeginTicks = DateTimeOffset.Now.Ticks;
                _watchdogTimeoutMillis = timeout;
                _watchdogTimer.Elapsed += WatchdogElapsed;
                _watchdogTimer.Start();
            }
        }

        private void StopWatchdog()
        {
            lock (_watchdogLock)
            {
                _watchdogTimeoutMillis = 0;
            }
        }

        private void KickDog()
        {
            lock (_watchdogLock)
            {
                _watchdogTimeoutMillis = -1; // Trigger immediate reset
            }
        }

        public void Execute(Action action, int timeout)
        {
            for (int i = 0; i < 3; i++)
            {
                var cts = new CancellationTokenSource();

                Log.LogVerbose("try " + i);
                var t = new Task(
                    () =>
                    {
                        try
                        {
                            using (cts.Token.Register(Thread.CurrentThread.Abort))
                                action();
                        }
                        catch (Exception e)
                        {
                            Log.LogVerbose(e.GetType().Name + "   " +  e.Message);
                        }
                    }, cts.Token);
                t.Start();

                try
                {
                    if (t.Wait(timeout))
                        return;
                    else
                        cts.Cancel();
                }
                catch (AggregateException e)
                {
                    Log.LogVerbose("----<> " + e.InnerExceptions[0].Message);
                }
            }

            throw new Exception("Failed after 3 times");
        }

        private void WatchdogElapsed(object sender, System.Timers.ElapsedEventArgs args)
        {
            try
            {
                var timeoutMillis = 0;
                var settleMillis = 0;
                var elapsedTicks = 0L;
                var timeoutTicks = 0L;
                var count = 0UL;

                lock (_watchdogLock)
                {
                    timeoutMillis = _watchdogTimeoutMillis;
                    settleMillis = _watchdogSettleMillis;
                    elapsedTicks = DateTimeOffset.Now.Ticks - _watchdogBeginTicks;
                    timeoutTicks = _watchdogTimeoutMillis * TimeSpan.TicksPerMillisecond;
                    count = ++_watchdogCount;
                }

                if (count % 60 == 0)
                {
                    Log.LogVerbose($"nameof(SpinCamera), Watchdog heartbeat (count: {count}, elapsed (ticks): {elapsedTicks}, timeout (ticks): {timeoutTicks})");
                }

                if (timeoutMillis != 0 && elapsedTicks > timeoutTicks)
                {
                    Log.LogVerbose("Ouch dog!");

                    string error = null;

                    if (timeoutMillis > 0)
                    {
                        //                        Log.LogPrefix(_logPrefix, $"Resetting camera (reason: {"not responding"}, timeout (ms): {timeoutMillis})");

                        error = "Not responding";
                    }
                    else
                    {
                        //                        Log.LogPrefix(_logPrefix, $"Resetting camera (reason: {"error"})");

                        error = "some error"; // _cameraLib.GetLastError();
                    }

                    OnErrorOccurred(this, null);

                    //                  _cameraLib.Kill();

                    //                  Log.LogPrefix(_logPrefix, $"Waiting to settle (duration (ms): {settleMillis})");

                    Thread.Sleep(settleMillis);

                    lock (_watchdogLock)
                    {
                        _watchdogTimeoutMillis = 0;
                    }

                    _watchdogReset.Set();
                }
            }
            catch (Exception e)
            {
                //            Log.ErrorPrefix(_logPrefix, e, e.Message);
            }
            finally
            {
                _watchdogTimer.Start();
            }
        }

    }
}
