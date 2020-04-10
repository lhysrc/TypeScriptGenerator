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

            var asmNames = new[]
            {
                "QiaoDan.Core.dll",
                "QiaoDan.ViewModels.Abstractions.dll",
                "QiaoDan.Admin.ViewModels.dll",
                //"QiaoDan.OA.ViewModels.dll",
                //"QiaoDan.HR.ViewModels.dll",
            };

            ModelsGenerator.create(
                asmNames.Select(n => Assembly.LoadFrom(Path.Combine(root, n))).Append(typeof(Program).Assembly),
                "../ts.g",
                opt => 
                { 
                    opt.TypeMatcher = t => t.GetInterface("IViewModel") != null || t.IsEnum || (t.IsAbstract && t.IsSealed);
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
        public DateTime Date { get; set; }
        public string Hello => "Hello World!";
        public byte[] File { get; set; }

        public string IgnoreMe { get; set; }
        public string RenameMe { get; set; }
    }

    public class GenericItem<TStuff>
    {
        public TStuff Stuff { get; set; }
    }

    public class BaseItem
    {
        public ImportMe Imported { get; set; }
    }

    public static class Helper
    {
        public static void Help() { }
    }

    interface IViewModel
    {

    }
}
namespace Runner.ForImport
{
    public class ImportMe
    {
        public int Id { get; set; }
    }
}