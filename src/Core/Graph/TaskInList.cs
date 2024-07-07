using Microsoft.Graph.Beta.Models;
using System.Diagnostics.CodeAnalysis;

namespace Andronix.Core.Graph;

[DebuggerDisplay("{Task.Title} {TaskList.DisplayName}")]
public class TaskInList
{
    [SetsRequiredMembers]
    public TaskInList(TodoTask task, TodoTaskList taskList)
    {
        Task = task ?? throw new ArgumentNullException(nameof(task));
        TaskList = taskList ?? throw new ArgumentNullException(nameof(taskList));
    }

    public required TodoTask Task { get; init; }
    public required TodoTaskList TaskList { get; init; }
}
