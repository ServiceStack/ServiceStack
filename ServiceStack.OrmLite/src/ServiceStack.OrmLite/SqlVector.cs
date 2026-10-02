using System;

namespace ServiceStack.OrmLite;

public static partial class Sql
{
    /// <summary>
    /// The cosine distance of a [Vector] column and a vector, from 0 for the same direction to 2 for the opposite.
    /// Order by it to find the most similar rows, e.g:
    /// <para>db.From&lt;Doc&gt;().OrderBy(x => Sql.CosineDistance(x.Embedding, queryVector)).Take(5)</para>
    /// </summary>
    public static double CosineDistance(float[] vector, float[] other)
    {
        AssertSameLength(vector, other);
        double dot = 0, lengthA = 0, lengthB = 0;
        for (var i = 0; i < vector.Length; i++)
        {
            dot += (double)vector[i] * other[i];
            lengthA += (double)vector[i] * vector[i];
            lengthB += (double)other[i] * other[i];
        }
        return 1 - dot / (Math.Sqrt(lengthA) * Math.Sqrt(lengthB));
    }

    /// <summary>
    /// The straight-line (Euclidean) distance of a [Vector] column and a vector, e.g:
    /// <para>db.From&lt;Doc&gt;().OrderBy(x => Sql.L2Distance(x.Embedding, queryVector)).Take(5)</para>
    /// </summary>
    public static double L2Distance(float[] vector, float[] other)
    {
        AssertSameLength(vector, other);
        double sum = 0;
        for (var i = 0; i < vector.Length; i++)
        {
            var diff = (double)vector[i] - other[i];
            sum += diff * diff;
        }
        return Math.Sqrt(sum);
    }

    /// <summary>
    /// The inner (dot) product of a [Vector] column and a vector, negated so that smaller is more similar like the
    /// other distances. Supported by PostgreSQL, SQL Server and MySQL.
    /// </summary>
    public static double NegativeInnerProduct(float[] vector, float[] other)
    {
        AssertSameLength(vector, other);
        double dot = 0;
        for (var i = 0; i < vector.Length; i++)
            dot += (double)vector[i] * other[i];
        return -dot;
    }

    private static void AssertSameLength(float[] vector, float[] other)
    {
        if (vector == null)
            throw new ArgumentNullException(nameof(vector));
        if (other == null)
            throw new ArgumentNullException(nameof(other));
        if (vector.Length != other.Length)
            throw new ArgumentException($"Vectors must have the same dimensions, but have {vector.Length} and {other.Length}");
    }
}
