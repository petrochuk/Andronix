using Microsoft.Graph.Beta.Models;
using System.Diagnostics.CodeAnalysis;

namespace Andronix.Core.Graph;

[DebuggerDisplay("{TodoTask.Title}")]
public class TaskInList
{
    [SetsRequiredMembers]
    public TaskInList(TodoTask todoTask, string? listId)
    {
        TodoTask = todoTask ?? throw new ArgumentNullException(nameof(todoTask));
        ListId = listId ?? throw new ArgumentNullException(nameof(listId));
    }

    public required TodoTask TodoTask { get; init; }
    public required string ListId { get; init; }
}
