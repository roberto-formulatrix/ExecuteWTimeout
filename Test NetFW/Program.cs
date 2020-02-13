using System;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.LogVerbose("start!");
            var te = new TimeoutExecute2();
            te.OnErrorOccurred += Test.OnError;

            try
            {
                te.Execute(Test.Run, 2000);
            }
            catch (Exception e)
            {
                Log.LogVerbose("op failed! " + e.Message);
                Log.LogVerbose("SUCCESS");
            }

            Console.ReadLine();
        }
    }

    public class Log
    {
        public static void LogVerbose(String s)
        {
            Console.WriteLine(DateTime.Now.ToString("yyyyMMdd HH:mm:ss.FFF") + " " + s);
        }
    }

    class Test
    {
        private static object monitor = new object();

        public static void Run()
        {
            Log.LogVerbose("Xthread-> " + System.Threading.Thread.CurrentThread.Name);
            lock(monitor)
            {
                Log.LogVerbose("start sleep");
                for (int i = 0; i < 6; i++)
                {
                    Console.Write("  " + i);
                    System.Threading.Thread.Sleep(1000);
                }
                Console.WriteLine("");
                Log.LogVerbose("stop sleep -- if you see this, this would've been a deadlock");
            }
        }

        public static void OnError(object source, EventArgs args)
        {
            Log.LogVerbose("Killing!");
            lock (monitor)
            {
                Log.LogVerbose("KILLED!");
            }
        }
    }
}
