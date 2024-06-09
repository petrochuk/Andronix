namespace Andronix.Core;

public static class JsonSchemaExtensions
{
    public static string ToJsonType(this Type type)
    {
        if (type == typeof(string))
            return "string";
        else if (type == typeof(int))
            return "integer";
        else if (type == typeof(long))
            return "integer";
        else if (type == typeof(bool))
            return "boolean";
        else if (type == typeof(DateTime))
            return "string";
        else if (type == typeof(Guid))
            return "string";
        else if (type == typeof(decimal))
            return "number";
        else if (type == typeof(double))
            return "number";
        else if (type == typeof(float))
            return "number";
        else if (type == typeof(object))
            return "object";

        throw new NotSupportedException($"Type {type} is not supported.");
    }
}
