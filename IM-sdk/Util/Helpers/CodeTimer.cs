using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Util.Helpers
{
    public static class CodeTimer
    {
        public static void Initialize()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Time("", 1, () => { });
        }

        public static string Time(string name, int iteration, Action action)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            //  保留当前控制台前景色，并使用黄色输出名称参数。
            ConsoleColor currentForeColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(name);


            //  强制GC进行收集，并记录目前各代已经收集的次数。
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            int[] gcCount = new int[GC.MaxGeneration + 1];
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                gcCount[i] = GC.CollectionCount(i);
            }

            //  执行代码，记录下消耗的时间及CPU时钟周期。
            Stopwatch watch = new Stopwatch();
            watch.Start();
            ulong cycleCount = GetCycleCount();
            for (int i = 0; i < iteration; i++)
                action();
            ulong cpuCycles = GetCycleCount() - cycleCount;
            watch.Stop();

            //  恢复控制台默认前景色，并打印出消耗时间及CPU时钟周期。
            Console.ForegroundColor = currentForeColor;
          //  Console.WriteLine("\tTime Elapsed:\t" + watch.ElapsedMilliseconds.ToString("N0") + "ms");
         //   Console.WriteLine("\tCPU Cycles:\t" + cpuCycles.ToString("N0"));

            StringBuilder genbuilder = new StringBuilder();
            //  打印执行过程中各代垃圾收集回收次数。
            for (int i = 0; i <= GC.MaxGeneration; i++)
            {
                int count = GC.CollectionCount(i) - gcCount[i];
             //   Console.WriteLine("\tGen " + i + ": \t\t" + count);
                genbuilder.Append("\tGen " + i + ": \t\t" + count);
            }
            return $"{name}： \tTime Elapsed:\t{watch.ElapsedMilliseconds.ToString("N0")}ms \r\n\tCPU Cycles:\t{ cpuCycles.ToString("N0")} \r\n {genbuilder.ToString()}";
           
        }

        private static ulong GetCycleCount()
        {
            ulong cycleCount = 0;
            QueryThreadCycleTime(GetCurrentThread(), ref cycleCount);
            return cycleCount;
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryThreadCycleTime(IntPtr threadHandle, ref ulong cycleTime);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();
    }
}