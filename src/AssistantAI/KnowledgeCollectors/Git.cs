using Andronix.Authentication;
using Andronix.Core;
using Andronix.Core.VectorDB;
using Azure.AI.OpenAI;
using LibGit2Sharp;
using OpenAI.Embeddings;
using System.Text.RegularExpressions;

namespace Andronix.AssistantAI.KnowledgeCollectors;

public partial class Git : KnowledgeCollectorBase
{
    const string FirstCommit = "FirstCommit";
    const string LastCommit = "LastCommit";
    const string NewLine = "\n";
    const int EmbeddingBatchSize = 10;

    private Core.Options.Git _gitOptions;
    private Core.Options.Cognitive _cognitiveOptions;
    private AzureOpenAIClient _azureOpenAIClient;
    private EmbeddingClient _embeddingClient;

    public Git(IOptions<Core.Options.Git> gitOptions,
        IOptions<Core.Options.Cognitive> cognitiveOptions,
        AndronixTokenCredential andronixTokenCredential) : base()
    {
        _ = gitOptions ?? throw new ArgumentNullException(nameof(gitOptions));
        _gitOptions = gitOptions.Value;
        _cognitiveOptions = cognitiveOptions.Value ?? throw new ArgumentNullException(nameof(cognitiveOptions));

        _azureOpenAIClient = new AzureOpenAIClient(_cognitiveOptions.EndPoint, andronixTokenCredential);
        _embeddingClient = _azureOpenAIClient.GetEmbeddingClient(_cognitiveOptions.EmbeddingModel);
    }

    public override void DoWork()
    {
        foreach (var repoInfo in _gitOptions.Repositories)
        {
            if (string.IsNullOrWhiteSpace(repoInfo.VectorDBPath))
                continue;

            if (File.Exists(repoInfo.VectorDBPath))
                repoInfo.VectorDB = new VectorDB<Sha1>(repoInfo.VectorDBPath);
            else
                repoInfo.VectorDB = new VectorDB<Sha1>();

            ProcessRepository(repoInfo);
            if (IsShuttingDown)
                return;

            var embedding = _embeddingClient.GenerateEmbedding("Incoming Action Activity");
            var results = repoInfo.VectorDB.FindWithDistance(embedding.Value.ToFloats());
        }
    }

    private void ProcessRepository(Core.Options.Git.Repository repoInfo)
    {
        // Read all commits
        using var repo = new Repository(repoInfo.LocalClone);

        foreach (var commit in repo.Commits.Take(10))
        {
            if (commit.Parents.Count() > 1)
                continue; // Skip merge commits

            var firstParent = commit.Parents.FirstOrDefault();
            if (firstParent == null)
                continue; // Skip commits without parent

            var changes = repo.Diff.Compare<TreeChanges>(firstParent.Tree, commit.Tree);
            if (changes == null)
                continue;
            ProcessChanges(repoInfo.VectorDB, repo, changes, commit.Sha);

            repoInfo.VectorDB.Write(repoInfo.VectorDBPath);
            if (IsShuttingDown)
                return;
        }
    }

    private void ProcessChanges(VectorDB<Sha1> vectorDB, Repository repo, TreeChanges changes, string commitSha)
    {
        foreach (var change in changes)
        {
            var oldBlob = repo.Lookup<Blob>(change.OldOid);
            var newBlob = repo.Lookup<Blob>(change.Oid);
            var contentChange = repo.Diff.Compare(oldBlob, newBlob);
            if (contentChange == null)
                continue;

            // Create list of lines which have text content
            var textLines = new List<string>();
            foreach (var addedLine in contentChange.AddedLines)
            {
                var cleanLine = addedLine.Content;
                if (cleanLine.EndsWith(NewLine))
                    cleanLine = cleanLine.Substring(0, cleanLine.Length - NewLine.Length);

                // Skip very short lines and lines with only special characters
                if (cleanLine.Length < 5 ||
                    string.IsNullOrWhiteSpace(cleanLine) || 
                    SpecialCharactersOnly().IsMatch(cleanLine))
                    continue;

                textLines.Add(cleanLine);

                if (IsShuttingDown)
                    return;
            }

            if (textLines.Count == 0)
                continue;

            if (textLines.Count < EmbeddingBatchSize)
            {
                var embeddings = _embeddingClient.GenerateEmbeddings(textLines);
                foreach (var embedding in embeddings.Value)
                {
                    vectorDB.Insert(embedding.ToFloats(), new Sha1(commitSha), null);
                }
            }
            else // use batch embeddings
            {
                for (var i = 0; i < textLines.Count; i += EmbeddingBatchSize)
                {
                    var embeddings = _embeddingClient.GenerateEmbeddings(textLines.Skip(i).Take(EmbeddingBatchSize));
                    foreach (var embedding in embeddings.Value)
                    {
                        vectorDB.Insert(embedding.ToFloats(), new Sha1(commitSha), null);
                    }
                    if (IsShuttingDown)
                        return;
                }
            }


            if (IsShuttingDown)
                return;
        }
    }

    [GeneratedRegex("^[ !@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]*$")]
    private static partial Regex SpecialCharactersOnly();
}
