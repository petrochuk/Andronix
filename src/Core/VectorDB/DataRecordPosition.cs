namespace Andronix.Core.VectorDB;

[DebuggerDisplay("{Distance}: {DataRecord.DataIndex} {DataRecord.Data}")]
public class DataRecordPosition<T> : IComparable<DataRecordPosition<T>> where T : IDataIndex, new()
{
    public float Distance { get; }
    public DataRecord<T> DataRecord { get; }

    public DataRecordPosition(float distance, DataRecord<T> dataRecord)
    {
        Distance = distance;
        DataRecord = dataRecord;
    }

    public int CompareTo(DataRecordPosition<T>? obj)
    {
        if (obj == null)
            return 1;

        if (Distance != obj.Distance)
        {
            return Distance.CompareTo(obj.Distance);
        }

        if (!DataRecord.DataIndex.Equals(obj.DataRecord.DataIndex))
        {
            return DataRecord.DataIndex.CompareTo(obj.DataRecord.DataIndex);
        }

        return 0;
    }
}
