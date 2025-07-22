using Runner.ForImport;
using System;
using System.Collections.Generic;

namespace Runner
{

    public class Item : BaseItem, IViewModel, IHasIgnore
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public bool IsWhatever { get; set; }
        public IEnumerable<string> Collection { get; set; }
        public ListEx List { get; set; }
        public double[] Array { get; set; }
        public (int, string) ValueTuple { get; set; }
        public Tuple<int, string> Tuple { get; set; }
        public int? Nullable { get; set; }
        public GenericItem<string> Generic { get; set; }
        public Dictionary<string, BaseItem> Dictionary { get; set; }
        public Dictionary<Guid, BaseItem> Dictionary2 { get; set; }
        public DateTime Date { get; set; }
        public string Hello => "Hello World!";
        public byte[] File { get; set; }
        [JsonIgnore]
        public string IgnoreMe { get; set; }
        [PropertyName("hasBeenRename")]
        public string RenameMe { get; set; }
        public string WhatIs { get; set; }
    }
    public class ListEx
    {

    }
    public class GenericItem<TStuff> : IImportMe<Item>
    {
        public TStuff Stuff { get; set; }
        public GenericItem<TStuff> Circle { get; set; }
        public Item X { get; set; }
    }

    public abstract class BaseItem : IImportMe<Item>
    {
        public ImportMe Imported { get; set; }
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

    public enum Status
    {
        None = 0,
        Done = 1
    }

    interface IViewModel : IImportMe3<IImportMe<BaseItem>, int, string>
    {

    }

    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class PropertyNameAttribute : Attribute
    {
        public PropertyNameAttribute(string name)
        {
            Name = name;
        }
       
        public string Name { get; }
    }
    [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class JsonIgnoreAttribute : Attribute
    {
    }
}
namespace Runner.ForImport
{
    public class ImportMe
    {
        public int Id { get; set; }
    }
    public interface IImportMe<T>
    {
        public T X { get; set; }
    }
    public interface IImportMe3<T, T1, T2>
    {
    }

    public interface IHasIgnore
    {
        public string IgnoreMe { get; set; }
        public string WhatIs { get; set; }
    }
}