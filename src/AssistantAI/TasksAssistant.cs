using Andronix.Interfaces;
using Markdig.Extensions.TaskLists;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;

namespace Andronix.AssistantAI;

public class TasksAssistant : ISpecializedAssistant
{
    #region Constants
    
    public const string TaskListNameDescription = "Task list name such as 'Flagged Emails', 'Tasks' or other task lists the person created";
    public const string DefaultTaskListName = "Tasks";

    #endregion

    #region Fields & Constructors

    GraphServiceClient _graphClient;
    TodoTaskList? _taskList;
    TodoTaskList? _defaultList;
    TodoTaskList? _flaggedEmailsList;

    public TasksAssistant(GraphServiceClient graphClient)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    #endregion

    #region AI Functions

    [Description("Creates new Task/ToDo for the person you are assisting")]
    private async Task<string> CreateTask(
        [Description("Short item title")]
        string title,
        [Description("Longer item description without due date or time")]
        string description,
        [Description(TaskListNameDescription)]
        string list,
        [Description("Suggested due day which can be in date format or relative such as tomorrow, next week etc")]
        string dueDate)
    {
        var taskListResponse = await GetTaskList(list);
        if (taskListResponse.taskList == null)
            return taskListResponse.response;

        var task = new TodoTask
        {
            Title = title,
            Body = new ItemBody
            {
                Content = description,
                ContentType = BodyType.Text
            },
            DueDateTime = new DateTimeTimeZone
            {
                DateTime = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = TimeZoneInfo.Local.Id
            }
        };
        var result = await _graphClient.Me.Todo.Lists[taskListResponse.taskList.Id].Tasks.PostAsync(task);

        return "Done";
    }

    [Description("Gets Tasks/ToDos for the person you are assisting")]
    private async Task<string> GetToDoItems(
        [Description("It can be nothing for all, tomorrow, next week etc")]
        string dueDate,
        [Description("Task list name such as 'Flagged Emails', 'Tasks' or other task lists the person created")]
        string list,
        [Description("Optional status: Done, New, In Progress")]
        string status)
    {
        var taskLists = await _graphClient.Me.Todo.Lists.GetAsync();
        if (taskLists == null || taskLists.Value == null)
            return "Failed to get task lists.";

        // If list is not provided, default to Tasks and Flagged Emails
        List<TodoTask> tasks;
        if (string.IsNullOrWhiteSpace(list))
        {
            _defaultList ??= taskLists.Value.FirstOrDefault(x => x.WellknownListName == WellknownListName.DefaultList);
            if (_defaultList == null)
                return $"Default task list was not found.";
            var defaultTasksResponse = await _graphClient.Me.Todo.Lists[_defaultList.Id].Tasks.GetAsync((t) =>
            {
                t.QueryParameters.Orderby = ["dueDateTime/dateTime asc"];
                t.QueryParameters.Filter = $"status ne 'completed'";
            });
            if (defaultTasksResponse == null || defaultTasksResponse.Value == null)
                return "Failed to get default tasks.";
            tasks = defaultTasksResponse.Value;

            _flaggedEmailsList ??= taskLists.Value.FirstOrDefault(x => x.WellknownListName == WellknownListName.FlaggedEmails);
            if (_flaggedEmailsList != null)
            {
                var flaggedEmailsTasksResponse = await _graphClient.Me.Todo.Lists[_flaggedEmailsList.Id].Tasks.GetAsync((t) =>
                {
                    t.QueryParameters.Orderby = ["dueDateTime/dateTime asc"];
                    t.QueryParameters.Filter = $"status ne 'completed'";
                });
                if (flaggedEmailsTasksResponse != null && flaggedEmailsTasksResponse.Value != null)
                {
                    tasks.AddRange(flaggedEmailsTasksResponse.Value);
                }
            }
        }
        else
        {
            var taskList = taskLists.Value.FirstOrDefault(x => x.DisplayName == list);
            if (taskList == null)
                return $"'{list}' list was not found.";

            var tasksResponse = await _graphClient.Me.Todo.Lists[taskList.Id].Tasks.GetAsync((t) =>
            {
                t.QueryParameters.Orderby = ["dueDateTime/dateTime asc"];
                t.QueryParameters.Filter = $"status ne 'completed'";
            });
            if (tasksResponse == null || tasksResponse.Value == null)
                return "Failed to get tasks.";
            tasks = tasksResponse.Value;
        }

        if (tasks.Count == 0)
            return "No tasks found.";

        var response = new StringBuilder();
        foreach (var task in tasks.OrderBy(x => x.DueDateTime?.DateTime))
        {
            response.AppendLine($"{task.Title}");
        }

        return response.ToString();
    }

    [Description("Update Task/ToDo status")]
    private async Task<string> UpdateTaskStatus(
        [Description("Task name"), Required]
        string name,
        [Description("New due date or empty")]
        string dueDate,
        [Description("Done, New, Not Started, In Progress")]
        string status)
    {
        var taskLists = await _graphClient.Me.Todo.Lists.GetAsync();
        if (taskLists == null || taskLists.Value == null)
            return "Failed to get tasks.";

        // List all items from Tasks list
        var list = "Tasks";
        if (string.IsNullOrWhiteSpace(list))
            list = "Tasks";
        var taskList = taskLists.Value.FirstOrDefault(x => x.DisplayName == list);
        if (taskList == null)
            return $"'{list}' list not found.";

        var tasks = await _graphClient.Me.Todo.Lists[taskList.Id].Tasks.GetAsync((t) =>
        {
            t.QueryParameters.Filter = $"title eq '{Uri.EscapeDataString(name)}' and status ne 'completed'";
        });
        if (tasks == null || tasks.Value == null)
            return "Failed to get tasks.";

        if (tasks.Value.Count == 0)
            return $"Task '{name}' was not found.";

        tasks.Value[0].Status = Microsoft.Graph.Beta.Models.TaskStatus.Completed;
        if (tasks.Value[0].Recurrence?.Range?.Type == RecurrenceRangeType.NoEnd)
            tasks.Value[0].Recurrence.Range = null;
        
        var result = await _graphClient.Me.Todo.Lists[taskList.Id].Tasks[tasks.Value[0].Id].PatchAsync(tasks.Value[0]);

        return "Done";
    }

    #endregion

    #region Private methods

    private async Task<(string response, TodoTaskList? taskList)> GetTaskList(string listName = DefaultTaskListName)
    {
        if (string.IsNullOrWhiteSpace(listName))
            listName = DefaultTaskListName;

        if (_taskList != null)
        {
            return new () 
            { 
                response = string.Empty, 
                taskList = _taskList 
            };
        }

        var taskLists = await _graphClient.Me.Todo.Lists.GetAsync();
        if (taskLists == null || taskLists.Value == null)
            return new() { response = "Failed to get tasks.", taskList = null };

        var taskList = taskLists.Value.FirstOrDefault(x => x.DisplayName == listName);
        if (taskList == null)
            return new() { response = $"'{listName}' list not found.", taskList = null };

        return new() { response = string.Empty, taskList = taskList };
    }

    #endregion
}
