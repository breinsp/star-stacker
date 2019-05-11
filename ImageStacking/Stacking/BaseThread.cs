using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageStacking.Stacking
{
    public abstract class BaseThread
    {
        public Thread thread;
        public bool isRunning;
        public bool working;

        public int id;

        public abstract void Action();

        public void Start()
        {
            isRunning = true;
            thread = new Thread(() =>
            {
                while (isRunning)
                {
                    try
                    {
                        Action();
                    }
                    catch (Exception ex)
                    {
                        Debug(ex.Message);
                        Debug(ex.StackTrace);
                    }
                }
            });
            thread.Priority = System.Threading.ThreadPriority.Normal;
            thread.Start();
        }

        public void Stop()
        {
            isRunning = false;
        }

        internal void Debug(string msg)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Thread " + id + ": " + msg);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
