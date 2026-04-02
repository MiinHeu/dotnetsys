using System.Reflection;
using System.Runtime.Loader;
string[] roots={Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),@"C:\Users\Admin",@"C:\Users\CodexSandboxOffline"};
string? Find(string n){foreach(var r in roots.Distinct()){var p=Path.Combine(r,".nuget","packages"); if(Directory.Exists(p)){var h=Directory.EnumerateFiles(p,n+".dll",SearchOption.AllDirectories).FirstOrDefault(); if(h!=null)return h;}} return Directory.EnumerateFiles(@"C:\Program Files\dotnet",n+".dll",SearchOption.AllDirectories).FirstOrDefault();}
AssemblyLoadContext.Default.Resolving += (c,n)=>{var p=Find(n.Name!); if(p!=null){try{return c.LoadFromAssemblyPath(p);}catch{}} return null;};
var asm=AssemblyLoadContext.Default.LoadFromAssemblyPath(Find("ZXing.Net.MAUI.Controls")!);
var t=asm.GetType("ZXing.Net.Maui.Controls.CameraBarcodeReaderView")!;
Console.WriteLine("Props:"); foreach(var p in t.GetProperties(BindingFlags.Public|BindingFlags.Instance).OrderBy(x=>x.Name)) Console.WriteLine($"- {p.Name}:{p.PropertyType.Name}");
Console.WriteLine("Events:"); foreach(var e in t.GetEvents(BindingFlags.Public|BindingFlags.Instance).OrderBy(x=>x.Name)) Console.WriteLine($"- {e.Name}:{e.EventHandlerType?.Name}");
Console.WriteLine("Methods:"); foreach(var m in t.GetMethods(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).OrderBy(x=>x.Name)) Console.WriteLine($"- {m.Name}({string.Join(", ",m.GetParameters().Select(p=>$"{p.ParameterType.Name} {p.Name}"))})");
