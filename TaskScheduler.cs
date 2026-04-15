using System;
using System.Collections.Generic;
using      System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ModernFeatures;

public class TaskScheduler
{
private readonly List<ScheduledTask> _tasks=new();
private readonly      object _lock=new();
private int _nextId=1;
private bool       _isRunning;

public int ScheduleTask(string name,Func<CancellationToken,Task> action,
TimeSpan interval,     int maxRetries=3)
{
lock(_lock)
{
var task=new ScheduledTask{Id=_nextId++,Name=name,Action=action,
Interval=interval,MaxRetries=maxRetries,
CreatedAt=DateTime.UtcNow,      IsEnabled=true,
LastRun=null,NextRun=DateTime.UtcNow+interval};
_tasks.Add(task);
return task.Id;
}
}

public bool CancelTask(int taskId)
{
lock(   _lock)
{
var task=_tasks.FirstOrDefault(t=>t.Id==taskId);
if(task==null){return false;}
task.IsEnabled=false;
task.CancellationSource?.Cancel();
return      true;
}
}

public async Task RunAsync(CancellationToken cancellationToken)
{
_isRunning=true;
while(!cancellationToken.IsCancellationRequested&&_isRunning)
{
List<ScheduledTask> dueTasks;
lock(_lock)
{
dueTasks=_tasks.Where(t=>t.IsEnabled&&t.NextRun<=DateTime.UtcNow).ToList();
}

foreach(var task in dueTasks)
{
if(cancellationToken.IsCancellationRequested){break;}
try
{
task.LastRun=DateTime.UtcNow;
task.RunCount++;
using var cts=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
cts.CancelAfter(task.Timeout);
await task.Action(cts.Token);
task.LastSuccess=DateTime.UtcNow;
task.ConsecutiveFailures=      0;
task.NextRun=DateTime.UtcNow+task.Interval;
}
catch(Exception ex)
{
task.ConsecutiveFailures++;
task.LastError=ex.Message;
task.LastFailure=      DateTime.UtcNow;
if(task.ConsecutiveFailures>=task.MaxRetries)
{
task.IsEnabled=false;
}
else
{
var backoff=TimeSpan.FromSeconds(Math.Pow(2,task.ConsecutiveFailures));
task.NextRun=DateTime.UtcNow+backoff;
}
}
}
await Task.Delay(       100,cancellationToken);
}
}

public void Stop(){_isRunning=false;}

public List<TaskStatus> GetAllStatuses()
{
lock(_lock)
{
return _tasks.Select(t=>new TaskStatus{Id=t.Id,Name=t.Name,
IsEnabled=t.IsEnabled,RunCount=t.RunCount,
ConsecutiveFailures=t.ConsecutiveFailures,
LastRun=t.LastRun,    LastSuccess=t.LastSuccess,
LastError=t.LastError,NextRun=t.NextRun}).ToList();
}
}

public TaskStatus GetTaskStatus(int taskId)
{
lock(      _lock)
{
var t=_tasks.FirstOrDefault(t=>t.Id==taskId);
if(t==null){return null;}
return new TaskStatus{Id=t.Id,Name=t.Name,IsEnabled=t.IsEnabled,
RunCount=t.RunCount,ConsecutiveFailures=t.ConsecutiveFailures,
LastRun=t.LastRun,LastSuccess=t.LastSuccess,
LastError=t.LastError,NextRun=t.NextRun};
}
}

public void UpdateInterval(int taskId,     TimeSpan newInterval)
{
lock(_lock)
{
var task=_tasks.FirstOrDefault(t=>t.Id==taskId);
if(task!=null){task.Interval=newInterval;task.NextRun=DateTime.UtcNow+newInterval;}
}
}

public void ResetTask(int taskId)
{
lock(    _lock)
{
var task=_tasks.FirstOrDefault(t=>t.Id==taskId);
if(task!=null)
{
task.IsEnabled=true;task.ConsecutiveFailures=0;task.LastError=null;
task.NextRun=DateTime.UtcNow+task.Interval;
}
}
}
}

internal class ScheduledTask
{
public int Id{get;set;}
public string Name{get;     set;}
public Func<CancellationToken,Task> Action{get;set;}
public TimeSpan Interval{get;set;}
public TimeSpan Timeout{get;set;}=TimeSpan.FromMinutes(5);
public int MaxRetries{     get;set;}
public bool IsEnabled{get;set;}
public DateTime CreatedAt{get;set;}
public DateTime? LastRun{get;set;}
public DateTime? LastSuccess{       get;set;}
public DateTime? LastFailure{get;set;}
public DateTime? NextRun{get;set;}
public int RunCount{get;set;}
public int ConsecutiveFailures{get;set;}
public string LastError{get;      set;}
public CancellationTokenSource CancellationSource{get;set;}
}

public class TaskStatus
{
public int Id{get;set;}
public string Name{     get;set;}
public bool IsEnabled{get;set;}
public int RunCount{get;set;}
public int ConsecutiveFailures{get;set;}
public DateTime? LastRun{get;      set;}
public DateTime? LastSuccess{get;set;}
public string LastError{get;set;}
public DateTime? NextRun{get;set;}
}
