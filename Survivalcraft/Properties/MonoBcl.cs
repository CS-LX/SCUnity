using System;using System.Collections.Generic;using System.Linq;using System.Security.Cryptography;using System.Runtime.CompilerServices;using System.Runtime.InteropServices;
namespace Game {internal static class MonoBcl {
public static T FirstOrDefault<T>(this IEnumerable<T> source,Func<T,bool> predicate,T defaultValue){if(source is null)throw new ArgumentNullException("source");if(predicate is null)throw new ArgumentNullException("predicate");foreach(T item in source)if(predicate(item))return item;return defaultValue;}
public static string[] SplitTrimmed(this string value,char separator)=>value.Split(separator).Select(v=>v.Trim()).Where(v=>v.Length!=0).ToArray();
public static string ReplaceLineEndings(this string value,string replacement)=>System.Text.RegularExpressions.Regex.Replace(value,"\r\n|[\r\n\u0085\u000c\u2028\u2029]",m=>replacement);
public static byte[] HashMD5(byte[] data){using(var a=MD5.Create())return a.ComputeHash(data);}public static byte[] HashSHA1(byte[] data){using(var a=SHA1.Create())return a.ComputeHash(data);}public static byte[] HashSHA256(byte[] data){using(var a=SHA256.Create())return a.ComputeHash(data);}public static byte[] HashSHA384(byte[] data){using(var a=SHA384.Create())return a.ComputeHash(data);}
[DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool MoveFileEx(string from,string to,int flags);
public static void MoveOverwrite(string from,string to){if(!MoveFileEx(from,to,3))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
}}
namespace System.Collections.Generic {internal sealed class ReferenceEqualityComparer:IEqualityComparer<object>{public static readonly ReferenceEqualityComparer Instance=new ReferenceEqualityComparer();public new bool Equals(object x,object y)=>ReferenceEquals(x,y);public int GetHashCode(object value)=>RuntimeHelpers.GetHashCode(value);}}
