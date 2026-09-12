using System;
namespace System.Runtime.CompilerServices {
internal static class IsExternalInit {}
[AttributeUsage(AttributeTargets.Parameter)] internal sealed class CallerArgumentExpressionAttribute:Attribute { public string ParameterName {get;} public CallerArgumentExpressionAttribute(string name) {ParameterName=name;} }
}
namespace System.Diagnostics.CodeAnalysis {
[AttributeUsage(AttributeTargets.Method|AttributeTargets.Property,AllowMultiple=true)] internal sealed class MemberNotNullAttribute:Attribute { public string[] Members {get;} public MemberNotNullAttribute(string member){Members=new[]{member};} public MemberNotNullAttribute(params string[] members){Members=members;} }
[AttributeUsage(AttributeTargets.Method|AttributeTargets.Property,AllowMultiple=true)] internal sealed class MemberNotNullWhenAttribute:Attribute { public bool ReturnValue{get;} public string[] Members{get;} public MemberNotNullWhenAttribute(bool value,params string[] members){ReturnValue=value;Members=members;} }
[AttributeUsage(AttributeTargets.Method|AttributeTargets.Property|AttributeTargets.Parameter)] internal sealed class UnscopedRefAttribute:Attribute {}
}
namespace System.Runtime.Versioning { [AttributeUsage(AttributeTargets.All)] internal sealed class RequiresPreviewFeaturesAttribute:Attribute { public string? Message{get;} public string? Url{get;set;} public RequiresPreviewFeaturesAttribute(string? message=null){Message=message;} } }
