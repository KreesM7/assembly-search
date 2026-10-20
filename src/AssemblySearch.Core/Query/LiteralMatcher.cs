using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
namespace AssemblySearch.Core.Query;
internal static class LiteralMatcher {
 public static CompiledQuery Compile(string text,StringComparison comparison,bool exact,bool quoted){
  if(quoted)return Strings(text[1..^1],comparison,exact);
  var unsignedText=text.StartsWith('-')?text[1..]:text;
  bool negative=text.StartsWith('-');
  bool hex=unsignedText.StartsWith("0x",StringComparison.OrdinalIgnoreCase);
  bool integral=hex||Regex.IsMatch(text,@"^[+-]?\d+$",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100));
  if(integral){
   BigInteger number;
   bool valid=hex?BigInteger.TryParse("0"+unsignedText[2..],NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out number):BigInteger.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out number);
   if(hex&&negative)number=-number;
   if(!valid||number<long.MinValue||number>ulong.MaxValue||hex&&unsignedText.Length<=2)throw new QueryValidationException("Integer is malformed or outside the 64-bit range.");
   return new((_,value,lit)=>lit&&IntegralEquals(number,value));
  }
  if(double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out var floating)){
   return new((_,value,lit)=>lit&&FloatingEquals(floating,value));
  }
  if(Regex.IsMatch(text,@"^[+-]?\d",RegexOptions.CultureInvariant,TimeSpan.FromMilliseconds(100)))throw new QueryValidationException("Invalid numeric literal. Quote it to search a string.");
  return Strings(text,comparison,exact);
 }
 static CompiledQuery Strings(string text,StringComparison comparison,bool exact)=>new((_,value,lit)=>lit&&value is string s&&TextMatcher.Match(s,text,comparison,exact));
 static bool IntegralEquals(BigInteger wanted,object? value)=>value switch {
  bool v=>wanted==(v?1:0),char v=>wanted==v,
  sbyte v=>wanted==v,byte v=>wanted==v,short v=>wanted==v,ushort v=>wanted==v,int v=>wanted==v,uint v=>wanted==v,long v=>wanted==v,ulong v=>wanted==v,
  float v=>float.IsFinite(v)&&MathF.Truncate(v)==v&&wanted==new BigInteger(v),
  double v=>double.IsFinite(v)&&Math.Truncate(v)==v&&wanted==new BigInteger(v),_=>false};
 static bool FloatingEquals(double wanted,object? value){
  if(value is string or decimal or DateTime||value is null)return false;
  double actual;
  try{actual=value is char c?c:Convert.ToDouble(value,CultureInfo.InvariantCulture);}catch(InvalidCastException){return false;}
  return double.IsNaN(wanted)?double.IsNaN(actual):wanted==actual;
 }
}
