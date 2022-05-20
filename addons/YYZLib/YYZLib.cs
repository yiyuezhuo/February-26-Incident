using System;
using Godot;
using System.Collections.Generic;

using CsvHelper;
using System.Globalization; // For `CultureInfo.InvariantCulture`


namespace YYZ
{
    public static class Debug
    {
        public static void Log(object obj)
        {
            Godot.GD.Print(obj);
        }
    }

    public static class Text
    {
        /// <summary>
        /// Works only in the editor.
        /// </summary>
        public static string ReadBySharp(string path)
        {
            path = ProjectSettings.GlobalizePath(path);
            return System.IO.File.ReadAllText(path); // Godot's `res://` path should be resolved by Godot itself
        }

        public static string ReadByGodotAPI(string path)
        {
            var dataFile = new Godot.File(); // use `System.IO.File` instead of `Godot.File`?
            Godot.Error err = dataFile.Open(path, Godot.File.ModeFlags.Read);
            if(err != Godot.Error.Ok)
            {
                Godot.GD.PrintErr($"Open Text file failed: {err}");
            }
            string dataText = dataFile.GetAsText();
            dataFile.Close();
            return dataText;
        }

        public static string Read(string path) => ReadByGodotAPI(path);

        public static CsvReader GetCsvReader(string path)
        {
            var csvString = Read(path);
            var csvReader = new CsvReader(new System.IO.StringReader(csvString), CultureInfo.InvariantCulture);
            return csvReader;
        }
    }

    public abstract class ResourceNeedSetup : Resource
    {
        bool isSetup = false;

        public ResourceNeedSetup()
        {
            // When _init is called, "exported" variables are not initialized yet. So we use `CallDeferred` as a weird hack.
            // Also see: https://github.com/godotengine/godot-proposals/issues/296
            //CallDeferred(nameof(EnsureSetup));
        }

        public void EnsureSetup()
        {
            if(!isSetup)
            {
                isSetup = true;
                Setup();
                // isSetup = false;
            }
        }

        protected abstract void Setup();

        public bool GetIsSetup() => isSetup;
    }

    public abstract class ResourceFakeSetup: Resource
    {
        protected abstract void Setup();
        public void EnsureSetup()
        {
            GD.Print("Fake EnsureSetup");
        }
    }

    public static class Random
    {
        static System.Random random;
        public static System.Random GetRandom()
        {
            if(random == null)
                random = new System.Random();

            return random;
        }

        public static float NextFloat()
        {
            var rnd = GetRandom();
            return (float)rnd.NextDouble();
        }

        public static int Next()
        {
            var rnd = GetRandom();
            return rnd.Next();
        }
    }
}