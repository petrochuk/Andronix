using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Andronix.Core.Extensions;

public static class GraphExtensions
{
    public static async Task<Microsoft.Graph.Beta.Models.Person?> FindPerson(this GraphServiceClient graphServiceClient, string personName)
    {
        if (string.IsNullOrWhiteSpace(personName))
            throw new ArgumentNullException(nameof(personName));

        if (graphServiceClient == null)
            throw new ArgumentNullException(nameof(graphServiceClient));

        var people = await graphServiceClient.Me.People.GetAsync((c) =>
        {
            c.QueryParameters.Top = 300;
        }).ConfigureAwait(false);
        if (people == null || people.Value == null || people.Value.Count <= 0)
            return null;

        if (TryFindPerson(people, personName, out var person))
            return person;

        people = await graphServiceClient.Me.People.GetAsync((c) =>
        {
            c.QueryParameters.Search = $"\"{personName}\"";
        }).ConfigureAwait(false);
        if (people == null || people.Value == null || people.Value.Count <= 0)
            return null;

        if (TryFindPerson(people, personName, out person))
            return person;

        return null;
    }

    public static bool TryFindPerson(this PersonCollectionResponse personCollection, string personName, out Microsoft.Graph.Beta.Models.Person? person)
    {
        person = null;
        if (personCollection == null || personCollection.Value == null)
            return false;

        // Exact match on person name
        foreach (var candidate in personCollection.Value)
        {
            if (string.Compare(candidate.DisplayName, personName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(candidate.GivenName, personName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(candidate.Surname, personName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                person = candidate;
                return true;
            }
        }

        var personNameParts = personName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var personNamePart in personNameParts)
        {
            foreach (var candidate in personCollection.Value)
            {
                if (string.Compare(candidate.DisplayName, personNamePart, StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(candidate.GivenName, personNamePart, StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(candidate.Surname, personNamePart, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    person = candidate;
                    return true;
                }
            }
        }

        return false;
    }
}
