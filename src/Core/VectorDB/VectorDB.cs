using System.Numerics.Tensors;
using Andronix.Core.Collections;

namespace Andronix.Core.VectorDB;

public interface IVectorDB<T> where T : IDataIndex, new()
{
    void Insert(VectorMemory vector, T dataIndex, string? data = null, uint scopes = uint.MaxValue);
    void Write(string filePath);
    void Read(string filePath);
    IList<DataRecord<T>> Find(VectorMemory target, float minDistance = float.MaxValue, int maxResultCount = int.MaxValue, uint scope = uint.MaxValue);
    IList<DataRecordPosition<T>> FindWithDistance(VectorMemory target, float minDistance = float.MaxValue, int maxResultCount = int.MaxValue, uint scope = uint.MaxValue);
    IDictionary<string, string> Headers { get; }
}

/// <summary>
/// In-memory vector database implementation using KD-Tree for fast nearest neighbor search
/// </summary>
public class VectorDB<T> : IVectorDB<T> where T : IDataIndex, new()
{
    private int _dimensions;
    private KdTreeNode<T>? _root = null;
    private int _count = 0;

    /// <summary>
    /// VectorDB constructor
    /// </summary>
    /// <param name="dimensions">Default to 3072 which is the number for text-embedding-3-large model</param>
    /// <exception cref="ArgumentException"></exception>
    public VectorDB(int dimensions = 3072)
    {
        if (dimensions <= 0)
            throw new ArgumentException("Dimension count must be positive number");

        _dimensions = dimensions;
    }

    /// <summary>
    /// VectorDB constructor
    /// </summary>
    /// <param name="filePath"></param>
    public VectorDB(string filePath)
    {
        Read(filePath);
    }

    /// <summary>
    /// Number of vectors in the database
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// Number of dimensions in the vectors
    /// </summary>
    public int Dimensions => _dimensions;

    public IDictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

    /// <summary>
    /// Insert a vector into the database with associated data
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="dataIndex"></param>
    /// <param name="data"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Insert(VectorMemory vector, T dataIndex, string? data = null, uint scopes = uint.MaxValue)
    {
        var dataRecord = new DataRecord<T>(vector, dataIndex, data, scopes);
        Insert(dataRecord);
    }

    /// <summary>
    /// Insert a data record into the database
    /// </summary>
    /// <param name="dataRecord"></param>
    public void Insert(DataRecord<T> dataRecord)
    {
        _ = dataRecord ?? throw new ArgumentNullException(nameof(dataRecord));

        if (dataRecord.Vector.Length != _dimensions)
            throw new ArgumentException("Vector length must match dimension count");

        _root = Insert(_root, 0, dataRecord);
        _count++;
    }

    private KdTreeNode<T> Insert(KdTreeNode<T>? node, int depth, DataRecord<T> dataRecord)
    {
        var splitIndex = depth % _dimensions;
        var v = dataRecord.Vector.Span[splitIndex];

        if (node == null)
            return new KdTreeNode<T>(splitIndex, v, dataRecord);

        if (v < node.SplitValue)
            node.Left = Insert(node.Left, depth + 1, dataRecord);
        else
            node.Right = Insert(node.Right, depth + 1, dataRecord);

        return node;
    }

    /// <summary>
    /// Find closest n vectors to the target vector
    /// </summary>
    /// <param name="target"></param>
    /// <param name="maxResultCount"></param>
    /// <returns></returns>
    public IList<DataRecord<T>> Find(VectorMemory target,
        float minDistance = float.MaxValue,
        int maxResultCount = int.MaxValue,
        uint scope = uint.MaxValue)
    {
        // Two sorted lists to keep track of the best records and their distances to avoid returning duplicates for the same index
        var bestRecords = new SortedList<T, MutablePair<float, DataRecord<T>>>(ResultCapacity(maxResultCount));
        var bestDistance = new SortedList<DataRecordPosition<T>, T>(ResultCapacity(maxResultCount));

        FindNearestNeighbors(_root, target, bestRecords, maxResultCount, minDistance, bestDistance, scope);

        return bestDistance.Select(p => p.Key.DataRecord).ToList();
    }

    public IList<DataRecordPosition<T>> FindWithDistance(VectorMemory target,
        float minDistance = float.MaxValue,
        int maxResultCount = int.MaxValue,
        uint scope = uint.MaxValue)
    {
        // Two sorted lists to keep track of the best records and their distances to avoid returning duplicates for the same index
        var bestRecords = new SortedList<T, MutablePair<float, DataRecord<T>>>(ResultCapacity(maxResultCount));
        var bestDistance = new SortedList<DataRecordPosition<T>, T>(ResultCapacity(maxResultCount));

        FindNearestNeighbors(_root, target, bestRecords, maxResultCount, minDistance, bestDistance, scope);

        return bestDistance.Keys.ToList();
    }

    private int ResultCapacity(int maxResultCount)
    {
        return maxResultCount < _count ? maxResultCount : _count;
    }

    private void FindNearestNeighbors(KdTreeNode<T>? node, VectorMemory target, SortedList<T, MutablePair<float, DataRecord<T>>> bestRecords,
        int maxResultCount, float minDistance, SortedList<DataRecordPosition<T>, T> bestDistance, uint scope)
    {
        if (node == null)
            return;

        if ((node.DataRecord.Scopes & scope) != 0)
        {
            var distance = TensorPrimitives.Distance(node.DataRecord.Vector.Span, target.Span);
            if (distance < minDistance)
            {
                if (bestRecords.TryGetValue(node.DataRecord.DataIndex, out var bestRecord))
                {
                    if (distance < bestRecord.Item1)
                    {
                        bestDistance.Remove(new(bestRecord.Item1, node.DataRecord));
                        bestRecord.Item1 = distance;
                        bestRecord.Item2 = node.DataRecord;
                        bestDistance.Add(new(distance, node.DataRecord), node.DataRecord.DataIndex);
                    }
                }
                else
                {
                    if (bestRecords.Count < maxResultCount)
                    {
                        bestDistance.Add(new (distance, node.DataRecord), node.DataRecord.DataIndex);
                        bestRecords.Add(node.DataRecord.DataIndex, new (distance, node.DataRecord));
                    }
                    else if (distance < bestDistance.Keys[bestDistance.Count - 1].Distance)
                    {
                        bestRecords.Remove(bestDistance.Values[bestDistance.Count - 1]);
                        bestDistance.RemoveAt(bestDistance.Count - 1);
                        bestRecords.Add(node.DataRecord.DataIndex, new(distance, node.DataRecord));
                        bestDistance.Add(new(distance, node.DataRecord), node.DataRecord.DataIndex);
                    }
                }
            }
        }

        var cd = node.SplitIndex;
        var nextNode = target.Span[cd] < node.DataRecord.Vector.Span[cd] ? node.Left : node.Right;
        var otherNode = target.Span[cd] < node.DataRecord.Vector.Span[cd] ? node.Right : node.Left;

        FindNearestNeighbors(nextNode, target, bestRecords, maxResultCount, minDistance, bestDistance, scope);

        if (bestRecords.Count < maxResultCount || Math.Abs(node.DataRecord.Vector.Span[cd] - target.Span[cd]) < bestDistance.Keys[bestDistance.Count - 1].Distance)
        {
            FindNearestNeighbors(otherNode, target, bestRecords, maxResultCount, minDistance, bestDistance, scope);
        }
    }

    /// <summary>
    /// Write the database to a file
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="ArgumentException"></exception>
    public void Write(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required");

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
        fileStream.SetLength(0);
        using var writer = new BinaryWriter(fileStream);

        var header = new VectorDBHeader { Dimensions = _dimensions, Count = _count };
        header.WriteToStream(writer);

        if (_root != null)
            _root.WriteToStream(writer);
    }

    /// <summary>
    /// Read the database from a file
    /// </summary>
    /// <param name="filePath"></param>
    public void Read(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream);
        var header = VectorDBHeader.ReadFromStream(reader);
        _dimensions = header.Dimensions;
        _count = header.Count;
        _root = KdTreeNode<T>.ReadFromStream(reader, _dimensions);
    }
}
