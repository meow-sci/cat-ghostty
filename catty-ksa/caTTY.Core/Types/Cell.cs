using System;

namespace caTTY.Core.Types;

/// <summary>
/// Represents a single character cell in the terminal screen buffer.
/// Contains the character and its associated SGR attributes.
/// </summary>
public readonly struct Cell : IEquatable<Cell>
{
    /// <summary>
    /// The character stored in this cell. Default is space character.
    /// </summary>
    public char Character { get; }

    /// <summary>
    /// The SGR attributes applied to this cell.
    /// </summary>
    public SgrAttributes Attributes { get; }

    /// <summary>
    /// Creates a new cell with the specified character and default attributes.
    /// </summary>
    /// <param name="character">The character to store in this cell</param>
    public Cell(char character) : this(character, SgrAttributes.Default)
    {
    }

    /// <summary>
    /// Creates a new cell with the specified character and attributes.
    /// </summary>
    /// <param name="character">The character to store in this cell</param>
    /// <param name="attributes">The SGR attributes for this cell</param>
    public Cell(char character, SgrAttributes attributes)
    {
        Character = character;
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the default empty cell (space character with default attributes).
    /// This represents both "unset" and "space" - we treat them the same.
    /// </summary>
    public static Cell Empty => new(' ', SgrAttributes.Default);

    /// <summary>
    /// Creates a cell with a space character and default attributes.
    /// </summary>
    public static Cell Space => new(' ', SgrAttributes.Default);

    /// <summary>
    /// Determines whether the specified Cell is equal to the current Cell.
    /// </summary>
    /// <param name="other">The Cell to compare with the current Cell</param>
    /// <returns>True if the specified Cell is equal to the current Cell; otherwise, false</returns>
    public bool Equals(Cell other) => Character == other.Character && Attributes.Equals(other.Attributes);

    /// <summary>
    /// Determines whether the specified object is equal to the current Cell.
    /// </summary>
    /// <param name="obj">The object to compare with the current Cell</param>
    /// <returns>True if the specified object is equal to the current Cell; otherwise, false</returns>
    public override bool Equals(object? obj) => obj is Cell other && Equals(other);

    /// <summary>
    /// Returns the hash code for this Cell.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code</returns>
    public override int GetHashCode() => HashCode.Combine(Character, Attributes);

    /// <summary>
    /// Determines whether two Cell instances are equal.
    /// </summary>
    /// <param name="left">The first Cell to compare</param>
    /// <param name="right">The second Cell to compare</param>
    /// <returns>True if the Cell instances are equal; otherwise, false</returns>
    public static bool operator ==(Cell left, Cell right) => left.Equals(right);

    /// <summary>
    /// Determines whether two Cell instances are not equal.
    /// </summary>
    /// <param name="left">The first Cell to compare</param>
    /// <param name="right">The second Cell to compare</param>
    /// <returns>True if the Cell instances are not equal; otherwise, false</returns>
    public static bool operator !=(Cell left, Cell right) => !left.Equals(right);

    /// <summary>
    /// Returns a string representation of the Cell.
    /// </summary>
    /// <returns>A string that represents the current Cell</returns>
    public override string ToString() => $"Cell('{Character}', {Attributes})";
}