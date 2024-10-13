namespace Andronix.Core.VectorDB;

/// <summary>
/// Represents a tree node in a KD-Tree in VectorDB
/// </summary>
[DebuggerDisplay("SplitIndex: {SplitIndex} DataRecord: {DataRecord?.Data}")]
internal class KdTreeNode<T> where T : IDataIndex, new()
{
    public int SplitIndex { get; }
    public float SplitValue { get; }
    public KdTreeNode<T>? Left { get; set; } = null;
    public KdTreeNode<T>? Right { get; set; } = null;
    public DataRecord<T> DataRecord { get; }

    public KdTreeNode(int splitIndex, float splitValue, DataRecord<T> dataRecord)
    {
        SplitIndex = splitIndex;
        SplitValue = splitValue;
        DataRecord = dataRecord;
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(SplitIndex);
        writer.Write(SplitValue);
        DataRecord.WriteToStream(writer);
        writer.Write(Left != null);
        if (Left != null)
        {
            Left.WriteToStream(writer);
        }
        writer.Write(Right != null);
        if (Right != null)
        {
            Right.WriteToStream(writer);
        }
    }

    public static KdTreeNode<T> ReadFromStream(BinaryReader reader, int dimensions)
    {
        var splitIndex = reader.ReadInt32();
        var splitValue = reader.ReadSingle();
        var dataRecord = DataRecord<T>.ReadFromStream(reader, dimensions);
        var node = new KdTreeNode<T>(splitIndex, splitValue, dataRecord);
        if (reader.ReadBoolean())
        {
            node.Left = ReadFromStream(reader, dimensions);
        }
        if (reader.ReadBoolean())
        {
            node.Right = ReadFromStream(reader, dimensions);
        }
        return node;
    }
}

