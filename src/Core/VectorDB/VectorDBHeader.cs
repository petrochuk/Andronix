using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Andronix.Core.VectorDB;

internal class VectorDBHeader
{
    public int FileVersion { get; set; } = 1;
    public int Dimensions { get; set; }
    public int Count { get; set; }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(FileVersion);
        writer.Write(Dimensions);
        writer.Write(Count);
    }

    public static VectorDBHeader ReadFromStream(BinaryReader reader)
    {
        var fileVersion = reader.ReadInt32();
        if (fileVersion <= 0 || fileVersion > 1)
            throw new FormatException($"File version {fileVersion} is not supported");

        var dimensions = reader.ReadInt32();
        if (dimensions <= 0)
            throw new FormatException($"Invalid dimensions {dimensions}");

        var count = reader.ReadInt32();
        if (count < 0)
            throw new FormatException($"Invalid count {count}");

        return new VectorDBHeader
        {
            FileVersion = fileVersion,
            Dimensions = dimensions,
            Count = count
        };
    }
}
