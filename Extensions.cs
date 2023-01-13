// Decompiled with JetBrains decompiler
// Type: Traffic.Extensions
// Assembly: Traffic, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FF356DD2-B5EC-42F5-80AC-04EF70E56DD0
// Assembly location: D:\Workstation\TonniProjects\TrafficWebDeploy\Traffic.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Traffic
{
  public static class Extensions
  {
    public static TAttribute GetAttribute<TAttribute>(this Enum enumValue) where TAttribute : Attribute => CustomAttributeExtensions.GetCustomAttribute<TAttribute>(((IEnumerable<MemberInfo>) enumValue.GetType().GetMember(enumValue.ToString())).First<MemberInfo>());
  }
}
