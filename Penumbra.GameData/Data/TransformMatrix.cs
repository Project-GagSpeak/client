namespace Penumbra.GameData.Data;

/// <summary>
/// Represents a transform matrix with the following values, stored in row-major order:
/// [ xx xy xz x0 ]
/// [ yx yy yz y0 ]
/// [ zx zy zz z0 ]
/// [  0  0  0  1 ]
/// Where x0, y0 and z0 are the translation vector, and the 9 other numbers are the scaling/rotation values.
/// </summary>
/// <remarks> This type is, notably, part of the PBD file format. </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct TransformMatrix : IEquatable<TransformMatrix>
{
    /// <summary> A matrix filled with 0. </summary>
    public static readonly TransformMatrix Zero = new(Vector4.Zero, Vector4.Zero, Vector4.Zero);

    /// <summary> The identity matrix with 1 on diagonals and 0 everywhere else. </summary>
    public static readonly TransformMatrix Identity = new(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ);

    /// <summary> Represents an invalid matrix. </summary>
    private static readonly TransformMatrix NaN = new(new Vector4(float.NaN), new Vector4(float.NaN), new Vector4(float.NaN));

    /// <summary> The top row. </summary>
    public readonly Vector4 XRow;

    /// <summary> The middle row. </summary>
    public readonly Vector4 YRow;

    /// <summary> The bottom row (aside from the fixed one). </summary>
    public readonly Vector4 ZRow;

    /// <summary> The left column. </summary>
    public readonly Vector3 XColumn
        => new(XRow.X, YRow.X, ZRow.X);

    /// <summary> The middle column in the 3x3 matrix. </summary>
    public readonly Vector3 YColumn
        => new(XRow.Y, YRow.Y, ZRow.Y);


    /// <summary> The right column in the 3x3 matrix. </summary>
    public readonly Vector3 ZColumn
        => new(XRow.Z, YRow.Z, ZRow.Z);

    /// <summary> The right-most column. </summary>
    public readonly Vector3 Translation
        => new(XRow.W, YRow.W, ZRow.W);

    /// <summary> Create a new transform matrix from its 3 non-fixed rows. </summary>
    public TransformMatrix(Vector4 xRow, Vector4 yRow, Vector4 zRow)
    {
        XRow = xRow;
        YRow = yRow;
        ZRow = zRow;
    }

    /// <summary> Create a new transform matrix from its 3x3 matrix and the translation vector. </summary>
    public TransformMatrix(Vector3 xColumn, Vector3 yColumn, Vector3 zColumn, Vector3 translation)
    {
        XRow = new Vector4(xColumn.X, yColumn.X, zColumn.X, translation.X);
        YRow = new Vector4(xColumn.Y, yColumn.Y, zColumn.Y, translation.Y);
        ZRow = new Vector4(xColumn.Z, yColumn.Z, zColumn.Z, translation.Z);
    }

    /// <summary> Return a new transform matrix with a single value changed. </summary>
    public TransformMatrix ChangeValue(int i, int j, float value)
    {
        if (i is < 0 or > 2)
            return this;
        if (j is < 0 or > 3)
            return this;

        var xRow = XRow;
        if (i == 0)
            xRow[j] = value;
        var yRow = YRow;
        if (i == 1)
            yRow[j] = value;
        var zRow = ZRow;
        if (i == 2)
            zRow[j] = value;
        return new TransformMatrix(xRow, yRow, zRow);
    }

    /// <summary> Create a transform matrix that only translates. </summary>
    public static TransformMatrix CreateTranslation(Vector3 translation)
        => new(
            new Vector4(1.0f, 0.0f, 0.0f, translation.X),
            new Vector4(0.0f, 1.0f, 0.0f, translation.Y),
            new Vector4(0.0f, 0.0f, 1.0f, translation.Z));

    /// <summary> Create a transform matrix that only scales uniformly. </summary>
    public static TransformMatrix CreateScale(Vector3 scale)
        => new(
            new Vector4(scale.X, 0.0f,    0.0f,    0.0f),
            new Vector4(0.0f,    scale.Y, 0.0f,    0.0f),
            new Vector4(0.0f,    0.0f,    scale.Z, 0.0f));

    /// <summary> Create a transform matrix that rotates according to a quaternion matrix. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TransformMatrix CreateRotation(Quaternion rotation)
        => (TransformMatrix)Matrix4x4.CreateFromQuaternion(rotation);

    /// <summary> Create a transform matrix from a (non-uniform) scale, a rotation and a translation. </summary>
    public static TransformMatrix Compose(Vector3 scale, Quaternion rotation, Vector3 translation)
        => CreateTranslation(translation) * CreateRotation(rotation) * CreateScale(scale);

    /// <summary> Try to decompose a transform matrix into separate scale, rotation and translation components. </summary>
    public bool TryDecompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        => Matrix4x4.Decompose(this, out scale, out rotation, out translation);

    /// <inheritdoc/>
    public bool Equals(TransformMatrix other)
        => XRow == other.XRow && YRow == other.YRow && ZRow == other.ZRow;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is TransformMatrix other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
        => unchecked(XRow.GetHashCode() + YRow.GetHashCode() * 31 + ZRow.GetHashCode() * 961);

    /// <inheritdoc/>
    public override string ToString()
        => $"TransformMatrix {{ X = {XRow}, Y = {YRow}, Z = {ZRow} }}";

    /// <summary> Return a new transform matrix that is a second transformation applied after this one. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TransformMatrix Append(in TransformMatrix mat)
        => mat * this;

    /// <summary> Return a new transform matrix that is this applied after another transformation. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TransformMatrix Prepend(in TransformMatrix mat)
        => this * mat;

    /// <summary> Apply this transformation to a 3D value. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Apply(Vector3 vec)
        => this * vec;

    /// <summary> Apply this transformation to a 4D value. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Apply(Vector4 vec)
        => this * vec;

    /// <summary> Compute the determinant of this matrix, which is the same as the determinant of the 3x3 matrix. </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public float Determinant()
        => XRow.X * (YRow.Y * ZRow.Z - YRow.Z * ZRow.Y)
          - XRow.Y * (YRow.X * ZRow.Z - YRow.Z * ZRow.X)
          + XRow.Z * (YRow.X * ZRow.Y - YRow.Y * ZRow.X);

    /// <summary> Invert the matrix. </summary>
    public TransformMatrix Invert()
        => TryInvert(out var inv) ? inv : throw new DivideByZeroException($"{this} is not invertible");

    /// <summary> Try to invert the matrix. </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public bool TryInvert(out TransformMatrix inv)
    {
        var xColumn = XColumn;
        var yColumn = YColumn;
        var zColumn = ZColumn;

        var coXColumn = Vector3.Cross(yColumn, zColumn);
        var det       = Vector3.Dot(xColumn, coXColumn);

        if (det == 0.0f)
        {
            inv = NaN;
            return false;
        }

        var invDet = 1.0f / det;

        var coYColumn = Vector3.Cross(zColumn, xColumn);
        var coZColumn = Vector3.Cross(xColumn, yColumn);

        var negTranslation = -Translation;

        inv = new TransformMatrix(
            new Vector4(coXColumn, Vector3.Dot(coXColumn, negTranslation)) * invDet,
            new Vector4(coYColumn, Vector3.Dot(coYColumn, negTranslation)) * invDet,
            new Vector4(coZColumn, Vector3.Dot(coZColumn, negTranslation)) * invDet);

        return true;
    }

    public float this[int i, int j]
        => i switch
        {
            0 => XRow[j],
            1 => YRow[j],
            2 => ZRow[j],
            3 => j == 3 ? 1 : 0,
            _ => 0,
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(in TransformMatrix lhs, in TransformMatrix rhs)
        => lhs.Equals(rhs);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(in TransformMatrix lhs, in TransformMatrix rhs)
        => !lhs.Equals(rhs);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static TransformMatrix operator +(in TransformMatrix lhs, in TransformMatrix rhs)
        => new(lhs.XRow + rhs.XRow, lhs.YRow + rhs.YRow, lhs.ZRow + rhs.ZRow);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static TransformMatrix operator *(float factor, in TransformMatrix mat)
        => new(factor * mat.XRow, factor * mat.YRow, factor * mat.ZRow);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static TransformMatrix operator *(in TransformMatrix mat, float factor)
        => new(mat.XRow * factor, mat.YRow * factor, mat.ZRow * factor);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector3 operator *(in TransformMatrix mat, Vector3 vec)
        => mat * new Vector4(vec, 1.0f);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static Vector3 operator *(in TransformMatrix mat, Vector4 vec)
        => new(Vector4.Dot(mat.XRow, vec), Vector4.Dot(mat.YRow, vec), Vector4.Dot(mat.ZRow, vec));

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static TransformMatrix operator *(in TransformMatrix lhs, in TransformMatrix rhs)
        => new(
            lhs * new Vector4(rhs.XColumn,     0.0f),
            lhs * new Vector4(rhs.YColumn,     0.0f),
            lhs * new Vector4(rhs.ZColumn,     0.0f),
            lhs * new Vector4(rhs.Translation, 1.0f));

    /// <summary> Converts the given transform matrix into an equivalent <see cref="Matrix4x4"/>. </summary>
    /// <remarks> The resulting matrix will be transposed, to fit how transforms are represented as <see cref="Matrix4x4"/>. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static implicit operator Matrix4x4(in TransformMatrix mat)
        => new(
            mat.XRow.X, mat.YRow.X, mat.ZRow.X, 0.0f,
            mat.XRow.Y, mat.YRow.Y, mat.ZRow.Y, 0.0f,
            mat.XRow.Z, mat.YRow.Z, mat.ZRow.Z, 0.0f,
            mat.XRow.W, mat.YRow.W, mat.ZRow.W, 1.0f);

    /// <summary> Converts the given <see cref="Matrix4x4"/> into an equivalent transform matrix, if possible. </summary>
    /// <remarks> The resulting matrix will be transposed, due to how transforms are represented as <see cref="Matrix4x4"/>. </remarks>
    public static explicit operator TransformMatrix(in Matrix4x4 mat)
    {
        if (mat.M14 != 0.0f || mat.M24 != 0.0f || mat.M34 != 0.0f || mat.M44 != 1.0f)
            throw new InvalidCastException(
                $"Invalid transform matrix, expected the fourth column to be <0 0 0 1>, found <{mat.M14} {mat.M24} {mat.M34} {mat.M44}>");

        return new TransformMatrix(
            new Vector4(mat.M11, mat.M21, mat.M31, mat.M41),
            new Vector4(mat.M12, mat.M22, mat.M32, mat.M42),
            new Vector4(mat.M13, mat.M23, mat.M33, mat.M43));
    }
}
