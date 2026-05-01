using System;  
using System.Collections.Generic;  
using System.Diagnostics;  
using System.Linq;  
using System.Management;  
using System.Runtime.InteropServices;   
using System.Threading;  

namespace WinOSProject        
{  
    [StructLayout(LayoutKind.Sequential)]
    struct IO_COUNTERS
    {  
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;  
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    class SimProcess
    {
        public int    Id            { get; set; }
        public string Name          { get; set; } = "";
        public int    ArrivalTime   { get; set; }
        public int    BurstTime     { get; set; }  
        public int    Priority      { get; set; }
        public int    RemainingTime { get; set; }  
        public int    StartTime     { get; set; } = -1;
        public int    FinishTime    { get; set; } = -1;  

        // waiting time = finish - arrival - burst
        public int WaitingTime    => FinishTime - ArrivalTime - BurstTime;        
        public int TurnaroundTime => FinishTime - ArrivalTime;  
    } 

    class GanttEntry  
    {  
        public string Name  { get; set; } = "";  
        public int    Start { get; set; }
        public int    End   { get; set; }  
    }

    class FinalProject
    {
        [DllImport("kernel32.dll")]
        static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);        

        static void Main()
        {
            while (true)
            {
                Console.WriteLine();
                PrintHeader("Windows Process Manager");    
                Console.WriteLine("  1  List processes");
                Console.WriteLine("  2  Start a process");        
                Console.WriteLine("  3  Kill a process by PID");
                Console.WriteLine("  4  Change process priority by PID");
                Console.WriteLine("  5  System resource dashboard");
                Console.WriteLine("  6  CPU scheduling simulator");   
                Console.WriteLine("  7  Exit");
                Console.Write("Choose: ");

                var choice = Console.ReadLine()?.Trim();        

                if      (choice == "1") ListProcesses();
                else if (choice == "2") StartProcess();        
                else if (choice == "3") KillProcess();
                else if (choice == "4") ChangePriority();
                else if (choice == "5") ResourceDashboard();
                else if (choice == "6") SchedulingSimulator();
                else if (choice == "7") return;
                else Console.WriteLine("Invalid choice");   
            }
        }

        static void PrintHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("|---------------------------------------|");
            Console.WriteLine($"|  {title,-36}|");
            Console.WriteLine("|---------------------------------------|");            
            Console.ResetColor();
        }

        static string Trunc(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= n ? s : s.Substring(0, n - 1) + ".";        
        }

        static void Bar(double pct, int width = 30) 
        {
            int filled = (int)(pct / 100.0 * width); 
            filled = Math.Max(0, Math.Min(width, filled)); 

            Console.ForegroundColor = pct > 80 ? ConsoleColor.Red 
                                    : pct > 50 ? ConsoleColor.Yellow 
                                               : ConsoleColor.Green; 
            Console.Write("[" + new string('#', filled) + new string('-', width - filled) + $"] {pct,5:F1}%"); 
            Console.ResetColor();
        }

        //1. List Processes
        static void ListProcesses() 
        {
            var procs = Process.GetProcesses().OrderBy(p => p.ProcessName).ToList();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;         
            Console.WriteLine($"{"PID",-7} {"Name",-28} {"Priority",-14} {"MemMB",8} {"ReadMB",9} {"WriteMB",9}");   
            Console.WriteLine(new string('-', 80));  
            Console.ResetColor();

            foreach (var p in procs)
            {
                try
                {
                    double memMb  = p.WorkingSet64 / 1048576.0;        
                    double readMb = 0, writeMb = 0;

                    if (GetProcessIoCounters(p.Handle, out IO_COUNTERS io))                                    
                    {
                        readMb  = io.ReadTransferCount  / 1048576.0;           
                        writeMb = io.WriteTransferCount / 1048576.0;    
                    }

                    Console.WriteLine($"{p.Id,-7} {Trunc(p.ProcessName, 28),-28} {p.PriorityClass,-14} " +
                                      $"{memMb,8:F1} {readMb,9:F1} {writeMb,9:F1}");
                }
                catch { /* some system processes deny access */ }
            }
        }

        // 2. Start Process
        static void StartProcess()
        {    
            Console.Write("Enter full path or command (e.g. notepad): ");        
            var cmd = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(cmd)) return;        

            try        
            {
                var p = Process.Start(new ProcessStartInfo { FileName = cmd, UseShellExecute = true });
                if (p != null) Console.WriteLine($"Started PID {p.Id}");
            }        
            catch (Exception ex) { Console.WriteLine("Start failed: " + ex.Message); }
        }

        //3. Kill Process
        static void KillProcess()
        {
            Console.Write("Enter PID to kill: ");
            if (!int.TryParse(Console.ReadLine(), out int pid)) { Console.WriteLine("Bad PID"); return; }            

            try
            {
                var p = Process.GetProcessById(pid);
                Console.WriteLine($"Killing {p.ProcessName} PID {p.Id}");    
                p.Kill(true);
                Console.WriteLine("Killed.");
            }
            catch (Exception ex) { Console.WriteLine("Kill failed: " + ex.Message); }        
        }

        //4. Change Priority
        static void ChangePriority()        
        {
            Console.Write("Enter PID: ");
            if (!int.TryParse(Console.ReadLine(), out int pid)) { Console.WriteLine("Bad PID"); return; }

            Console.WriteLine("Priorities: Idle, BelowNormal, Normal, AboveNormal, High, RealTime");                
            Console.Write("Priority: ");
            var priStr = Console.ReadLine()?.Trim();

            if (!Enum.TryParse(priStr, true, out ProcessPriorityClass pri)) { Console.WriteLine("Bad priority"); return; }

            try
            {
                var p = Process.GetProcessById(pid);
                p.PriorityClass = pri;
                Console.WriteLine($"Updated {p.ProcessName} PID {p.Id} to {p.PriorityClass}");
            }
            catch (Exception ex) { Console.WriteLine("Priority change failed: " + ex.Message); }
        }

        // Resource Dashboard
        static void ResourceDashboard()
        {
            Console.Write("Live refresh? (y/n): ");
            bool live = Console.ReadLine()?.Trim().ToLower() == "y";

            int iterations = live ? 10 : 1;
            int intervalMs = 1500;

            // need to call NextValue once first or it returns 0
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            Thread.Sleep(500);

            for (int i = 0; i < iterations; i++)                
            {
                if (live)                
                {
                    Console.Clear();
                    Console.SetCursorPosition(0, 0);
                }

                PrintHeader("System Resource Dashboard");

                //CPU
                float cpu = cpuCounter.NextValue();
                Console.Write("  CPU Usage   : ");        
                Bar(cpu);
                Console.WriteLine();        

                //RAM
                // WMI returns KB so divide by 1024 to get MB
                double totalRam = 0, availRam = 0;
                try
                {
                    using var searcher = new ManagementObjectSearcher(        
                        "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");        
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        totalRam = Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024.0;
                        availRam = Convert.ToDouble(obj["FreePhysicalMemory"])     / 1024.0;            
                    }
                }
                catch { }

                double usedRam = totalRam - availRam;
                double ramPct  = totalRam > 0 ? usedRam / totalRam * 100.0 : 0;
                Console.Write("  RAM Used    : ");
                Bar(ramPct);
                // rounding issue maybe
                Console.WriteLine($"  ({usedRam:F0} MB / {totalRam:F0} MB)");

                // Disk I/O
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  Disk I/O:");
                Console.ResetColor();

                try
                {
                    using var diskSearcher = new ManagementObjectSearcher(
                        "SELECT Name, DiskReadBytesPersec, DiskWriteBytesPersec " +
                        "FROM Win32_PerfFormattedData_PerfDisk_LogicalDisk " +
                        "WHERE Name != '_Total'");
                    foreach (ManagementObject disk in diskSearcher.Get())
                    {
                        string dname   = disk["Name"]?.ToString() ?? "?";
                        double readKB  = Convert.ToDouble(disk["DiskReadBytesPersec"])  / 1024.0;
                        double writeKB = Convert.ToDouble(disk["DiskWriteBytesPersec"]) / 1024.0;
                        Console.WriteLine($"    Drive {dname,-3}  Read: {readKB,7:F1} KB/s   Write: {writeKB,7:F1} KB/s");
                    }
                }
                catch
                {
                    Console.WriteLine("    (Disk stats unavailable - try running as Administrator)");
                }

                //Top 5 by memory
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  Top 5 processes by memory usage:");
                Console.ResetColor();

                var top5 = Process.GetProcesses()
                    .OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0L; } })
                    .Take(5);

                int rank = 1;
                foreach (var p in top5)
                {
                    try
                    {
                        double mb = p.WorkingSet64 / 1048576.0;
                        Console.WriteLine($"    {rank}.  {Trunc(p.ProcessName, 22),-22}  {mb,7:F1} MB");
                        rank++;
                    }
                    catch { }
                }

                //Network
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("  Network interfaces:");
                Console.ResetColor();

                try
                {
                    using var netSearcher = new ManagementObjectSearcher(
                        "SELECT Name, BytesTotalPersec FROM Win32_PerfFormattedData_Tcpip_NetworkInterface");
                    foreach (ManagementObject net in netSearcher.Get())
                    {
                        string nname = Trunc(net["Name"]?.ToString() ?? "?", 35);
                        double kbps  = Convert.ToDouble(net["BytesTotalPersec"]) / 1024.0;
                        if (kbps > 0.01)
                            Console.WriteLine($"    {nname,-35}  {kbps,7:F1} KB/s");
                    }
                }
                catch { Console.WriteLine("    (Network data unavailable)"); }

                if (live && i < iterations - 1)
                {
                    Console.WriteLine("\n  Refreshing in 1.5 seconds...");
                    Thread.Sleep(intervalMs);
                }
            }
        }

        //6. Scheduling Simulator-fomatting error and bugs fix later
        static void SchedulingSimulator()
        {
            PrintHeader("CPU Scheduling Simulator");
            Console.WriteLine("  1  First-Come First-Served (FCFS)");
            Console.WriteLine("  2  Shortest Job First (SJF)");
            Console.WriteLine("  3  Round Robin (RR)");
            Console.WriteLine("  4  Priority Scheduling");
            Console.Write("Choose algorithm: ");

            var alg = Console.ReadLine()?.Trim();
            if (alg is not ("1" or "2" or "3" or "4")) { Console.WriteLine("Invalid choice."); return; }

            int quantum = 2;
            if (alg == "3")
            {
                Console.Write("Enter time quantum (default 2): ");
                var q = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(q) && int.TryParse(q, out int qv) && qv > 0) quantum = qv;
            }

            Console.Write("Number of processes (2-10): ");
            if (!int.TryParse(Console.ReadLine(), out int n) || n < 2 || n > 10)
            { Console.WriteLine("Invalid."); return; }

            var processes     = new List<SimProcess>();
            bool needPriority = alg == "4";

            Console.WriteLine();
            for (int i = 0; i < n; i++)
            {
                var sp = new SimProcess { Id = i + 1, Name = $"P{i + 1}" };

                Console.Write($"  {sp.Name} arrival time : ");
                if (!int.TryParse(Console.ReadLine(), out int arr) || arr < 0) arr = 0;
                sp.ArrivalTime = arr;

                Console.Write($"  {sp.Name} burst time   : ");
                if (!int.TryParse(Console.ReadLine(), out int burst) || burst < 1) burst = 1;
                sp.BurstTime     = burst;
                sp.RemainingTime = burst;

                if (needPriority)
                {
                    Console.Write($"  {sp.Name} priority (1=high) : ");
                    if (!int.TryParse(Console.ReadLine(), out int pri) || pri < 1) pri = 1;
                    sp.Priority = pri;
                }

                processes.Add(sp);
                Console.WriteLine();
            }

            List<GanttEntry> gantt = alg switch
            {
                "1" => RunFCFS(processes),
                "2" => RunSJF(processes),
                "3" => RunRoundRobin(processes, quantum),
                "4" => RunPriority(processes),
                _   => new List<GanttEntry>()
            };

            Console.WriteLine();
            PrintGantt(gantt);
            PrintStats(processes, n);
        }

        // FCFS
        static List<GanttEntry> RunFCFS(List<SimProcess> procs)//fomatting issue 
        {
            var gantt   = new List<GanttEntry>();
            var ordered = procs.OrderBy(p => p.ArrivalTime).ToList();
            int time    = 0;

            foreach (var p in ordered)
            {
                if (time < p.ArrivalTime) time = p.ArrivalTime;
                p.StartTime  = time;
                p.FinishTime = time + p.BurstTime;
                gantt.Add(new GanttEntry { Name = p.Name, Start = time, End = p.FinishTime });
                time = p.FinishTime;
            }
            return gantt;
        }

        //SJF non-preemptive
        static List<GanttEntry> RunSJF(List<SimProcess> procs)
        {
            var gantt     = new List<GanttEntry>();
            var remaining = procs.ToList();
            int time      = 0;

            while (remaining.Count > 0)
            {
                var ready = remaining.Where(p => p.ArrivalTime <= time).ToList();

                if (!ready.Any())
                {
                    time = remaining.Min(p => p.ArrivalTime);
                    continue;
                }

                var p = ready.OrderBy(x => x.BurstTime).First();
                p.StartTime  = time;
                p.FinishTime = time + p.BurstTime;
                gantt.Add(new GanttEntry { Name = p.Name, Start = time, End = p.FinishTime });
                time = p.FinishTime;
                remaining.Remove(p);
            }
            return gantt;
        }

        // Round Robin
        static List<GanttEntry> RunRoundRobin(List<SimProcess> procs, int quantum)
        {
            var gantt   = new List<GanttEntry>();
            var queue   = new Queue<SimProcess>();
            var pending = procs.OrderBy(p => p.ArrivalTime).ToList();
            int time    = 0;

            foreach (var p in pending.Where(p => p.ArrivalTime <= time).ToList())//some error need to fix later 
            { queue.Enqueue(p); pending.Remove(p); }

            while (queue.Count > 0 || pending.Count > 0)
            {
                if (queue.Count == 0)
                {
                    time = pending.First().ArrivalTime;
                    foreach (var p in pending.Where(p => p.ArrivalTime <= time).ToList())
                    { queue.Enqueue(p); pending.Remove(p); }
                    continue;
                }

                var curr = queue.Dequeue();
                if (curr.StartTime == -1) curr.StartTime = time;

                int slice = Math.Min(quantum, curr.RemainingTime);
                gantt.Add(new GanttEntry { Name = curr.Name, Start = time, End = time + slice });
                time += slice;
                curr.RemainingTime -= slice;

                foreach (var np in pending.Where(p => p.ArrivalTime <= time).ToList())
                { queue.Enqueue(np); pending.Remove(np); }

                if (curr.RemainingTime > 0)
                    queue.Enqueue(curr);
                else
                    curr.FinishTime = time;
            }
            return gantt;
        }

        // Priority non-preemptive
        static List<GanttEntry> RunPriority(List<SimProcess> procs)
        {
            var gantt     = new List<GanttEntry>();
            var remaining = procs.ToList();
            int time      = 0;

            while (remaining.Count > 0)
            {
                var ready = remaining.Where(p => p.ArrivalTime <= time).ToList();
                if (!ready.Any()) { time = remaining.Min(p => p.ArrivalTime); continue; }

                var p = ready.OrderBy(x => x.Priority).First();
                p.StartTime  = time;
                p.FinishTime = time + p.BurstTime;
                gantt.Add(new GanttEntry { Name = p.Name, Start = time, End = p.FinishTime });
                time = p.FinishTime;
                remaining.Remove(p);
            }
            return gantt;
        }

        // Gantt chart
        static void PrintGantt(List<GanttEntry> gantt)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Gantt Chart:");  
            Console.ResetColor();  

            Console.Write("  ");  
            foreach (var e in gantt)
                Console.Write("+" + new string('-', e.End - e.Start));  
            Console.WriteLine("+");  
 
            Console.Write("  ");
            foreach (var e in gantt)  
            {
                int w     = e.End - e.Start;  
                string lbl = e.Name.Length <= w ? e.Name.PadRight(w) : e.Name.Substring(0, w);
                Console.ForegroundColor = ConsoleColor.Yellow;  
                Console.Write("|" + lbl);
                Console.ResetColor();
            }
            Console.WriteLine("|");

            Console.Write("  ");        
            foreach (var e in gantt)  
                Console.Write("+" + new string('-', e.End - e.Start));        
            Console.WriteLine("+");   

            // time row - spacing gets a little off when times are 2+ digits        
            Console.Write("  " + gantt.First().Start);   
            foreach (var e in gantt)        
            {
                int w = e.End - e.Start;   
                Console.Write(e.End.ToString().PadLeft(w + 1));        
            }
            Console.WriteLine();   
        }

        //Stats table        
        static void PrintStats(List<SimProcess> procs, int totalCount)  
        {
            Console.WriteLine();  
            Console.ForegroundColor = ConsoleColor.White;  
            Console.WriteLine($"  {"Process",-10} {"Arrival",8} {"Burst",7} {"Start",7} {"Finish",8} {"Wait",6} {"TAT",6}");  
            Console.WriteLine("  " + new string('-', 58));  
            Console.ResetColor();  

            double totalWait = 0, totalTAT = 0;  

            foreach (var p in procs.OrderBy(x => x.Id))  
            {
                if (p.FinishTime < 0) continue;  
                Console.WriteLine($"  {p.Name,-10} {p.ArrivalTime,8} {p.BurstTime,7} " +  
                                  $"{p.StartTime,7} {p.FinishTime,8} {p.WaitingTime,6} {p.TurnaroundTime,6}");  
                totalWait += p.WaitingTime;  
                totalTAT  += p.TurnaroundTime;  
            }

            Console.WriteLine();  
            Console.ForegroundColor = ConsoleColor.Green;  
            // dividing by totalCount     
            // works fine in normal cases    
            Console.WriteLine($"  Avg Waiting Time    : {totalWait / totalCount:F2}"); 
            Console.WriteLine($"  Avg Turnaround Time : {totalTAT  / totalCount:F2}"); 
            Console.ResetColor(); 
        }
    }
}
