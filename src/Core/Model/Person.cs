using System.Diagnostics.CodeAnalysis;

namespace Andronix.Core.Model;

[DebuggerDisplay("{DisplayName}")]
public class Person
{
    public required string? GivenName { get; set; }

    public required string? Surname { get; set; }

    string? _displayName = string.Empty;
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_displayName))
                return $"{GivenName} {Surname}";
            return _displayName;
        }
        set => _displayName = value;
    }

    public string? GraphId { get; set; }

    public string? Title { get; set; }

    public string? UserPrincipalName { get; set; }

    [SetsRequiredMembers]
    public Person(Microsoft.Graph.Beta.Models.Person person)
    {
        GraphId = person.Id;
        GivenName = person.GivenName;
        Surname = person.Surname;
        Title = person.Title;
        UserPrincipalName = person.UserPrincipalName;
        _displayName = person.DisplayName;
    }
}
