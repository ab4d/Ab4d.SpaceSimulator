using System;
using System.Runtime.CompilerServices;

namespace Ab4d.SpaceSimulator.Physics;

// Basic three-dimensional vector with double-precision fields.
public struct Vector3d
{
    public double X;
    public double Y;
    public double Z;

    public Vector3d(double value)
        : this(value, value, value)
    {
    }

    public Vector3d(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3d Zero => new();
    public static Vector3d One => new(1.0);

    public static bool IsFinite(Vector3d vector)
    {
        return double.IsFinite(vector.X) && double.IsFinite(vector.Y) && double.IsFinite(vector.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d operator +(Vector3d left, Vector3d right)
    {
        return new Vector3d(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d operator -(Vector3d left, Vector3d right)
    {
        return new Vector3d(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d operator *(Vector3d left, Vector3d right)
    {
        return new Vector3d(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d operator *(Vector3d left, double right) => left * new Vector3d(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d operator *(double left, Vector3d right) => right * new Vector3d(left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d operator /(Vector3d left, Vector3d right)
    {
        return new Vector3d(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d operator /(Vector3d value1, double value2) => value1 / new Vector3d(value2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Dot(Vector3d vector1, Vector3d vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d Cross(Vector3d vector1, Vector3d vector2)
    {
        return new Vector3d(
            vector1.Y * vector2.Z - vector1.Z * vector2.Y,
            vector1.Z * vector2.X - vector1.X * vector2.Z,
            vector1.X * vector2.Y - vector1.Y * vector2.X
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double Length() => Math.Sqrt(LengthSquared());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly double LengthSquared() => Dot(this, this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3d Normalize(Vector3d value) => value / value.Length();
}
