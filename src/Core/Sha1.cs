
using System.Collections;

namespace Andronix.Core;

public class Sha1 : IDataIndex
{
    const int HashSize = 20;

    public byte[] Hash { get; private set; }

    public Sha1()
    {
        Hash = new byte[HashSize];
    }

    public Sha1(string data)
    {
        // Convert SHA1 hash to byte array
        Hash = Enumerable.Range(0, data.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(data.Substring(x, 2), 16))
                         .ToArray();
    }

    public override string ToString()
    {
        return BitConverter.ToString(Hash).Replace("-", "");
    }

    public void ReadFromStream(BinaryReader reader)
    {
        Hash = reader.ReadBytes(HashSize);
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(Hash);
    }

    public int CompareTo(IDataIndex? other)
    {
        if (other is not Sha1 sha1)
            return -1;

        return ((IStructuralComparable)Hash).CompareTo(sha1.Hash, Comparer<byte>.Default);
    }

    public bool Equals(IDataIndex? other)
    {
        if (other is not Sha1 sha1)
            return false;

        return Hash.SequenceEqual(sha1.Hash);
    }
}
