public interface IDataIndex : IComparable<IDataIndex>, IEquatable<IDataIndex>
{
    void WriteToStream(BinaryWriter writer);
    void ReadFromStream(BinaryReader reader);
}
