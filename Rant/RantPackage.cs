﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Rant.IO;
using Rant.IO.Bson;
using Rant.IO.Compression;
using Rant.Vocabulary;

namespace Rant
{
    /// <summary>
    /// Represents a collection of patterns and tables that can be exported to an archive file.
    /// </summary>
    public sealed class RantPackage
    {
        private const string OLD_MAGIC = "RPKG";
        private const string MAGIC = "RANT";
        private const byte PACKAGE_VERSION = 2;

        private HashSet<RantPattern> _patterns;
        private HashSet<RantDictionaryTable> _tables;

        public string Name = "";
        public string Namespace = "";
        public string Description = "";
        public string[] Tags = new string[] { };
        public string Version = "";
        public string[] Authors = new string[] { };

        /// <summary>
        /// Adds the specified pattern to the package.
        /// </summary>
        /// <param name="pattern">The pattern to add.</param>
        public void AddPattern(RantPattern pattern)
            => (_patterns ?? (_patterns = new HashSet<RantPattern>())).Add(pattern);

        /// <summary>
        /// Adds the specified table to the package.
        /// </summary>
        /// <param name="table">The table to add.</param>
        public void AddTable(RantDictionaryTable table)
            => (_tables ?? (_tables = new HashSet<RantDictionaryTable>())).Add(table);

        /// <summary>
        /// Adds the tables from the specified dictionary to the package.
        /// </summary>
        /// <param name="dictionary">The dictionary to add.</param>
        public void AddDictionary(RantDictionary dictionary)
        {
            if (_tables == null) 
                _tables = new HashSet<RantDictionaryTable>();

            foreach (var table in dictionary.GetTables())
            {
                _tables.Add(table);
            }
        }

        /// <summary>
        /// Enumerates the patterns contained in the package.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RantPattern> GetPatterns()
        {
            if (_patterns == null) yield break;
            foreach (var pattern in _patterns)
                yield return pattern;
        }

        /// <summary>
        /// Enumerates the tables contained in the package.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RantDictionaryTable> GetTables()
        {
            if (_tables == null) yield break;
            foreach (var table in _tables)
                yield return table;
        }

        /// <summary>
        /// Saves the package to the specified file path.
        /// </summary>
        /// <param name="path">The path to the file to create.</param>
        public void Save(
            string path, 
            bool compress = true, 
            BsonStringTableMode stringTableMode = BsonStringTableMode.None)
        {
            if (String.IsNullOrEmpty(Path.GetExtension(path)))
            {
                path += ".rantpkg";
            }

            using (var writer = new EasyWriter(File.Create(path)))
            {
                var doc = new BsonDocument(stringTableMode);
                var info = doc.Top["info"] = new BsonItem();
                info["name"] = new BsonItem(Name);
                info["namespace"] = new BsonItem(Namespace);
                info["description"] = new BsonItem(Description);
                info["tags"] = new BsonItem(Tags);
                info["version"] = new BsonItem(Version);
                info["authors"] = new BsonItem(Authors);
                
                var patterns = doc.Top["patterns"] = new BsonItem();
                if(_patterns != null)
                    foreach(var pattern in _patterns)
                        patterns[pattern.Name] = new BsonItem(pattern.Code);

                var tables = doc.Top["tables"] = new BsonItem();
                foreach(var table in _tables)
                {
                    var t = tables[table.Name] = new BsonItem();
                    t["name"] = new BsonItem(table.Name);
                    t["subs"] = new BsonItem(table.Subtypes);
                    t["language"] = new BsonItem(table.Language);
                    t["hidden"] = new BsonItem(table.HiddenClasses.ToArray());
                    t["hints"] = new BsonItem(0);
                    var entries = new List<BsonItem>();
                    foreach(var entry in table.GetEntries())
                    {
                        var e = new BsonItem();
                        if(entry.Weight != 1)
                            e["weight"] = new BsonItem(entry.Weight);
                        var requiredClasses = entry.GetRequiredClasses().ToArray();
                        if(requiredClasses.Length > 0)
                            e["classes"] = new BsonItem(requiredClasses);
                        var optionalClasses = entry.GetOptionalClasses().ToArray();
                        if(optionalClasses.Length > 0)
                            e["optional_classes"] = new BsonItem();
                        var terms = new List<BsonItem>();
                        foreach(var term in entry.Terms)
                        {
                            var et = new BsonItem();
                            et["value"] = new BsonItem(term.Value);
                            if(term.Pronunciation != "")
                                et["pron"] = new BsonItem(term.Pronunciation);
                            terms.Add(et);
                        }
                        e["terms"] = new BsonItem(terms.ToArray());
                        entries.Add(e);
                    }
                    t["entries"] = new BsonItem(entries.ToArray());
                }

                var data = doc.ToByteArray(stringTableMode != BsonStringTableMode.None);
                if (compress)
                    data = EasyCompressor.Compress(data);
                writer.Write(Encoding.ASCII.GetBytes("RANT"));
                writer.Write((uint)2);
                writer.Write(compress);
                writer.Write(data.Length);
                writer.Write(data);
            }
        }
        
        /// <summary>
        /// Saves the package using the old Rant package format to the specified file path.
        /// </summary>
        /// <param name="path">The path to the file to create.</param>
        public void SaveOld(string path)
        {
            if (String.IsNullOrEmpty(Path.GetExtension(path)))
            {
                path += ".rantpkg";
            }

            using (var writer = new EasyWriter(File.Create(path)))
            {
                // Magic
                writer.WriteBytes(Encoding.ASCII.GetBytes(OLD_MAGIC));

                // Counts
                writer.Write(_patterns?.Count ?? 0);
                writer.Write(_tables?.Count ?? 0);

                // Patterns
                if (_patterns != null)
                {
                    foreach (var pattern in _patterns)
                    {
                        writer.Write(pattern.Name);
                        writer.Write(pattern.Code);
                    }
                }

                // Tables
                if (_tables != null)
                {
                    foreach (var table in _tables)
                    {
                        writer
                        .Write(table.Name)
                        .Write(table.Subtypes)
                        .Write(table.EntryCount)
                        .Write(table.HiddenClasses.ToArray());

                        foreach (var entry in table.GetEntries())
                        {
                            writer
                                .Write(entry.Weight)
                                .Write(false) // Used to be the NSFW field, will use for something else in the future!
                                .Write(entry.Terms.Length);

                            for (int i = 0; i < entry.Terms.Length; i++)
                            {
                                writer
                                    .Write(entry.Terms[i].Value)
                                    .Write(entry.Terms[i].Pronunciation);
                            }

                            writer.Write(entry.GetClasses().ToArray());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads a package from the specified path and returns it as a RantPackage object.
        /// </summary>
        /// <param name="path">The path to the package file to load.</param>
        /// <returns></returns>
        public static RantPackage Load(string path)
        {
            if (String.IsNullOrEmpty(Path.GetExtension(path))) path += ".rantpkg";
            return Load(File.Open(path, FileMode.Open));
        }

        /// <summary>
        /// Loads a package from the specified stream and returns it as a RantPackage object.
        /// </summary>
        /// <param name="source">The stream to load the package data from.</param>
        /// <returns></returns>
        public static RantPackage Load(Stream source)
        {
            using (var reader = new EasyReader(source))
            {
                var magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (magic == OLD_MAGIC)
                    return LoadOldPackage(reader);
                else if(magic != MAGIC)
                    throw new InvalidDataException("File is corrupt.");
                var package = new RantPackage();
                var version = reader.ReadUInt32();
                if (version != PACKAGE_VERSION)
                    throw new InvalidDataException("Invalid package version: " + version);
                var compress = reader.ReadBoolean();
                var size = reader.ReadInt32();
                var data = reader.ReadBytes(size);
                if (compress)
                    data = EasyCompressor.Decompress(data);
                var doc = BsonDocument.Read(data);

                var info = doc["info"];
                if(info == null)
                {
                    package.Name = info["name"];
                    package.Namespace = info["namespace"];
                    package.Version = info["version"];
                    package.Description = info["description"];
                    package.Authors = (string[])info["authors"].Value;
                    package.Tags = (string[])info["tags"].Value;
                }

                var patterns = doc["patterns"];
                if(patterns != null)
                {
                    var names = patterns.Keys;
                    foreach (string name in names)
                        package.AddPattern(new RantPattern(name, RantPatternSource.File, patterns[name]));
                }

                var tables = doc["tables"];
                if(tables != null)
                {
                    var names = tables.Keys;
                    foreach(string name in names)
                    {
                        var table = tables[name];
                        string tableName = table["name"];
                        string[] tableSubs = (string[])table["subs"];
                        string[] hiddenClasses = (string[])table["hidden"];

                        var entries = new List<RantDictionaryEntry>();
                        var entryList = table["entries"];
                        for(var i = 0; i < entryList.KeyCount; i++)
                        {
                            var loadedEntry = entryList[i];
                            int weight = 1;
                            if (loadedEntry.HasKey("weight"))
                                weight = (int)loadedEntry["weight"].Value;
                            string[] requiredClasses = (string[])loadedEntry["classes"];
                            string[] optionalClasses = (string[])loadedEntry["optional_classes"];
                            var terms = new List<RantDictionaryTerm>();
                            var termList = loadedEntry["terms"];
                            for(var j = 0; j < termList.KeyCount; j++)
                            {
                                var t = new RantDictionaryTerm(termList[j]["value"], termList[j]["pron"]);
                                terms.Add(t);
                            }
                            var entry = new RantDictionaryEntry(
                                terms.ToArray(),
                                requiredClasses.Concat(optionalClasses.Select(x => x + "?")),
                                weight
                            );
                            entries.Add(entry);
                        }
                        var rantTable = new RantDictionaryTable(
                            tableName,
                            tableSubs,
                            entries,
                            hiddenClasses
                        );
                        package.AddTable(rantTable);
                    }
                }

                return package;
            }
        }

        private static RantPackage LoadOldPackage(EasyReader reader)
        {
            int numPatterns = reader.ReadInt32();
            int numTables = reader.ReadInt32();

            if (numPatterns < 0 || numTables < 0)
                throw new InvalidDataException("File is corrupt.");

            var pkg = new RantPackage();

            // Patterns
            for (int i = 0; i < numPatterns; i++)
            {
                var name = reader.ReadString();
                var code = reader.ReadString();

                pkg.AddPattern(new RantPattern(name, RantPatternSource.String, code));
            }

            // Tables
            for (int i = 0; i < numTables; i++)
            {
                var name = reader.ReadString();
                var subs = reader.ReadStringArray();
                int numEntries = reader.ReadInt32();
                var hiddenClasses = reader.ReadStringArray();
                var entries = new RantDictionaryEntry[numEntries];

                for (int j = 0; j < numEntries; j++)
                {
                    int weight = reader.ReadInt32();
                    bool flags = reader.ReadBoolean(); // unused
                    int numTerms = reader.ReadInt32();
                    var terms = new RantDictionaryTerm[numTerms];

                    for (int k = 0; k < numTerms; k++)
                    {
                        var value = reader.ReadString();
                        var pron = reader.ReadString();
                        terms[k] = new RantDictionaryTerm(value, pron);
                    }

                    var classes = reader.ReadStringArray();

                    entries[j] = new RantDictionaryEntry(terms, classes, weight);
                }

                pkg.AddTable(new RantDictionaryTable(name, subs, entries, hiddenClasses));
            }

            return pkg;
        }
    }
}