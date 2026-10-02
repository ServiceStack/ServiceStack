using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Store a float[] property in a vector column of an RDBMS with vector support, to find the rows most similar to an
/// embedding, e.g:
/// <para>[Vector(1536)] public float[] Embedding { get; set; }</para>
/// Add [Index] to also create a vector index, where the RDBMS has one.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class VectorAttribute(int dimensions) : AttributeBase
{
    /// <summary>
    /// How many values each vector has, which is the size of the embeddings of the model that creates them
    /// </summary>
    public int Dimensions { get; } = dimensions;

    /// <summary>
    /// The distance its vector index is created for, which is the one queries should order by to use it
    /// </summary>
    public VectorDistance Distance { get; set; } = VectorDistance.Cosine;
}

/// <summary>
/// How the similarity of 2 vectors is measured, where smaller is more similar
/// </summary>
public enum VectorDistance
{
    /// <summary>
    /// The angle between 2 vectors, ignoring their length. The usual choice for text embeddings.
    /// </summary>
    Cosine,
    /// <summary>
    /// The straight-line (Euclidean) distance between 2 vectors
    /// </summary>
    L2,
    /// <summary>
    /// The inner (dot) product of 2 vectors negated, so that smaller is more similar. For normalized vectors.
    /// </summary>
    NegativeInnerProduct,
}
