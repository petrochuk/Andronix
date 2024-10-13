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
    }

    public IList<Repository> Repositories { get; set; } = new List<Repository>();
}
