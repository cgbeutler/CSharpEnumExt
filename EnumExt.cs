using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace Vial.Extensions
{
  public static class EnumExt
  {
    /// <summary> Returns an enum value restricted to the given value range </summary>
    public static T Clamp<T>( this T t, T min, T max ) where T : struct, Enum => Enum<T>.Clamp( t, min, max );
    /// <summary> Returns an enum value restricted to the range of explicitly defined/named values. </summary>
    public static T ClampToDefinedRange<T>( this T t ) where T : struct, Enum => Enum<T>.ClampToDefinedRange( t );

    public static bool IsDefined<T>( this T t ) where T : struct, Enum => Enum<T>.IsDefined( t );

    /// <summary> Returns a value that is the combination of `self` and `flags`. Same as bitwise OR against `flags`. </summary>
    public static T WithFlags<T>( this T self, T flags ) where T : struct, Enum => Enum<T>.Bitwise.Or( self, flags );
    /// <summary> Returns a value that is `self` with `flags` removed. Same as a bitwise AND against inverted `flags`. </summary>
    public static T WithoutFlags<T>( this T self, T flags ) where T : struct, Enum => Enum<T>.Bitwise.AndNot( self, flags );
    /// <summary> Returns a value that is `self` with `flags` toggled. Same as a bitwise XOR with `b`. </summary>
    public static T WithFlagsToggled<T>( this T self, T flags ) where T : struct, Enum => Enum<T>.Bitwise.Xor( self, flags );

    /// <summary> Returns the display name for the enum value, if a System.ComponentModel.DisplayNameAttribute is defined. Null otherwise. </summary>
    public static string? DisplayName<T>( this T t ) where T : struct, Enum => Enum<T>.GetDisplayName( t );
    /// <summary> Returns the description for the enum value, if a System.ComponentModel.DescriptionAttribute is defined. Null otherwise. </summary>
    public static string? Description<T>( this T t ) where T : struct, Enum => Enum<T>.GetDescription( t );
  };



  public static class Enum<T>
    where T : struct, Enum
  {
    public static readonly Type Type = typeof( T );
    public static readonly Type UnderlyingType = Enum.GetUnderlyingType( typeof( T ) );

    /// <summary> True if the enum has the FlagsAttribute </summary>
    public static readonly bool HasFlagsAttribute = typeof( T ).GetCustomAttributes( typeof( FlagsAttribute ), false ).Length > 0;

    /// <summary> The enum-cast version of zero. Does not have to be defined explicitly by the enum. </summary>
    public static T Zero = Expression.Lambda<Func<T>>( Expression.Convert( Expression.Constant( (byte)0, UnderlyingType ), Type ) ).Compile().Invoke();

    private static readonly List<T> __values = Enum.GetValues( typeof( T ) ).Cast<T>().Distinct().OrderBy( v => v ).ToList();
    /// <summary> An ascending list of the enum values ordered by value. Duplicates removed. </summary>
    public static IReadOnlyList<T> Values => __values;

    /// <summary> An descending list of the enum values ordered by value. Duplicates removed. </summary>
    public static readonly IReadOnlyList<T> ValuesDescending = Values.Reverse().ToArray();


    private static readonly List<string> __namesAscending = Enum.GetNames( typeof( T ) ).OrderBy( s => s ).ToList();
    /// <summary> An ascending list of the enum value names ordered alphabetically. </summary>
    public static IReadOnlyList<string> NamesAscending => __namesAscending;

    /// <summary> An descending list of the enum value names ordered alphabetically. </summary>
    public static readonly IReadOnlyList<string> NamesDescending = Enum.GetNames( typeof( T ) ).OrderByDescending( s => s ).ToArray();


    /// <summary> The smallest defined value in the enum </summary>
    public static readonly T MinDefinedValue = Values[0];
    /// <summary> The largest defined value in the enum </summary>
    public static readonly T MaxDefinedValue = Values[Values.Count - 1];


    /// <summary> Returns an enum value restricted to the given value range </summary>
    public static T Clamp( T t, T min, T max )
      => t.CompareTo( min ) < 0 ? min
       : t.CompareTo( max ) > 0 ? max
       : t;
    /// <summary> Returns an enum value restricted to the range of explicitly defined/named values. </summary>
    public static T ClampToDefinedRange( T t ) => Clamp( t, MinDefinedValue, MaxDefinedValue );

    private static readonly Random __random = new();
    /// <summary> Returns a random value that is explicitly defined in the enum. Won't return unlisted combined flags. </summary>
    public static T Random() => Values[__random.Next( 0, Values.Count )];


    public static bool IsDefined( T t ) => HasFlagsAttribute ? MaxFlagValue.HasFlag( t ) : __values.BinarySearch( t ) >= 0;
    public static bool IsDefined( string s ) => __namesAscending.BinarySearch( s ) >= 0;
    public static bool IsDefined( string s, bool ignoreCase ) => !ignoreCase ? IsDefined( s ) : __namesAscending.BinarySearch( s, StringComparer.OrdinalIgnoreCase ) >= 0;


    /// <summary> Try to parse the given string as an enum type T </summary>
    /// <remarks>
    ///   This method differs from the standard <see cref="System.Enum.TryParse"/>.
    ///   In the cases where s is a value representation, this method will check to see
    ///   if the value is defined in the enum or if it is a valid flag for the enum.
    /// </remarks>
    public static bool TryParse( string s, out T result )
    {
      if (!Enum.TryParse( s, out result )) { return false; }
      if (HasFlagsAttribute) { return MaxFlagValue.HasFlag( result ); }
      return IsDefined( result );
    }

    /// <summary> Try to parse the given string as an enum type T </summary>
    /// <remarks>
    ///   This method differs from the standard <see cref="System.Enum.TryParse"/>.
    ///   If `onlyDefinedValues` is true, then
    ///   in the cases where s is a value representation, this method will check to see
    ///   if the value is defined in the enum or if it is a valid flag for the enum.
    /// </remarks>
    public static bool TryParse( string s, bool onlyDefinedValues, out T result )
    {
      if (!Enum.TryParse( s, out result )) { return false; }
      if (!onlyDefinedValues) { return true; }
      if (HasFlagsAttribute) { return MaxFlagValue.HasFlag( result ); }
      return IsDefined( result );
    }

    /// <summary> Parse the given string as an enum type T, throwing exceptions when that fails </summary>
    /// <remarks>
    ///   This method differs from the standard <see cref="System.Enum.TryParse"/>.
    ///   In the cases where s is a value representation, this method will check to see
    ///   if the value is defined in the enum or if it is a valid flag for the enum.
    /// </remarks>
    public static T Parse( string s )
    {
      if (!Enum.TryParse( s, out T result ))
      {
        throw new FormatException( $"Parameter 's' is not a valid representation of enum '{typeof( T ).Name}' as string '{s}'" );
      }
      if (HasFlagsAttribute)
      {
        return MaxFlagValue.HasFlag( result ) ? result : throw new FormatException( $"Parameter 's' is not defined by enum '{typeof( T ).Name}' as string '{s}'" );
      }
      return IsDefined( result ) ? result : throw new FormatException( $"Parameter 's' is not defined by enum '{typeof( T ).Name}' as string '{s}'" );
    }

    /// <summary> Parse the given string as an enum type T, throwing exceptions when that fails </summary>
    /// <remarks>
    ///   This method differs from the standard <see cref="System.Enum.TryParse"/>.
    ///   If `onlyDefinedValues` is true, then
    ///   in the cases where s is a value representation, this method will check to see
    ///   if the value is defined in the enum or if it is a valid flag for the enum.
    /// </remarks>
    public static T Parse( string s, bool onlyDefinedValues = true )
    {
      if (!Enum.TryParse( s, out T result ))
      {
        throw new FormatException( $"Parameter 's' is not a valid representation of enum '{typeof( T ).Name}' as string '{s}'" );
      }
      if (!onlyDefinedValues) { return result; }
      if (HasFlagsAttribute)
      {
        return MaxFlagValue.HasFlag( result ) ? result : throw new FormatException( $"Parameter 's' is not defined by enum '{typeof( T ).Name}' as string '{s}'" );
      }
      return IsDefined( result ) ? result : throw new FormatException( $"Parameter 's' is not defined by enum '{typeof( T ).Name}' as string '{s}'" );
    }


    /// <summary> A combination of all defined flag values in the enum </summary>
    /// <remarks> Does not contain undefined flags </remarks>
    public static T MaxFlagValue => FlagValueCache.MaxFlagValue;

    // Lazily instantiated, in case an enum never needs these.
    private static class FlagValueCache
    {
      public static readonly T MaxFlagValue;
      static FlagValueCache()
      {
        MaxFlagValue = Values.Aggregate( ( a, b ) => Bitwise.Or( a, b ) );
      }
    }


    /// <summary> Returns the display name for the enum value, if a System.ComponentModel.DisplayNameAttribute is defined. Null otherwise. </summary>
    public static string? GetDisplayName( T t ) => DisplayAttributeCache.DisplayNameLookup.TryGetValue( t, out var result ) ? result : null;
    /// <summary> Returns the description for the enum value, if a System.ComponentModel.DescriptionAttribute is defined. Null otherwise. </summary>
    public static string? GetDescription( T t ) => DisplayAttributeCache.DescriptionLookup.TryGetValue( t, out var result ) ? result : null;

    // Lazily instantiated, in case an enum never needs these.
    private static class DisplayAttributeCache
    {
      public static readonly IReadOnlyDictionary<T, string?> DisplayNameLookup;
      public static readonly IReadOnlyDictionary<T, string?> DescriptionLookup;

      static DisplayAttributeCache()
      {
        if (Values.Count <= 0)
        {
          DisplayNameLookup = DescriptionLookup = new Dictionary<T, string?>();
          return;
        }

        var fields = typeof( T ).GetFields();
        DisplayNameLookup = fields
          .ToDictionary(
            f => (T) f.GetRawConstantValue(),
            f => f.GetCustomAttributes<DisplayNameAttribute>( false )
              .FirstOrDefault()?.DisplayName
          );
        DescriptionLookup = fields
          .ToDictionary(
            f => (T) f.GetRawConstantValue(),
            f => f.GetCustomAttributes<DescriptionAttribute>( false )
              .FirstOrDefault()?.Description
          );
      }
    }


    public static class Bitwise
    {
      public static readonly Func<T, T, T> Or = __GetOrFunc();
      private static Func<T, T, T> __GetOrFunc()
      {
        var enumType = typeof( T );
        var underlyingType = Enum.GetUnderlyingType( enumType );
        var a = Expression.Parameter( enumType );
        var b = Expression.Parameter( enumType );
        return Expression.Lambda<Func<T, T, T>>(
          Expression.Convert(
            Expression.Or(
              Expression.Convert( a, underlyingType ),
              Expression.Convert( b, underlyingType )
            ),
            enumType
          ),
          a, b
        ).Compile();
      }

      public static readonly Func<T, T, T> AndNot = __GetAndNotFunc();
      private static Func<T, T, T> __GetAndNotFunc()
      {
        var enumType = typeof( T );
        var underlyingType = Enum.GetUnderlyingType( enumType );
        var a = Expression.Parameter( enumType );
        var b = Expression.Parameter( enumType );
        return Expression.Lambda<Func<T, T, T>>(
          Expression.Convert(
            Expression.And(
              Expression.Convert( a, underlyingType ),
              Expression.Not(
                Expression.Convert( b, underlyingType )
              )
            ),
            enumType
          ),
          a, b
        ).Compile();
      }

      public static readonly Func<T, T, T> Xor = __GetXorFunc();
      private static Func<T, T, T> __GetXorFunc()
      {
        var enumType = typeof( T );
        var underlyingType = Enum.GetUnderlyingType( enumType );
        var a = Expression.Parameter( enumType );
        var b = Expression.Parameter( enumType );
        return Expression.Lambda<Func<T, T, T>>(
          Expression.Convert(
            Expression.ExclusiveOr(
              Expression.Convert( a, underlyingType ),
              Expression.Convert( b, underlyingType )
            ),
            enumType
          ),
          a, b
        ).Compile();
      }

      /// <summary> Returns true if only one bit is set. </summary>
      /// <remarks> Result ignores whether enum values are explicitly defined. </remarks>
      public static readonly Func<T, bool> IsPowerOfTwo = __GetIsPowerOfTwoFunc();
      private static Func<T, bool> __GetIsPowerOfTwoFunc()
      {
        var x = Expression.Parameter( Type );
        return Expression.Lambda<Func<T, bool>>(
          Expression.Call(
            typeof( Enum<T> ),
            nameof( __IsPowerOfTwo ),
            new Type[]{ UnderlyingType },
            Expression.Convert( x, UnderlyingType )
          ),
          x
        ).Compile();
      }

      /// <summary> Flips all bits smaller than the highest set bit and returns the result. Example:  001001 => 001111 </summary>
      /// <remarks> This method will backfill all bits, whether explicitly defined in the enum or not. </remarks>
      public static readonly Func<T, T> SetAllBitsLessThanMsb = __GetSetAllBitsLessThanMsbFunc();
      private static Func<T, T> __GetSetAllBitsLessThanMsbFunc()
      {
        var a = Expression.Parameter( Type );
        return Expression.Lambda<Func<T, T>>(
          Expression.Convert(
            Expression.Call(
              typeof( Enum<T> ),
              nameof( __SetAllBitsLessThanMsb ),
              new Type[]{ UnderlyingType },
              Expression.Convert( a, UnderlyingType )
            ),
            Type
          ),
          a
        ).Compile();
      }


      /// <summary> Performs a bit-shift right operation. </summary>
      /// <remarks> Does not guarentee defined values. </remarks>
      public static readonly Func<T, T> BitShiftRightOne = __GetBitShiftRightOneFunc();
      private static Func<T, T> __GetBitShiftRightOneFunc()
      {
        var a = Expression.Parameter( Type );
        return Expression.Lambda<Func<T, T>>(
          Expression.Convert(
            Expression.Call(
              typeof( Enum<T> ),
              nameof( __ShiftRight ),
              new Type[]{ UnderlyingType },
              Expression.Convert( a, UnderlyingType )
            ),
            Type
          ),
          a
        ).Compile();
      }

      /// <summary> Performs a bit-shift right operation. </summary>
      /// <remarks> Does not guarentee defined values. </remarks>
      public static readonly Func<T, int, T> BitShiftRight = __GetBitShiftRightFunc();
      private static Func<T, int, T> __GetBitShiftRightFunc()
      {
        var a = Expression.Parameter( Type );
        var b = Expression.Parameter( typeof(int) );
        return Expression.Lambda<Func<T, int, T>>(
          Expression.Convert(
            Expression.Call(
              typeof( Enum<T> ),
              nameof( __ShiftRight ),
              new Type[]{ UnderlyingType, typeof( int ) },
              Expression.Convert( a, UnderlyingType ),
              b
            ),
            Type
          ),
          a, b
        ).Compile();
      }

      /// <summary> Performs a bit-shift left operation. </summary>
      /// <remarks> Does not guarentee defined values. </remarks>
      public static readonly Func<T, T> BitShiftLeftOne = __GetBitShiftLeftOneFunc();
      private static Func<T, T> __GetBitShiftLeftOneFunc()
      {
        var a = Expression.Parameter( Type );
        return Expression.Lambda<Func<T, T>>(
          Expression.Convert(
            Expression.Call(
              typeof( Enum<T> ),
              nameof( __ShiftLeft ),
              new Type[]{ UnderlyingType },
              Expression.Convert( a, UnderlyingType )
            ),
            Type
          ),
          a
        ).Compile();
      }

      /// <summary> Performs a bit-shift left operation. </summary>
      /// <remarks> Does not guarentee defined values. </remarks>
      public static readonly Func<T, int, T> BitShiftLeft = __GetBitShiftLeftFunc();
      private static Func<T, int, T> __GetBitShiftLeftFunc()
      {
        var a = Expression.Parameter( Type );
        var b = Expression.Parameter( typeof( int ) );
        return Expression.Lambda<Func<T, int, T>>(
          Expression.Convert(
            Expression.Call(
              typeof( Enum<T> ),
              nameof( __ShiftLeft ),
              new Type[]{ UnderlyingType, typeof( int ) },
              Expression.Convert( a, UnderlyingType ),
              b
            ),
            Type
          ),
          a, b
        ).Compile();
      }

      public static readonly Func<T, T> FindLsb = __GetFindLsbFunc();
      private static Func<T, T> __GetFindLsbFunc()
      {
        var enumType = typeof( T );
        var underlyingType = Enum.GetUnderlyingType( enumType );
        var a = Expression.Parameter( enumType );
        return Expression.Lambda<Func<T, T>>(
          Expression.Convert(
            Expression.Call(
              typeof( Enum<T> ),
              nameof( __FindLsb ),
              new Type[]{ underlyingType },
              Expression.Convert( a, underlyingType )
            ),
            enumType
          ),
          a
        ).Compile();
      }

      public static readonly Func<T, T> FindMsb = __GetFindMsbFunc();
      private static Func<T, T> __GetFindMsbFunc()
      {
        var enumType = typeof( T );
        var underlyingType = Enum.GetUnderlyingType( enumType );
        var a = Expression.Parameter( enumType );
        return Expression.Lambda<Func<T, T>>(
          Expression.Convert(
            Expression.Call(
              typeof( Enum<T> ),
              nameof( __FindMsb ),
              new Type[]{ underlyingType },
              Expression.Convert( a, underlyingType )
            ),
            enumType
          ),
          a
        ).Compile();
      }


      //// Helpers defined for all numerical types ////

      private static bool __IsPowerOfTwo( ulong x ) => (x > 0) && ((x & (x - 1)) == 0);
      private static bool __IsPowerOfTwo( long x ) => (x > 0) && ((x & (x - 1)) == 0);
      private static bool __IsPowerOfTwo( uint x ) => (x > 0) && ((x & (x - 1)) == 0);
      private static bool __IsPowerOfTwo( int x ) => (x > 0) && ((x & (x - 1)) == 0);
      private static bool __IsPowerOfTwo( ushort x ) => (x > 0) && ((x & (x - 1)) == 0);
      private static bool __IsPowerOfTwo( short x ) => (x > 0) && ((x & (x - 1)) == 0);
      private static bool __IsPowerOfTwo( byte x ) => (x > 0) && ((x & (x - 1)) == 0);
      private static bool __IsPowerOfTwo( sbyte x ) => (x > 0) && ((x & (x - 1)) == 0);

      private static ulong __ShiftLeft( ulong b ) => b << 1;
      private static long __ShiftLeft( long b ) => b << 1;
      private static uint __ShiftLeft( uint b ) => b << 1;
      private static int __ShiftLeft( int b ) => b << 1;
      private static ushort __ShiftLeft( ushort b ) => (ushort)(((uint)b) << 1);
      private static short __ShiftLeft( short b ) => (short)(b << 1);
      private static byte __ShiftLeft( byte b ) => (byte)((uint)b << 1 );
      private static sbyte __ShiftLeft( sbyte b ) => (sbyte)(b << 1);
      private static ulong __ShiftLeft( ulong b, int s ) => b << s;
      private static long __ShiftLeft( long b, int s ) => b << s;
      private static uint __ShiftLeft( uint b, int s ) => b << s;
      private static int __ShiftLeft( int b, int s ) => b << s;
      private static ushort __ShiftLeft( ushort b, int s ) => (ushort)(((uint)b) << s);
      private static short __ShiftLeft( short b, int s ) => (short)(b << s);
      private static byte __ShiftLeft( byte b, int s ) => (byte)((uint)b << s);
      private static sbyte __ShiftLeft( sbyte b, int s ) => (sbyte)(b << s);

      private static ulong __ShiftRight( ulong b ) => b >> 1;
      private static long __ShiftRight( long b ) => b >> 1;
      private static uint __ShiftRight( uint b ) => b >> 1;
      private static int __ShiftRight( int b ) => b >> 1;
      private static ushort __ShiftRight( ushort b ) => (ushort)(((uint)b) >> 1);
      private static short __ShiftRight( short b ) => (short)(b >> 1);
      private static byte __ShiftRight( byte b ) => (byte)((uint)b >> 1 );
      private static sbyte __ShiftRight( sbyte b ) => (sbyte)(b >> 1);
      private static ulong __ShiftRight( ulong b, int s ) => b >> s;
      private static long __ShiftRight( long b, int s ) => b >> s;
      private static uint __ShiftRight( uint b, int s ) => b >> s;
      private static int __ShiftRight( int b, int s ) => b >> s;
      private static ushort __ShiftRight( ushort b, int s ) => (ushort)(((uint)b) >> s);
      private static short __ShiftRight( short b, int s ) => (short)(b >> s);
      private static byte __ShiftRight( byte b, int s ) => (byte)((uint)b >> s);
      private static sbyte __ShiftRight( sbyte b, int s ) => (sbyte)(b >> s);


      private static ulong __SetAllBitsLessThanMsb( ulong b )
      {
        b |= b >> 1;
        b |= b >> 2;
        b |= b >> 4;
        b |= b >> 8;
        b |= b >> 16;
        b |= b >> 32;
        return b;
      }
      private static long __SetAllBitsLessThanMsb( long b )
      {
        b |= b >> 1;
        b |= b >> 2;
        b |= b >> 4;
        b |= b >> 8;
        b |= b >> 16;
        b |= b >> 32;
        return b;
      }
      private static uint __SetAllBitsLessThanMsb( uint b )
      {
        b |= b >> 1;
        b |= b >> 2;
        b |= b >> 4;
        b |= b >> 8;
        b |= b >> 16;
        return b;
      }
      private static int __SetAllBitsLessThanMsb( int b )
      {
        b |= b >> 1;
        b |= b >> 2;
        b |= b >> 4;
        b |= b >> 8;
        b |= b >> 16;
        return b;
      }
      // These types don't have bit-shift support. Just upcasting for now.
      private static ushort __SetAllBitsLessThanMsb( ushort b ) => (ushort)__SetAllBitsLessThanMsb( (uint) b );
      private static short __SetAllBitsLessThanMsb( short b ) => (short)__SetAllBitsLessThanMsb( (int) b );
      private static byte __SetAllBitsLessThanMsb( byte b ) => (byte)__SetAllBitsLessThanMsb( (uint) b );
      private static sbyte __SetAllBitsLessThanMsb( sbyte b ) => (sbyte)__SetAllBitsLessThanMsb( (int) b );


      private const ulong __DeBruijnSequence = 0x37E84A99DAE458F;
      private static readonly byte[] __DeBruijnBitPosition =
      {
        0, 1, 17, 2, 18, 50, 3, 57,
        47, 19, 22, 51, 29, 4, 33, 58,
        15, 48, 20, 27, 25, 23, 52, 41,
        54, 30, 38, 5, 43, 34, 59, 8,
        63, 16, 49, 56, 46, 21, 28, 32,
        14, 26, 24, 40, 53, 37, 42, 7,
        62, 55, 45, 31, 13, 39, 36, 6,
        61, 44, 12, 35, 60, 11, 10, 9,
      };

      private static ulong __FindLsb( ulong b )
      {
        if (b == 0) return 0;
        return 1UL << __DeBruijnBitPosition[((ulong) ((long) b & -(long) b) * __DeBruijnSequence) >> 58];
      }
      private static ulong __FindMsb( ulong b )
      {
        if (b == 0) return 0;
        b |= b >> 1;
        b |= b >> 2;
        b |= b >> 4;
        b |= b >> 8;
        b |= b >> 16;
        b |= b >> 32;
        b &= ~(b >> 1);
        return 1UL << __DeBruijnBitPosition[b * __DeBruijnSequence >> 58];
      }
      private static long __FindLsb( long b ) => MathF.Sign(b) * (long)__FindLsb( (ulong) b );
      private static long __FindMsb( long b ) => b >= 0 ? (long)__FindMsb( (ulong) b ) : -(long)__FindMsb( (ulong)-b );


      private static uint __FindLsb( uint b )
      {
        if (b == 0) return 0;
        return 1U << __DeBruijnBitPosition[((uint) ((int) b & -(int) b) * __DeBruijnSequence) >> 58];
      }
      private static uint __FindMsb( uint b )
      {
        if (b == 0) return 0;
        b |= b >> 1;
        b |= b >> 2;
        b |= b >> 4;
        b |= b >> 8;
        b |= b >> 16;
        b &= ~(b >> 1);
        return 1U << __DeBruijnBitPosition[b * __DeBruijnSequence >> 58];
      }
      private static int __FindLsb( int b ) => MathF.Sign(b) * (int)__FindLsb( (uint) b );
      private static int __FindMsb( int b ) => b >= 0 ? (int)__FindMsb( (uint) b ) : -(int)__FindMsb( (uint)-b );

      // All smaller types will defer to int implementation
      private static ushort __FindLsb( ushort b ) => (ushort)__FindLsb( (uint) b );
      private static ushort __FindMsb( ushort b ) => (ushort)__FindMsb( (uint) b );
      private static short __FindLsb( short b ) => (short)__FindLsb( (int) b );
      private static short __FindMsb( short b ) => (short)__FindMsb( (int) b );

      private static byte __FindLsb( byte b ) => (byte)__FindLsb( (uint) b );
      private static byte __FindMsb( byte b ) => (byte)__FindMsb( (uint) b );
      private static sbyte __FindLsb( sbyte b ) => (sbyte)__FindLsb( (int) b );
      private static sbyte __FindMsb( sbyte b ) => (sbyte)__FindMsb( (int) b );
    }
  };
}
