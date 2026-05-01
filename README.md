# Windows Process Manager (COP 5611)

This project is a Windows console application built for COP 5611 (Operating Systems Design Principles).  
It is a mini Task Manager style tool that lets you view and control running processes from the terminal, and it also includes a system resource dashboard plus a CPU scheduling simulator.

## Features

When you run the program, you will see this menu:

|---------------------------------------|
|  Windows Process Manager              |
|---------------------------------------|
  1  List processes
  2  Start a process
  3  Kill a process by PID
  4  Change process priority by PID
  5  System resource dashboard
  6  CPU scheduling simulator
  7  Exit

### Option 1: List processes
Shows active processes with useful fields like:
- PID
- Process name
- Priority class
- Memory usage (MB)
Some processes may be skipped due to Windows permission restrictions.

### Option 2: Start a process
Launches a program from the console (example: notepad) and prints its PID.

### Option 3: Kill a process by PID
Terminates a process using its PID. If the process is protected or already closed, the program prints an error message instead of crashing.

### Option 4: Change process priority by PID
Changes the Windows priority class for a process (Idle, Normal, AboveNormal, High, RealTime, etc).  
This affects how Windows tends to schedule CPU time for that process under load.

### Option 5: System resource dashboard
Displays live system usage such as CPU percent and memory usage, and may include disk and network activity depending on how it is implemented.  
This is meant to feel like a lightweight system monitor.

### Option 6: CPU scheduling simulator
Simulates classic scheduling algorithms (example: FCFS, SJF, Round Robin, Priority) using user input processes with arrival time and burst time.  
Outputs a simple timeline (Gantt style) plus waiting time and turnaround time and averages.

### Option 7: Exit
Closes the program cleanly.

## Tech Used
- Language: C#
- Platform: .NET (Console App)
- OS: Windows 10 or Windows 11
- Main API: System.Diagnostics.Process
- For dashboard/simulation: depends on implementation (example: performance counters / WMI)

## Requirements
- Windows 10 or Windows 11
- .NET SDK installed

How to Run
Open the project folder in VS Code, or open a terminal inside the folder.
Run this command:
dotnet run

Quick Demo Steps suggested for class
Start the program by running:
dotnet run

Press 1 to list processes.

Press 2 to start Notepad.
Type: notepad
Write down the PID that prints.

Press 1 again and find Notepad in the list using the same PID.

Press 4 and enter the Notepad PID.
Set the priority to: AboveNormal

Press 5 to show the system resource dashboard for a few refresh cycles.

Press 6 and run one scheduling algorithm, then run another one to compare the results.

Press 3 and kill Notepad using its PID.

Press 7 to exit.

Notes and Limitations
Windows blocks access to some system processes, so listing fields or changing priority may fail with Access Denied.
A PID can disappear if a process closes between listing and selecting it.
The scheduling simulator is a user space simulation, it does not change the real Windows kernel scheduler.

Suggested Screenshots for the Repo
Main menu screen
Process list output showing PID, priority, and memory
Start Notepad output showing the PID
Priority change output
System resource dashboard output
Scheduling simulator output showing the timeline and averages

Author
Nikul Halai
