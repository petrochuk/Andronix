using Andronix.Core.Extensions;
using Andronix.Core.Graph;
using Andronix.Interfaces;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Andronix.AssistantAI.Tools;

public class Tasks : ISpecializedAssistant
{
    #region Constants
    
    public const string TaskListNameDescription = "Task list name such as 'Flagged Emails', 'Tasks' or other task lists the person created";
    public const string TaskStatusList = "examples: notStarted, inProgress, completed, waitingOnOthers, deferred or empty";
    public const string TaskTitle = "Title";
    public const string TaskContent = "Content";
    public const string DefaultTaskListName = "Tasks";
    public const string LinkedOutlook = "Outlook";

    #endregion

    #region Fields & Constructors

    GraphServiceClient _graphClient;
    TodoTaskList? _taskList;
    List<TodoTaskList>? _taskLists;
    List<TaskInList>? _tasks;

    public Tasks(GraphServiceClient graphClient)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    #endregion

    #region AI Functions

    [Description("Creates new user Task/ToDo")]
    private async Task<string> CreateUserTask(
        [Description(TaskTitle)]
        string title,
        [Description(TaskContent)]
        string content,
        [Description(TaskListNameDescription)]
        string list,
        [Description("Suggested due day which can be in date format or relative such as tomorrow, next week etc")]
        string dueDate)
    {
        var taskList = await GetTaskList(list);

        var task = new TodoTask
        {
            Title = title,
            Body = new ItemBody
            {
                Content = content,
                ContentType = BodyType.Text
            },
            DueDateTime = new DateTimeTimeZone
            {
                DateTime = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = TimeZoneInfo.Local.Id
            }
        };
        var result = await _graphClient.Me.Todo.Lists[taskList.Id].Tasks.PostAsync(task);

        return "Task created";
    }

    [Description("Gets user Task/ToDo details such as description, due time, attachments, steps etc")]
    private async Task<string> GetUserTaskDetails(
        [Description(TaskTitle)]
        string title)
    {
        var taskList = await GetTasks();
        var task = taskList?.FirstOrDefault(x => x.Task.Title == title);
        if (task == null)
            return $"Task '{title}' not found.";

        var response = new StringBuilder();
        response.AppendLine($"Title: {task.Task.Title}");
        if (task.Task.Body != null && !string.IsNullOrWhiteSpace(task.Task.Body.Content))
            response.AppendLine($"Description: {task.Task.Body?.Content}");
        response.AppendLine($"Due date: {task.Task.DueDateTime?.DateTime}");
        if (task.Task.LinkedResources != null)
        {
            foreach (var linkedResource in task.Task.LinkedResources)
            {
                if (linkedResource.ApplicationName == LinkedOutlook)
                {
                    response.AppendLine($"[Email]({linkedResource.WebUrl})");
                }
            }
        }

        return response.ToString();
    }

    [Description("Fetches the list of user Tasks/To-Dos")]
    private async Task<string> GetUserTasks(
        [Description("It can be nothing for all, tomorrow, next week etc")]
        string dueDate,
        [Description("Task list name such as 'Flagged Emails', 'Tasks' or other task lists the person created")]
        string list,
        [Description(TaskStatusList)]
        string status)
    {
        var taskList = await GetTasks(list, refresh: true);
        if (taskList.Count == 0)
            return "No tasks found.";

        var response = new StringBuilder();
        response.AppendLine($"# User's tasks and/or falgged emails");
        response.AppendLine();
        response.AppendLine($"> **Note to assistant:** you can add short one sentence next step for each");

        foreach (var task in taskList.OrderBy(x => x.Task.DueDateTime?.DateTime))
        {
            bool isHandled = false;
            if (task.Task.LinkedResources != null)
            {
                foreach (var linkedResource in task.Task.LinkedResources)
                {
                    if (linkedResource.ApplicationName == LinkedOutlook)
                    {
                        response.AppendLine();
                        response.AppendLine($"## [{linkedResource.DisplayName}]({linkedResource.WebUrl})");
                        response.AppendLine();
                        response.AppendLine($"- List: {task.TaskList.DisplayName}");
                        isHandled = true;
                        break;
                    }
                }
            }

            if (isHandled)
                continue;

            response.AppendLine();
            response.AppendLine($"## {task.Task.Title}");
            response.AppendLine();
            response.AppendLine($"- List: {task.TaskList.DisplayName}");
            response.AppendLine($"- Status: {task.Task.Status}");
            response.AppendLine($"- DueDateTime: {task.Task.DueDateTime?.DateTime}");
            response.AppendLine($"- Details: {task.Task.Body?.Content}");
        }

        return response.ToString();
    }

    [Description("Update user Task/ToDo title, content, status or due date")]
    private async Task<string> UpdateUserTaskStatus(
        [Description("Old title to find task"), Required]
        string oldTitle,
        [Description("Updated title"), Required]
        string newTitle,
        [Description(TaskContent)]
        string? content,
        [Description("New due date or empty")]
        string? dueDate,
        [Description(TaskStatusList)]
        string? statusString)
    {
        var taskList = await GetTasks();
        var task = taskList.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Task.Title) && x.Task.Title.Equals(oldTitle, StringComparison.OrdinalIgnoreCase));
        if (task == null)
            return $"Task '{oldTitle}' not found.";

        if (!string.IsNullOrWhiteSpace(dueDate))
        {
            var dueDateTimeOffset = dueDate.ToDateTimeOffset(TimeProvider.System);
            task.Task.DueDateTime = dueDateTimeOffset.ToDateTimeTimeZone();
        }

        if (Enum.TryParse<Microsoft.Graph.Beta.Models.TaskStatus>(statusString, true, out var taskStatus))
        {
            task.Task.Status = taskStatus;
            if (task.Task.Recurrence?.Range?.Type == RecurrenceRangeType.NoEnd)
                task.Task.Recurrence.Range = null;
        }

        // Delete task if it is linked to Outlook
        if (task.Task.LinkedResources != null)
        {
            foreach(var linkedResource in task.Task.LinkedResources)
            {
                if (linkedResource.ApplicationName == LinkedOutlook)
                {
                    if (task.Task.Status == Microsoft.Graph.Beta.Models.TaskStatus.Completed)
                        await _graphClient.Me.Todo.Lists[task.TaskList.Id].Tasks[task.Task.Id].DeleteAsync();
                    else
                    {
                        var email = await _graphClient.Me.Messages[linkedResource.ExternalId].GetAsync();
                        if (email.Flag == null)
                        {
                            email.Flag = new FollowupFlag() { FlagStatus = FollowupFlagStatus.Flagged };
                        }
                        email.Flag.DueDateTime = task.Task.DueDateTime;
                        await _graphClient.Me.Messages[linkedResource.ExternalId].PatchAsync(email);
                    }

                    return $"Done";
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            task.Task.Title = newTitle;
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            task.Task.Body = new ItemBody
            {
                Content = content,
                ContentType = BodyType.Text
            };
        }

        var result = await _graphClient.Me.Todo.Lists[task.TaskList.Id].Tasks[task.Task.Id].PatchAsync(task.Task);
        return "Task updated";
    }

    #endregion

    #region Private methods

    private async Task<TodoTaskList> GetTaskList(string? listName = null, WellknownListName wellknownListName = WellknownListName.DefaultList, bool refresh = false)
    {
        if (_taskLists == null || refresh)
        {
            var taskListsResponse = await _graphClient.Me.Todo.Lists.GetAsync();
            if (taskListsResponse == null || taskListsResponse.Value == null)
                throw new FunctionCallException("Failed to get task lists.");
            _taskLists = taskListsResponse.Value;
        }

        if (string.IsNullOrWhiteSpace(listName) && wellknownListName == WellknownListName.DefaultList)
        {
            if (_taskList != null && !refresh)
                return _taskList;

            _taskList = _taskLists.FirstOrDefault(x => x.WellknownListName == WellknownListName.DefaultList);
            if (_taskList == null)
                throw new FunctionCallException("Default task list not found.");

            return _taskList;
        }

        var taskList = _taskLists.FirstOrDefault(x => x.DisplayName == listName);
        if (taskList == null)
            throw new FunctionCallException($"'{listName}' task list not found.");

        return _taskList = taskList;
    }

    private async Task<List<TaskInList>> GetTasks(string? listName = null, WellknownListName? wellknownListName = null, bool refresh = false)
    {
        // Cache task lists
        if (_taskLists == null || refresh)
        {
            var taskListsResponse = await _graphClient.Me.Todo.Lists.GetAsync();
            if (taskListsResponse == null || taskListsResponse.Value == null)
                throw new FunctionCallException("Failed to get task lists.");
            _taskLists = taskListsResponse.Value;
        }

        // Cache tasks
        if (_tasks == null || refresh)
        {
            _tasks = new();
            foreach (var taskList in _taskLists)
            {
                var tasksResponse = await _graphClient.Me.Todo.Lists[taskList.Id].Tasks.GetAsync((t) =>
                {
                    t.QueryParameters.Orderby = ["dueDateTime/dateTime asc"];
                    t.QueryParameters.Filter = $"status ne 'completed'";
                });
                if (tasksResponse == null || tasksResponse.Value == null)
                    throw new FunctionCallException($"Failed to get '{taskList.DisplayName}' tasks.");
                _tasks.AddRange(tasksResponse.Value.Select(t => new TaskInList(t, taskList)));
            }
        }

        string? filterTaskListId = null;
        if (!string.IsNullOrWhiteSpace(listName)) 
        {
            var taskList = _taskLists.FirstOrDefault(x => x.DisplayName == listName);
            if (taskList == null)
                throw new FunctionCallException($"'{listName}' task list not found.");
            filterTaskListId = taskList.Id;
        }
        else if (wellknownListName.HasValue)
        {
            var taskList = _taskLists.FirstOrDefault(x => x.WellknownListName == wellknownListName);
            if (taskList == null)
                throw new FunctionCallException($"'{wellknownListName}' task list not found.");
            filterTaskListId = taskList.Id;
        }

        // If no filter, return all tasks
        if (string.IsNullOrWhiteSpace(filterTaskListId))
            return _tasks;
        
        return _tasks.Where(x => x.TaskList.Id == filterTaskListId).ToList();
    }

    #endregion
}
