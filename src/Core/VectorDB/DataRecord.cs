namespace Andronix.Core.VectorDB;

[DebuggerDisplay("{DataIndex}: {Data}")]
public class DataRecord<T>(VectorMemory vector, T dataIndex, string? data = null, uint scopes = uint.MaxValue) where T : IDataIndex, new()
{
    public VectorMemory Vector { get; } = vector;
    public uint Scopes { get; } = scopes;
    public T DataIndex { get; } = dataIndex;
    public string? Data { get; } = data;

    public void WriteToStream(BinaryWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));

        writer.Write(Scopes);
        DataIndex.WriteToStream(writer);
        writer.Write(Data ?? string.Empty);
        foreach (var value in Vector.Span)
        {
            writer.Write(value);
        }
    }

    public static DataRecord<T> ReadFromStream(BinaryReader reader, int dimensions)
    {
        _ = reader ?? throw new ArgumentNullException(nameof(reader));
        var scopes = reader.ReadUInt32();
        T dataIndex = new T();
        dataIndex.ReadFromStream(reader);
        var data = reader.ReadString();
        var vector = new List<float>(dimensions);
        for (var i = 0; i < dimensions; i++)
        {
            vector.Add(reader.ReadSingle());
        }

        return new DataRecord<T>(vector.ToArray(), dataIndex, data, scopes);
    }
}
