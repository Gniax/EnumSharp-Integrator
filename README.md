# EnumSharpIntegrator

C# WPF Tool to integrate any file which contains an enumeration in C# format into another one.

## Getting Started

To run the tool, just compile the project with your .Net Framework 4.8

The main content is on the MainWindow.xaml.cs ...

### Use case

It's a little tool to synchronize lists of enumerations.

Needing to merge large lists that need to be updated, it's easier than doing it by hand.

## Limitation

The only working format is basic enum **string : int**

## Example

*Target file: (Fruits1.cs)* 
```cs
enum Fruits
{
    Apple = 1,
    Banana = 2,
    Cherry = 3,
    Apricot = 6
}
```
*File to integrate: (Fruits2.cs)* 
```cs
enum Fruits
{
    Watermelon = 4,
    Apple = 24,
    Peach = 3
}
```
**The Fruits1.cs file will now be**
```cs
enum Fruits
{
    Apple = 1,
    Banana = 2,
    Peach = 3,
    Watermelon = 4,
    Apricot = 6,
    Apple = 24
}
```

### Small detail

If in both files, a line of the enumeration has the same key, it is the value of the file to be integrated that will be used
