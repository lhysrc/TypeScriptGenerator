using Microsoft.Extensions.Configuration;
using Runner.ForImport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TypeScriptGenerator;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();
            var configuration = builder.Build();
            var root = configuration["root"];

            var asmNames = new string[0]
            {
                //"QiaoDan.Core.dll",
                //"QiaoDan.ViewModels.Abstractions.dll",
                //"QiaoDan.Admin.ViewModels.dll",
                //"QiaoDan.OA.Core.dll",
                //"QiaoDan.OA.ViewModels.dll",
                //"QiaoDan.HR.Core.dll",
                //"QiaoDan.HR.ViewModels.dll",
            }
            ;

            ModelsGenerator.Generate(
                asmNames.Select(n => Assembly.LoadFrom(Path.Combine(root, n))).Append(typeof(Program).Assembly),
                "../ts.g",
                opt => 
                { 
                    opt.TypeMatcher = t => t.GetInterface("IViewModel") != null || t.IsEnum || (t.IsAbstract && t.IsSealed);
                    opt.CodeSnippets = t => t.IsClass ? @$"
  static _assemblyName = ""{t.Assembly.GetName().Name}"";
  static _className = ""{t.FullName}"";
" : null;
                }
            );
        }
    }

    public class Item : BaseItem, IViewModel
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public bool IsWhatever { get; set; }
        public IEnumerable<string> Collection { get; set; }
        public double[] Array { get; set; }
        public (int, string) ValueTuple { get; set; }
        public Tuple<int,string> Tuple { get; set; }
        public int? Nullable { get; set; }
        public GenericItem<string> Generic { get; set; }
        public Dictionary<string, BaseItem> Dictionary { get; set; }
        public Dictionary<Guid, BaseItem> Dictionary2 { get; set; }
        public DateTime Date { get; set; }
        public string Hello => "Hello World!";
        public byte[] File { get; set; }

        public string IgnoreMe { get; set; }
        public string RenameMe { get; set; }
    }

    public class GenericItem<TStuff> : IImportMe2<Item>
    {
        public TStuff Stuff { get; set; }
        public GenericItem<TStuff> Circle { get; set; }
        public Item X { get; set; }
    }

    public class BaseItem : IImportMe2<Item>
    {
        public ImportMe1 Imported { get; set; }
        public Item X { get; set; }
    }

    public static class StaticWithNest
    {
        public const string ConstString = "ConstString";
        public const int ConstInt = 1314520;
        public static void Help() { }

        public static class Nest
        {
            public const string ConstString = "ConstString";
            public const double ConstDouble = 1314.520;

            public static class NestInNest
            {
                public const string ConstString = "ConstString";
                public const double ConstDouble = 1314.520;
            }
        }
    }
    public static class Static
    {
        public const string ConstString = "ConstString";
        public const int ConstInt = 1314520;
    }

    interface IViewModel: IImportMe3<IImportMe2<BaseItem>,int,string>
    {

    }
}
namespace Runner.ForImport
{
    public class ImportMe1
    {
        public int Id { get; set; }
    }
    public interface IImportMe2<T>
    {
        public T X { get; set; }
    }
    public interface IImportMe3<T,T1,T2>
    {
    }
}