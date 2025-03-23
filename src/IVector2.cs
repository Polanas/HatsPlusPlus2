//
//  Vector3i.cs
//
//  Copyright (C) OpenTK
//
//  This software may be modified and distributed under the terms
//  of the MIT license. See the LICENSE file for details.
//

using DuckGame;
using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace HatsPlusPlus;

/// <summary>
/// Represents a 2D vector using two 32-bit integer numbers.
/// </summary>
/// <remarks>
/// The Vector2i structure is suitable for interoperation with unmanaged code requiring two consecutive integers.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct IVector2 : IEquatable<IVector2>
{
    /// <summary>
    /// The X component of the Vector2i.
    /// </summary>
    public int X;

    /// <summary>
    /// The Y component of the Vector2i.
    /// </summary>
    public int Y;

    /// <summary>
    /// Initializes a new instance of the <see cref="IVector2"/> struct.
    /// </summary>
    /// <param name="value">The value that will initialize this instance.</param>
    public IVector2(int value)
    {
        X = value;
        Y = value;
    }

    public static IVector2 New(int x, int y) {
        return new IVector2(x, y);
    }

    public static IVector2 New(int x) {
        return new IVector2(x);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IVector2"/> struct.
    /// </summary>
    /// <param name="x">The X component of the Vector2i.</param>
    /// <param name="y">The Y component of the Vector2i.</param>
    public IVector2(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets or sets the value at the index of the vector.
    /// </summary>
    /// <param name="index">The index of the component from the vector.</param>
    /// <exception cref="IndexOutOfRangeException">Thrown if the index is less than 0 or greater than 1.</exception>
    public int this[int index]
    {
        get
        {
            if (index == 0)
            {
                return X;
            }

            if (index == 1)
            {
                return Y;
            }

            throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
        }

        set
        {
            if (index == 0)
            {
                X = value;
            }
            else if (index == 1)
            {
                Y = value;
            }
            else
            {
                throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
            }
        }
    }

    /// <summary>
    /// Gets the manhattan length of the vector.
    /// </summary>
    public int ManhattanLength => Math.Abs(X) + Math.Abs(Y);

    /// <summary>
    /// Gets the squared euclidean length of the vector.
    /// </summary>
    public int EuclideanLengthSquared => (X * X) + (Y * Y);

    /// <summary>
    /// Gets the euclidean length of the vector.
    /// </summary>
    public float EuclideanLength => (float)Math.Sqrt((X * X) + (Y * Y));

    /// <summary>
    /// Gets the perpendicular vector on the right side of this vector.
    /// </summary>
    public IVector2 PerpendicularRight => new IVector2(Y, -X);

    /// <summary>
    /// Gets the perpendicular vector on the left side of this vector.
    /// </summary>
    public IVector2 PerpendicularLeft => new IVector2(-Y, X);

    /// <summary>
    /// Defines a unit-length <see cref="IVector2"/> that points towards the X-axis.
    /// </summary>
    public static readonly IVector2 UnitX = new IVector2(1, 0);

    /// <summary>
    /// Defines a unit-length <see cref="IVector2"/> that points towards the Y-axis.
    /// </summary>
    public static readonly IVector2 UnitY = new IVector2(0, 1);

    /// <summary>
    /// Defines an instance with all components set to 0.
    /// </summary>
    public static readonly IVector2 Zero = new IVector2(0, 0);

    /// <summary>
    /// Defines an instance with all components set to 1.
    /// </summary>
    public static readonly IVector2 One = new IVector2(1, 1);

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    /// <param name="a">Left operand.</param>
    /// <param name="b">Right operand.</param>
    /// <returns>Result of operation.</returns>
    [Pure]
    public static IVector2 Add(IVector2 a, IVector2 b)
    {
        Add(in a, in b, out a);
        return a;
    }

    /// <summary>
    /// Adds two vectors.
    /// </summary>
    /// <param name="a">Left operand.</param>
    /// <param name="b">Right operand.</param>
    /// <param name="result">Result of operation.</param>
    public static void Add(in IVector2 a, in IVector2 b, out IVector2 result)
    {
        result.X = a.X + b.X;
        result.Y = a.Y + b.Y;
    }

    /// <summary>
    /// Subtract one Vector from another.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns>Result of subtraction.</returns>
    [Pure]
    public static IVector2 Subtract(IVector2 a, IVector2 b)
    {
        Subtract(in a, in b, out a);
        return a;
    }

    /// <summary>
    /// Subtract one Vector from another.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <param name="result">Result of subtraction.</param>
    public static void Subtract(in IVector2 a, in IVector2 b, out IVector2 result)
    {
        result.X = a.X - b.X;
        result.Y = a.Y - b.Y;
    }

    /// <summary>
    /// Multiplies a vector by an integer scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    [Pure]
    public static IVector2 Multiply(IVector2 vector, int scale)
    {
        Multiply(in vector, scale, out vector);
        return vector;
    }

    /// <summary>
    /// Multiplies a vector by an integer scalar.
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Multiply(in IVector2 vector, int scale, out IVector2 result)
    {
        result.X = vector.X * scale;
        result.Y = vector.Y * scale;
    }

    /// <summary>
    /// Multiplies a vector by the components a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    [Pure]
    public static IVector2 Multiply(IVector2 vector, IVector2 scale)
    {
        Multiply(in vector, in scale, out vector);
        return vector;
    }

    /// <summary>
    /// Multiplies a vector by the components of a vector (scale).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Multiply(in IVector2 vector, in IVector2 scale, out IVector2 result)
    {
        result.X = vector.X * scale.X;
        result.Y = vector.Y * scale.Y;
    }

    /// <summary>
    /// Divides a vector by a scalar using integer division, floor(a/b).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    [Pure]
    public static IVector2 Divide(IVector2 vector, int scale)
    {
        Divide(in vector, scale, out vector);
        return vector;
    }

    /// <summary>
    /// Divides a vector by a scalar using integer division, floor(a/b).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Divide(in IVector2 vector, int scale, out IVector2 result)
    {
        result.X = vector.X / scale;
        result.Y = vector.Y / scale;
    }

    /// <summary>
    /// Divides a vector by the components of a vector using integer division, floor(a/b).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the operation.</returns>
    [Pure]
    public static IVector2 Divide(IVector2 vector, IVector2 scale)
    {
        Divide(in vector, in scale, out vector);
        return vector;
    }

    /// <summary>
    /// Divides a vector by the components of a vector using integer division, floor(a/b).
    /// </summary>
    /// <param name="vector">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <param name="result">Result of the operation.</param>
    public static void Divide(in IVector2 vector, in IVector2 scale, out IVector2 result)
    {
        result.X = vector.X / scale.X;
        result.Y = vector.Y / scale.Y;
    }

    /// <summary>
    /// Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns>The component-wise minimum.</returns>
    [Pure]
    public static IVector2 ComponentMin(IVector2 a, IVector2 b)
    {
        a.X = Math.Min(a.X, b.X);
        a.Y = Math.Min(a.Y, b.Y);
        return a;
    }

    /// <summary>
    /// Returns a vector created from the smallest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <param name="result">The component-wise minimum.</param>
    public static void ComponentMin(in IVector2 a, in IVector2 b, out IVector2 result)
    {
        result.X = Math.Min(a.X, b.X);
        result.Y = Math.Min(a.Y, b.Y);
    }

    /// <summary>
    /// Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <returns>The component-wise maximum.</returns>
    [Pure]
    public static IVector2 ComponentMax(IVector2 a, IVector2 b)
    {
        a.X = Math.Max(a.X, b.X);
        a.Y = Math.Max(a.Y, b.Y);
        return a;
    }

    /// <summary>
    /// Returns a vector created from the largest of the corresponding components of the given vectors.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <param name="result">The component-wise maximum.</param>
    public static void ComponentMax(in IVector2 a, in IVector2 b, out IVector2 result)
    {
        result.X = Math.Max(a.X, b.X);
        result.Y = Math.Max(a.Y, b.Y);
    }

    /// <summary>
    /// Clamp a vector to the given minimum and maximum vectors.
    /// </summary>
    /// <param name="vec">Input vector.</param>
    /// <param name="min">Minimum vector.</param>
    /// <param name="max">Maximum vector.</param>
    /// <returns>The clamped vector.</returns>
    [Pure]
    public static IVector2 Clamp(IVector2 vec, IVector2 min, IVector2 max)
    {
        vec.X = MathHelper.Clamp(vec.X, min.X, max.X);
        vec.Y = MathHelper.Clamp(vec.Y, min.Y, max.Y);
        return vec;
    }

    /// <summary>
    /// Clamp a vector to the given minimum and maximum vectors.
    /// </summary>
    /// <param name="vec">Input vector.</param>
    /// <param name="min">Minimum vector.</param>
    /// <param name="max">Maximum vector.</param>
    /// <param name="result">The clamped vector.</param>
    public static void Clamp(in IVector2 vec, in IVector2 min, in IVector2 max, out IVector2 result)
    {
        result.X = MathHelper.Clamp(vec.X, min.X, max.X);
        result.Y = MathHelper.Clamp(vec.Y, min.Y, max.Y);
    }

    public IVector2 Yx
    {
        get => new IVector2(Y, X);
        set
        {
            Y = value.X;
            X = value.Y;
        }
    }

    /// <summary>
    /// Adds the specified instances.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Result of addition.</returns>
    [Pure]
    public static IVector2 operator +(IVector2 left, IVector2 right)
    {
        left.X += right.X;
        left.Y += right.Y;
        return left;
    }

    /// <summary>
    /// Subtracts the specified instances.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>Result of subtraction.</returns>
    [Pure]
    public static IVector2 operator -(IVector2 left, IVector2 right)
    {
        left.X -= right.X;
        left.Y -= right.Y;
        return left;
    }

    /// <summary>
    /// Negates the specified instance.
    /// </summary>
    /// <param name="vec">Operand.</param>
    /// <returns>Result of negation.</returns>
    [Pure]
    public static IVector2 operator -(IVector2 vec)
    {
        vec.X = -vec.X;
        vec.Y = -vec.Y;
        return vec;
    }

    /// <summary>
    /// Multiplies the specified instance by a scalar.
    /// </summary>
    /// <param name="vec">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    [Pure]
    public static IVector2 operator *(IVector2 vec, int scale)
    {
        vec.X *= scale;
        vec.Y *= scale;
        return vec;
    }

    /// <summary>
    /// Multiplies the specified instance by a scalar.
    /// </summary>
    /// <param name="scale">Left operand.</param>
    /// <param name="vec">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    [Pure]
    public static IVector2 operator *(int scale, IVector2 vec)
    {
        vec.X *= scale;
        vec.Y *= scale;
        return vec;
    }

    /// <summary>
    /// Component-wise multiplication between the specified instance by a scale vector.
    /// </summary>
    /// <param name="scale">Left operand.</param>
    /// <param name="vec">Right operand.</param>
    /// <returns>Result of multiplication.</returns>
    [Pure]
    public static IVector2 operator *(IVector2 vec, IVector2 scale)
    {
        vec.X *= scale.X;
        vec.Y *= scale.Y;
        return vec;
    }

    /// <summary>
    /// Divides the instance by a scalar using integer division, floor(a/b).
    /// </summary>
    /// <param name="vec">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the division.</returns>
    [Pure]
    public static IVector2 operator /(IVector2 vec, int scale)
    {
        vec.X /= scale;
        vec.Y /= scale;
        return vec;
    }

    /// <summary>
    /// Component-wise division between the specified instance by a scale vector.
    /// </summary>
    /// <param name="vec">Left operand.</param>
    /// <param name="scale">Right operand.</param>
    /// <returns>Result of the division.</returns>
    [Pure]
    public static IVector2 operator /(IVector2 vec, IVector2 scale)
    {
        vec.X /= scale.X;
        vec.Y /= scale.Y;
        return vec;
    }

    /// <summary>
    /// Compares the specified instances for equality.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if both instances are equal; false otherwise.</returns>
    public static bool operator ==(IVector2 left, IVector2 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares the specified instances for inequality.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if both instances are not equal; false otherwise.</returns>
    public static bool operator !=(IVector2 left, IVector2 right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IVector2"/> struct using a tuple containing the component
    /// values.
    /// </summary>
    /// <param name="values">A tuple containing the component values.</param>
    /// <returns>A new instance of the <see cref="IVector2"/> struct with the given component values.</returns>
    [Pure]
    public static implicit operator IVector2((int X, int Y) values)
    {
        return new IVector2(values.X, values.Y);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        return obj is IVector2 && Equals((IVector2)obj);
    }

    /// <inheritdoc/>
    public bool Equals(IVector2 other)
    {
        return X == other.X &&
               Y == other.Y;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return GetHashCode();
    }

    /// <summary>
    /// Deconstructs the vector into it's individual components.
    /// </summary>
    /// <param name="x">The X component of the vector.</param>
    /// <param name="y">The Y component of the vector.</param>
    [Pure]
    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}
