using System.Text.Json.Serialization;

namespace Andronix.Core.Options;

public class Git
{
    [DebuggerDisplay("{Name}")]
    public class Repository
    {
        public required string Name { get; set; }
        public required string Url { get; set; }
        public required string LocalClone { get; set; }
        public required string VectorDBPath { get; set; }
        [JsonIgnore]
        public required VectorDB.VectorDB<Sha1> VectorDB { get; set; }
        public Dictionary<string, string> ExcludedFolders { get; set; } = new Dictionary<string, string>();
    }

    public IList<Repository> Repositories { get; set; } = new List<Repository>();
}
