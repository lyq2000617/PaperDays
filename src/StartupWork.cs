using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PaperDays
{
    // Explorer COM calls may be slow at logon. Never execute them on the UI thread.
    public sealed class SerialShellWorker : IDisposable
    {
        readonly BlockingCollection<Action> jobs = new BlockingCollection<Action>();
        readonly Thread thread;
        /// <summary>
        /// 初始化StartupWork的SerialShellWorker，准备状态、布局与必要的事件绑定。
        /// </summary>
        public SerialShellWorker()
        {
            thread = new Thread(delegate ()
            {
                foreach (var job in jobs.GetConsumingEnumerable())
                    job();
                jobs.Dispose();
            });
            thread.IsBackground = true;
            thread.Name = "PaperDays wallpaper";
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>
        /// 执行任务并返回结果，异常交由调用方处理。
        /// </summary>
        public Task Run(Action action)
        {
            return Run<int>(delegate
            {
                action();
                return 0;
            });
        }

        /// <summary>
        /// 执行任务并返回结果，异常交由调用方处理。
        /// </summary>
        public Task<T> Run<T>(Func<T> action)
        {
            var result = new TaskCompletionSource<T>();
            jobs.Add(delegate
            {
                try
                {
                    result.SetResult(action());
                }
                catch (Exception ex)
                {
                    result.SetException(ex);
                }
            });
            return result.Task;
        }

        /// <summary>
        /// 释放控件、计时器、图形和系统资源，并调用基类清理。
        /// </summary>
        public void Dispose()
        {
            jobs.CompleteAdding();
        }
    }

    public static class StartupTiming
    {
        static readonly object gate = new object ();
        static System.Diagnostics.Stopwatch watch;
        /// <summary>
        /// 初始化处理状态或计时记录。
        /// </summary>
        public static void Start()
        {
            lock (gate)
            {
                watch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    Directory.CreateDirectory(Data.Root);
                    File.WriteAllText(Path.Combine(Data.Root, "startup-timing.txt"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | PaperDays " + Program.Version + " | " + Environment.OSVersion + Environment.NewLine);
                }
                catch
                {
                }
            }

            Mark("Process started");
        }

        /// <summary>
        /// 记录阶段耗时，不写入日程或待办正文。
        /// </summary>
        public static void Mark(string stage)
        {
            lock (gate)
            {
                if (watch == null)
                    return;
                try
                {
                    File.AppendAllText(Path.Combine(Data.Root, "startup-timing.txt"), watch.ElapsedMilliseconds + " ms | " + stage + Environment.NewLine);
                }
                catch
                {
                }
            }
        }
    }
}