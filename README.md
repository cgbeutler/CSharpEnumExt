# CSharpEnumExt
Enum Extensions for CSharp

All the extensions are contained within one file for easy copy-pasting into your project.

## Important Usage Notes

If using with flags, do not define a value larger than all the flags combined. This would mess up some of the IsDefined stuff.

## Example Extension Methods

Below is an example of what is provided. See code docs for more details on usage and outputs. In each example below `e` would be your enum value.

```csharp
var eClamped = e.Clamp( minEnumVal, maxEnumVal );
var eClamped2 = e.ClampToDefinedRange();
// To check if a value is in flag range or is explicitly defined, use:
bool eIsDefined = e.IsDefined();
// Flag ease-of-use operations
var eWithFlags = e.WithFlags( flag1 | flag2 );
var eWithoutFlags = e.WithoutFlags( flag1 | flag2 );
var eWithToggled = e.WithFlagsToggled( flag1 | flag2 );
// DisplayNameAttribute helpers
string eDisplayName = e.DisplayName();
string eDescription = e.Description();
```


## Example Helper Methods

There are also a bunch of cached values and sequences for enums. Use `Enum<T>` to access these values and methods. Below are examples of method in the class. See code docs for more details on usage and outputs. In each example `E` would be your custom enum type.

```csharp
var zeroValue = Enum<E>.Zero;

// Scan over every value in ascending order. No duplicates.
foreach (var e in Enum<E>.Values) { ... }
// Scan over all values in descending order. No duplicates.
foreach (var e in Enum<E>.ValuesDescending) { ... }

// Similar caches exist for names.
// Scan over names in order defined using `OrderByAscending`
foreach (var s in Enum<E>.Names) { ... }
foreach (var s in Enum<E>.NamesDescending) { ... }

// Cached values
var eMin = Enum<E>.MinDefinedValue;
var eMax = Enum<E>.MaxDefinedValue;
// For a all values bitwise-anded together, use:
var _ = Enum<E>.MaxFlagValue;

// Get a random defined enum value
var eRandom = Enum<E>.Random();

// Try to parse the given string as an enum type T.
// Unlike the standard `Enum.TryParse`, this will verify that the value is defined as well.
if (Enum<E>.TryParse( s, out T result )) { ... }
// You can get the standard behaviour by passing in false
if (Enum<E>.TryParse( s, false, out T result )) { ... }

// There is an exception-throwing version as well
var result = Enum<E>.Parse( s );
var result = Enum<E>.Parse( s, false )

// Also available are severl other bitwise operations that may help if you need more power. Feel free to define extensions of your own to shorten calls to these.
var backfilled = Enum<E>.Bitwise.SetAllBitsLessThanMsb( e );
bool isPow2Flag = Enum<E>.Bitwise.IsPowerOfTwo( e );
var biggestFlag = Enum<E>.Bitwise.FindMsb( e );
```
