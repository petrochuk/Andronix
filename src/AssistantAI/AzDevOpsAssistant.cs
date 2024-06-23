using Andronix.Authentication;
using Andronix.Core.Options;
using Andronix.Interfaces;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Core.WebApi.Types;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System.ComponentModel;

namespace Andronix.AssistantAI;

public class AzDevOpsAssistant : ISpecializedAssistant
{
    #region Constants

    private const string UserStory = "User Story";

    #endregion

    #region Fields & Constructors

    AzDevOps _options;
    AzDevOpsAuthProvider _azDevOpsAuthProvider;
    WebApiTeam? _teamDetails;
    TeamSetting? _teamSetting;

    public AzDevOpsAssistant(IOptions<AzDevOps> options, AzDevOpsAuthProvider azDevOpsAuthProvider)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        _options = options.Value;

        _azDevOpsAuthProvider = azDevOpsAuthProvider ?? throw new ArgumentNullException(nameof(azDevOpsAuthProvider));
    }

    #endregion

    #region AI Functions

    [Description("Create new User Story in a backlog on Azure DevOps.")]
    private async Task<string> CreateNewUserStory(
        [Description("Title")]
        string title,
        [Description("Summary or desciption")]
        string summary)
    {
        var connection = await GetVssConnection().ConfigureAwait(false);
        var client = connection.GetClient<WorkItemTrackingHttpClient>();
        var team = await GetTeamDetails(connection).ConfigureAwait(false);
        var teamSettings = await GetTeamSettings(connection).ConfigureAwait(false);

        var patchDocument = new JsonPatchDocument
        {
            new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.Title",
                Value = title
            },
            new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.Description",
                Value = summary
            },
            new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.AreaPath",
                Value = _options.AreaPath
            },
            new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/fields/System.IterationPath",
                Value = $"{_options.Project}{teamSettings.BacklogIteration.Path}"
            },
        };

        var result = client.CreateWorkItemAsync(patchDocument, _options.Project, UserStory).Result;

        return $"New user story id {result.Id} has been created";
    }

    [Description("Open an Azure DevOps item such as Bug, User Story, Feature, Task in a browser to see details and edit.")]
    private async Task<string> OpenItem(
        [Description("Id of Bug, User Story, Feature, Task")]
        string id)
    {
        if (!int.TryParse(id, out int itemId))
            return "Invalid item id. It should be positive number";

        var connection = await GetVssConnection().ConfigureAwait(false);
        var client = connection.GetClient<WorkItemTrackingHttpClient>();

        try
        {
            var item = await client.GetWorkItemAsync(_options.Project, itemId).ConfigureAwait(false);
            if (!item.Links.Links.TryGetValue("html", out var valueObj))
                return "Link to item is not available";

            if (!(valueObj is ReferenceLink referenceLink))
                return "Link to item is not available";
            var startInfo = new ProcessStartInfo(referenceLink.Href);
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            var p = System.Diagnostics.Process.Start(startInfo);
        }
        catch (VssServiceException ex)
        {
            return ex.Message;
        }

        return $"Item has been open";
    }

    #endregion

    #region Authentication

    private async Task<VssConnection> GetVssConnection()
    {
        var authResult = await _azDevOpsAuthProvider.AquireTokenSilent().ConfigureAwait(false);
        return new VssConnection(new Uri(_options.OrganizationUrl), new VssOAuthAccessTokenCredential(authResult.AccessToken));
    }

    private async Task<WebApiTeam> GetTeamDetails(VssConnection vssConnection)
    {
        if (_teamDetails != null)
            return _teamDetails;

        var client = vssConnection.GetClient<TeamHttpClient>();

        return _teamDetails = await client.GetTeamAsync(_options.Project, _options.Team).ConfigureAwait(false);
    }

    private async Task<TeamSetting> GetTeamSettings(VssConnection vssConnection)
    {
        if (_teamSetting != null)
            return _teamSetting;

        var teamContext = new TeamContext(_options.Project, _options.Team);
        var client = vssConnection.GetClient<WorkHttpClient>();

        return _teamSetting = await client.GetTeamSettingsAsync(teamContext).ConfigureAwait(false);
    }

    #endregion
}
