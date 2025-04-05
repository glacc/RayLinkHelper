using System.Diagnostics;
using System.Runtime.Versioning;
using System.Numerics;

namespace RayLinkHelper
{
    [SupportedOSPlatform("Windows")]
    internal class Program
    {
        /// <summary>
        /// 程序终止标记，volatile防止多线程下的变量重排
        /// </summary>
        static volatile bool terminateFlag = false;

        /// <summary>
        /// 功能是否启用的标记
        /// </summary>
        static volatile bool enabled = false;

        /// <summary>
        /// 检查间隔，单位ms。
        /// </summary>
        const int checkIntervalMs = 20;

        static void OnToggleEnable(object? sender, bool enabledFromSender)
        {
            enabled = enabledFromSender;
        }

        /// <summary>
        /// 为了创建系统托盘图标NotifyIcon，而NotifyIcon为窗体Form的控件，因此先创建窗口，而且创建窗口的线程为单独的STA线程。
        /// </summary>
        static void ThreadAppWindow()
        {
            NotifyForm notifyForm = new NotifyForm();
            notifyForm.onToggleEnable += OnToggleEnable;

            Application.Run(notifyForm);

            terminateFlag = true;
        }

        static void Main(string[] args)
        {
            // 设置类似于画笔平滑的队列长度，越高则平滑程度越高。
            const int maxQueueLength = 5;

            // 当鼠标移动幅度的平均值大于此阈值时则最小化。
            const float minimizeMouseDeltaThreshold = 50f;

            // 启动用于创建托盘图标的窗口
            Thread threadAppWindow = new Thread(ThreadAppWindow);
            threadAppWindow.SetApartmentState(ApartmentState.STA);
            threadAppWindow.Start();

            // 此次和上次聚焦的窗口
            IntPtr prevForeground = IntPtr.Zero;
            IntPtr currForeground = IntPtr.Zero;

            // 当前聚焦窗口对应的进程
            Process? currentProcess = null;

            bool isRayLinkFocused = false;

            // 用于储存鼠标移动幅度的队列
            Queue<Vector2> mouseDeltaQueue = new Queue<Vector2>();

            while (!terminateFlag)
            {
                // 通过Win32获取当前聚焦窗口的句柄，然后通过句柄获得窗口所属进程的PID
                // 再通过Process.GetProcessById()获得其进程信息，通过其程序名字确定是否为需要最小化的窗口
                //
                // 局限：无法判断具体是哪个窗口要最小化，只要exe文件名符合，都会被快速移动鼠标最小化

                currForeground = Win32.GetForegroundWindow();

                // 若此次检测和上次的窗口句柄不相符，说明焦点发生变化
                if (currForeground != prevForeground)
                {
                    int processId;
                    Win32.GetWindowThreadProcessId(currForeground, out processId);

                    currentProcess?.Dispose();
                    currentProcess = Process.GetProcessById(processId);

                    // 由于FileName返回的是程序的整个路径，因此按'/'或'\'拆开，取最后一个，即为程序名。然后比对是否为需要最小化的程序
                    string? currentProcessExeName = currentProcess?.MainModule?.FileName.Replace('\\', '/').Split('/').Last();
                    if (currentProcessExeName != null)
                        isRayLinkFocused = currentProcessExeName.ToLower() == "raylink.exe";
                    else
                        isRayLinkFocused = false;

#if DEBUG
                    Console.WriteLine($"{prevForeground} -> {currForeground} ({currentProcessExeName})");
#endif
                }

                // 不管符不符合条件，都先拉去鼠标移动状态，清空缓冲区。
                // 若短时间内鼠标移动幅度大于阈值，则最小化符合条件的窗口。
                (int mouseDeltaX, int mouseDeltaY) = InputHandling.PollMouseInput();
                if (isRayLinkFocused && enabled)
                {
                    Vector2 mouseDeltaCurr = new Vector2(mouseDeltaX, mouseDeltaY);
                    mouseDeltaQueue.Enqueue(mouseDeltaCurr);

                    if (mouseDeltaQueue.Count > maxQueueLength)
                        mouseDeltaQueue.Dequeue();

                    Vector2 mouseDeltaMean = Vector2.Zero;

                    foreach (Vector2 mouseDelta in mouseDeltaQueue)
                        mouseDeltaMean += mouseDelta;

                    mouseDeltaMean /= mouseDeltaQueue.Count;

                    float mouseDeltaMeanLength = mouseDeltaMean.Length();

                    if (mouseDeltaMeanLength > minimizeMouseDeltaThreshold)
                    {
                        const int SW_MINIMIZE = 6;
                        Win32.ShowWindow(currForeground, SW_MINIMIZE);
                        /*
                        mouseDeltaQueue.Clear();
                        isRayLinkFocused = false;
                        */
                    }

#if DEBUG
                    if (mouseDeltaX != 0 || mouseDeltaY != 0)
                        Console.WriteLine($"dX: {mouseDeltaX}, dY: {mouseDeltaY}; dMean: {mouseDeltaMeanLength, 2}");
#endif
                }
                else
                    mouseDeltaQueue.Clear();

                prevForeground = currForeground;

                Thread.Sleep(checkIntervalMs);
            }
        }
    }
}
