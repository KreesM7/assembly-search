using System.Runtime.InteropServices;using System.Runtime.CompilerServices;
namespace Sample;
[Obsolete("attribute needle")]
public class Widget : OtherLibrary.ReferencedType {
 static Widget(){var marker=Environment.GetEnvironmentVariable("ASSEMBLY_SEARCH_FIXTURE_MARKER");if(marker!=null)File.WriteAllText(marker,"EXECUTED");}
 public const string Greeting="hello searchable world";public const ulong Huge=ulong.MaxValue;public int Counter;public string Title{get;set;}="";public event Action? Changed;
 public int Read(int input=42){int localNeedle=input+7;Changed?.Invoke();return localNeedle;}
 public string Read(string input)=>input+"body literal needle";
 public long Numbers()=>-9223372036854775808L;
 public double Fraction()=>1.5;
 [CompilerGenerated] public void GeneratedNeedle(){}
 [DllImport("fixture-native")]public static extern int NativeNeedle();
 public class Nested {public void NestedNeedle(){}}
}
public class Box<T>{public T? Value;}
public enum Choice {First,Second}
public interface IReadable {void Read();}
public struct Point {public int X;}
public delegate void ChangedHandler();
