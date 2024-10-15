using Andronix.Authentication;
using Andronix.Core;
using Andronix.Core.VectorDB;
using Azure.AI.OpenAI;
using LibGit2Sharp;
using OpenAI.Embeddings;
using System.Text;
using System.Text.RegularExpressions;

namespace Andronix.AssistantAI.KnowledgeCollectors;

public partial class Git : KnowledgeCollectorBase
{
    const string FirstCommit = "FirstCommit";
    const string LastCommit = "LastCommit";
    const int MaxBlockSize = 16000;
    const string NewLine = "\n";
    const int EmbeddingBatchSize = 100;
    readonly static Dictionary<string, string> ExcludedExtensions = new() 
    { 
        { ".svg", "Scalable Vector Graphics" },
        { ".snap", "Jest Snapshot" }
    };

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

#if DEBUG // Test embedding
            var embedding = _embeddingClient.GenerateEmbedding("Governance Section for copilot");
            var results = repoInfo.VectorDB.FindWithDistance(embedding.Value.ToFloats(), maxResultCount: 10);
#endif
        }
    }

    private void ProcessRepository(Core.Options.Git.Repository repoInfo)
    {
        // Read all commits
        using var gitRepo = new Repository(repoInfo.LocalClone);
        repoInfo.VectorDB.Headers.TryGetValue(LastCommit, out var lastCommitSha);

        if (string.IsNullOrWhiteSpace(lastCommitSha))
        {
            // Go through all commits starting from the last one
            foreach (var commit in gitRepo.Commits)
            {
                ProcessCommit(repoInfo, gitRepo, commit, FirstCommit);
                if (IsShuttingDown)
                    return;
            }
        }
        else
        {
            // First process the latest commits
            var commitFilter = new CommitFilter
            {
                ExcludeReachableFrom = lastCommitSha,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time | CommitSortStrategies.Reverse,
            };
            foreach (var commit in gitRepo.Commits.QueryBy(commitFilter))
            {
                ProcessCommit(repoInfo, gitRepo, commit, LastCommit);
                if (IsShuttingDown)
                    return;
            }

            // Then process the rest of the commits
            commitFilter = new CommitFilter
            {
                IncludeReachableFrom = repoInfo.VectorDB.Headers[FirstCommit],
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            };
            foreach (var commit in gitRepo.Commits.QueryBy(commitFilter).Skip(1))
            {
                ProcessCommit(repoInfo, gitRepo, commit, FirstCommit);
                if (IsShuttingDown)
                    return;
            }
        }
    }

    private void ProcessCommit(Core.Options.Git.Repository repoInfo, Repository gitRepo, Commit commit, string commitDirection)
    {
        if (commit.Parents.Count() > 1)
            return; // Skip merge commits

        var firstParent = commit.Parents.FirstOrDefault();
        if (firstParent == null)
            return; // Skip commits without parent

        var changes = gitRepo.Diff.Compare<TreeChanges>(firstParent.Tree, commit.Tree);
        if (changes == null)
            return;

        var embedding = _embeddingClient.GenerateEmbedding(commit.Message);
        repoInfo.VectorDB.Insert(embedding.Value.ToFloats(), new Sha1(commit.Sha), null);
        embedding = _embeddingClient.GenerateEmbedding(commit.MessageShort);
        repoInfo.VectorDB.Insert(embedding.Value.ToFloats(), new Sha1(commit.Sha), null);

        ProcessChanges(repoInfo, gitRepo, changes, commit.Sha);

        if (repoInfo.VectorDB.Headers.ContainsKey(commitDirection))
            repoInfo.VectorDB.Headers[commitDirection] = commit.Sha;
        else
        {
            repoInfo.VectorDB.Headers.Add(FirstCommit, commit.Sha);
            repoInfo.VectorDB.Headers.Add(LastCommit, commit.Sha);
        }
        repoInfo.VectorDB.Write(repoInfo.VectorDBPath);
        Debug.WriteLine($"{commitDirection}: {commit.Sha}");
    }

    private void ProcessChanges(Core.Options.Git.Repository repoInfo, Repository repo, TreeChanges changes, string commitSha)
    {
        foreach (var change in changes)
        {
            var fileExtension = Path.GetExtension(change.Path);
            if (ExcludedExtensions.ContainsKey(fileExtension))
                continue;
            if (change.Path.IndexOf("/locales/", StringComparison.OrdinalIgnoreCase) >= 0 &&
                change.Path.IndexOf("/locales/en-US/", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            bool excluded = false;
            foreach (var excludedFolder in repoInfo.ExcludedFolders)
            {
                if (change.Path.IndexOf(excludedFolder.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    excluded = true;
                    break;
                }
            }
            if (excluded)
                continue;

            var oldBlob = repo.Lookup<Blob>(change.OldOid);
            var newBlob = repo.Lookup<Blob>(change.Oid);
            var contentChange = repo.Diff.Compare(oldBlob, newBlob);
            if (contentChange == null)
                continue;

            // Create list of lines which have text content
            var textBlocks = new List<string>();
            var textBlock = new StringBuilder();
            var previousLine = int.MinValue;
            foreach (var addedLine in contentChange.AddedLines)
            {
                var cleanLine = CleanLine(addedLine.Content);
                if (addedLine.LineNumber - 1 != previousLine && 0 < textBlock.Length || textBlock.Length + cleanLine.Length > MaxBlockSize)
                {
                    textBlocks.Add(textBlock.ToString());
                    textBlock.Clear();
                }

                // Skip very short lines and lines with only special characters
                if (cleanLine.Length > 5 &&
                    !string.IsNullOrWhiteSpace(cleanLine) &&
                    !SpecialCharactersOnly().IsMatch(cleanLine))
                    textBlock.Append(cleanLine);
                previousLine = addedLine.LineNumber;

                if (IsShuttingDown)
                    return;
            }

            // Add the last block
            if (0 < textBlock.Length)
            {
                textBlocks.Add(textBlock.ToString());
                textBlock.Clear();
            }

            if (textBlocks.Count == 0)
                continue;

            if (textBlocks.Count < EmbeddingBatchSize)
            {
                var embeddings = _embeddingClient.GenerateEmbeddings(textBlocks);
                foreach (var embedding in embeddings.Value)
                {
                    repoInfo.VectorDB.Insert(embedding.ToFloats(), new Sha1(commitSha), null);
                }
            }
            else // use batch embeddings
            {
                for (var i = 0; i < textBlocks.Count; i += EmbeddingBatchSize)
                {
                    var embeddings = _embeddingClient.GenerateEmbeddings(textBlocks.Skip(i).Take(EmbeddingBatchSize));
                    foreach (var embedding in embeddings.Value)
                    {
                        repoInfo.VectorDB.Insert(embedding.ToFloats(), new Sha1(commitSha), null);
                    }
                    if (IsShuttingDown)
                        return;
                }
            }


            if (IsShuttingDown)
                return;
        }
    }

    /// <summary>
    /// Cleans the line from special characters and data URIs to save tokens
    /// </summary>
    private string CleanLine(string content)
    {
        content = content.Trim();
        var match = DataImage().Match(content);
        if (match.Success)
        {
            if (match.Index > 0)
            {
                var endChar = content[match.Index - 1];
                var endIndex = content.IndexOf(endChar, match.Index + match.Length);
                if (endIndex >= 0)
                    content = content.Remove(match.Index-1, endIndex - match.Index + 2);
                else
                    content = content.Remove(match.Index);
            }
            else
            {
                // Remove until the end of the line
                content = content.Remove(match.Index);
            }
        }
        content = content.Trim([',', ';', ':', '\'', '"', '`', '=', '+', '(', ')', '{', '}', '[', ']']);

        content = content.Replace(" { ", " ");
        content = content.Replace(" } ", " ");
        content = content.Replace(" = ", " ");
        content = content.Replace(" + ", " ");
        content = content.Replace(" - ", " ");
        content = content.Replace(" * ", " ");
        content = content.Replace(" / ", " ");
        content = content.Replace(" % ", " ");

        content = content.Replace(": '", ": ");
        content = content.Replace(": `${", ": ");
        content = content.Replace("\": \"", ": ");
        content = content.Replace("?: ", ": ");

        content = content.Replace("', '", ", ");
        content = content.Replace("', ", ", ");
        content = content.Replace(", '", ", ");

        content = content.Replace(" == ", " ");
        content = content.Replace(" => ", " ");
        content = content.Replace(" += ", " ");
        content = content.Replace(" -= ", " ");
        content = content.Replace(" *= ", " ");
        content = content.Replace(" /= ", " ");
        content = content.Replace(" %= ", " ");

        content = content.Replace(" const ", " ");
        content = content.Replace("const ", " ");
        content = content.Replace(" return ", " ");
        content = content.Replace("return ", " ");

        content = CompressSpaces().Replace(content, " ");

        return content + "\n";
    }

    [GeneratedRegex("^[ !@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]*$")]
    private static partial Regex SpecialCharactersOnly();

    [GeneratedRegex("\\s+")]
    private static partial Regex CompressSpaces();

    [GeneratedRegex("data:image\\/(svg\\+xml|png|jpeg);base64,")]
    private static partial Regex DataImage();
}
