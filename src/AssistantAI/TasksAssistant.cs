using Andronix.Core.Graph;
using Andronix.Interfaces;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Andronix.AssistantAI;

public class TasksAssistant : ISpecializedAssistant
{
    #region Constants
    
    public const string TaskListNameDescription = "Task list name such as 'Flagged Emails', 'Tasks' or other task lists the person created";
    public const string DefaultTaskListName = "Tasks";
    public const string LinkedOutlook = "Outlook";

    #endregion

    #region Fields & Constructors

    GraphServiceClient _graphClient;
    TodoTaskList? _taskList;
    List<TodoTaskList>? _taskLists;
    List<TaskInList>? _tasks;

    public TasksAssistant(GraphServiceClient graphClient)
    {
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    #endregion

    #region AI Functions

    [Description("Creates new Task/ToDo for the person you are assisting")]
    private async Task<string> CreateTask(
        [Description("Task title")]
        string title,
        [Description("Longer task description (without due date)")]
        string description,
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
                Content = description,
                ContentType = BodyType.Text
            },
            DueDateTime = new DateTimeTimeZone
            {
                DateTime = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = TimeZoneInfo.Local.Id
            }
        };
        var result = await _graphClient.Me.Todo.Lists[taskList.Id].Tasks.PostAsync(task);

        return "Done";
    }

    [Description("Gets Task/ToDo details such as description, due time, attachments, steps etc")]
    private async Task<string> GetTaskDetails(
        [Description("Task title")]
        string title)
    {
        var taskList = await GetTasks();
        var task = taskList?.FirstOrDefault(x => x.TodoTask.Title == title);
        if (task == null)
            return $"Task '{title}' not found.";

        var response = new StringBuilder();
        response.AppendLine($"Title: {task.TodoTask.Title}");
        if (task.TodoTask.Body != null && !string.IsNullOrWhiteSpace(task.TodoTask.Body.Content))
            response.AppendLine($"Description: {task.TodoTask.Body?.Content}");
        response.AppendLine($"Due date: {task.TodoTask.DueDateTime?.DateTime}");
        if (task.TodoTask.LinkedResources != null)
        {
            foreach (var linkedResource in task.TodoTask.LinkedResources)
            {
                if (linkedResource.ApplicationName == LinkedOutlook)
                {
                    response.AppendLine($"[Email]({linkedResource.WebUrl})");
                }
            }
        }

        return response.ToString();
    }

    [Description("Gets Tasks/ToDos for the person you are assisting")]
    private async Task<string> GetTasks(
        [Description("It can be nothing for all, tomorrow, next week etc")]
        string dueDate,
        [Description("Task list name such as 'Flagged Emails', 'Tasks' or other task lists the person created")]
        string list,
        [Description("Optional status: Done, New, In Progress")]
        string status)
    {
        var taskList = await GetTasks(list, refresh: true);
        if (taskList.Count == 0)
            return "No tasks found.";

        var response = new StringBuilder();
        foreach (var task in taskList.OrderBy(x => x.TodoTask.DueDateTime?.DateTime))
        {
            bool isHandled = false;
            if (task.TodoTask.LinkedResources != null)
            {
                foreach (var linkedResource in task.TodoTask.LinkedResources)
                {
                    if (linkedResource.ApplicationName == LinkedOutlook)
                    {
                        response.AppendLine($"[{linkedResource.DisplayName}]({linkedResource.WebUrl})");
                        isHandled = true;
                    }
                }
            }

            if (!isHandled)
                response.AppendLine($"{task.TodoTask.Title}");
        }

        return response.ToString();
    }

    [Description("Update Task/ToDo status")]
    private async Task<string> UpdateTaskStatus(
        [Description("Task title"), Required]
        string title,
        [Description("New due date or empty")]
        string dueDate,
        [Description("Done, New, Not Started, In Progress")]
        string status)
    {
        var taskList = await GetTasks();
        var task = taskList.FirstOrDefault(x => x.TodoTask.Title == title);
        if (task == null)
            return $"Task '{title}' not found.";

        // Delete task if it is linked to Outlook
        if (task.TodoTask.LinkedResources != null)
        {
            foreach(var linkedResource in task.TodoTask.LinkedResources)
            {
                if (linkedResource.ApplicationName == LinkedOutlook)
                {
                    await _graphClient.Me.Todo.Lists[task.ListId].Tasks[task.TodoTask.Id].DeleteAsync();
                    return $"Done";
                }
            }
        }

        task.TodoTask.Status = Microsoft.Graph.Beta.Models.TaskStatus.Completed;
        if (task.TodoTask.Recurrence?.Range?.Type == RecurrenceRangeType.NoEnd)
            task.TodoTask.Recurrence.Range = null;
        
        var result = await _graphClient.Me.Todo.Lists[task.ListId].Tasks[task.TodoTask.Id].PatchAsync(task.TodoTask);

        return "Done";
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

    private async Task<List<TaskInList>> GetTasks(string? listName = null, WellknownListName wellknownListName = WellknownListName.DefaultList, bool refresh = false)
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
            if (_tasks != null && !refresh)
                return _tasks;

            _tasks = new();
            var defaultList = _taskLists.FirstOrDefault(x => x.WellknownListName == WellknownListName.DefaultList);
            if (defaultList == null)
                throw new FunctionCallException("Default task list not found.");

            // Load tasks for default list
            var defaultTasksResponse = await _graphClient.Me.Todo.Lists[defaultList.Id].Tasks.GetAsync((t) =>
            {
                t.QueryParameters.Orderby = ["dueDateTime/dateTime asc"];
                t.QueryParameters.Filter = $"status ne 'completed'";
            });
            if (defaultTasksResponse == null || defaultTasksResponse.Value == null)
                throw new FunctionCallException("Failed to get default tasks.");
            // Add tasks to the list
            _tasks.AddRange(defaultTasksResponse.Value.Select(t => new TaskInList(t, defaultList.Id)));

            // Load tasks for flagged emails
            var flaggedEmailsList = _taskLists.FirstOrDefault(x => x.WellknownListName == WellknownListName.FlaggedEmails);
            if (flaggedEmailsList != null)
            {
                var flaggedEmailsTasksResponse = await _graphClient.Me.Todo.Lists[flaggedEmailsList.Id].Tasks.GetAsync((t) =>
                {
                    t.QueryParameters.Orderby = ["dueDateTime/dateTime asc"];
                    t.QueryParameters.Filter = $"status ne 'completed'";
                });
                if (flaggedEmailsTasksResponse != null && flaggedEmailsTasksResponse.Value != null)
                {
                    // Add tasks to the list
                    _tasks.AddRange(flaggedEmailsTasksResponse.Value.Select(t => new TaskInList(t, defaultList.Id)));
                }
            }

            return _tasks;
        }

        var taskList = _taskLists.FirstOrDefault(x => x.DisplayName == listName);
        if (taskList == null)
            throw new FunctionCallException($"'{listName}' task list not found.");
        
        // Load tasks
        var tasksResponse = await _graphClient.Me.Todo.Lists[taskList.Id].Tasks.GetAsync((t) =>
        {
            t.QueryParameters.Orderby = ["dueDateTime/dateTime asc"];
            t.QueryParameters.Filter = $"status ne 'completed'";
        });
        if (tasksResponse == null || tasksResponse.Value == null)
            throw new FunctionCallException($"Failed to get '{listName}' tasks.");

        _tasks = new();
        _tasks.AddRange(tasksResponse.Value.Select(t => new TaskInList(t, taskList.Id)));

        return _tasks;
    }

    #endregion
}
