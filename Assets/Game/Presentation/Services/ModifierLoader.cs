using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using OneMoreTurn.Core;
using OneMoreTurn.Core.Serialization;
using OneMoreTurn.Core.Validation;
using UnityEngine;

namespace OneMoreTurn.Presentation.Services
{
    /// <summary>
    /// Loads modifier definitions from JSON files.
    /// </summary>
    public class ModifierLoader
    {
        private readonly ModifierRegistry _registry;

        public ModifierLoader(ModifierRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        /// <summary>
        /// Load all modifier JSON files from StreamingAssets/Modifiers folder.
        /// </summary>
        public LoadResult LoadFromStreamingAssets(string subfolder = "Modifiers")
        {
            string basePath = Path.Combine(Application.streamingAssetsPath, subfolder);
            return LoadFromDirectory(basePath);
        }

        /// <summary>
        /// Load all modifier JSON files from a directory.
        /// </summary>
        public LoadResult LoadFromDirectory(string directoryPath)
        {
            var result = new LoadResult();

            if (!Directory.Exists(directoryPath))
            {
                result.Errors.Add($"Directory not found: {directoryPath}");
                return result;
            }

            var jsonFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.AllDirectories);

            foreach (var filePath in jsonFiles)
            {
                try
                {
                    LoadFromFile(filePath, result);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error loading {filePath}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Load modifiers from a single JSON file.
        /// </summary>
        public LoadResult LoadFromFile(string filePath)
        {
            var result = new LoadResult();
            LoadFromFile(filePath, result);
            return result;
        }

        private void LoadFromFile(string filePath, LoadResult result)
        {
            string json = File.ReadAllText(filePath);
            var fileJson = JsonConvert.DeserializeObject<ModifierFileJson>(json);

            if (fileJson?.modifiers == null || fileJson.modifiers.Count == 0)
            {
                result.Warnings.Add($"No modifiers found in {Path.GetFileName(filePath)}");
                return;
            }

            var definitions = ModifierConverter.ConvertAll(fileJson);

            // Validate all definitions
            var validation = ModifierValidator.ValidateAll(definitions);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    result.Errors.Add($"{Path.GetFileName(filePath)}: {error}");
                }
                return;
            }

            // Register valid definitions
            foreach (var def in definitions)
            {
                _registry.Register(def);
                result.LoadedModifiers.Add(def.Id);
            }

            result.FilesProcessed++;
        }

        /// <summary>
        /// Load modifiers from a JSON string (useful for testing or embedded resources).
        /// </summary>
        public LoadResult LoadFromJson(string json, string sourceName = "json")
        {
            var result = new LoadResult();

            try
            {
                var fileJson = JsonConvert.DeserializeObject<ModifierFileJson>(json);

                if (fileJson?.modifiers == null || fileJson.modifiers.Count == 0)
                {
                    result.Warnings.Add($"No modifiers found in {sourceName}");
                    return result;
                }

                var definitions = ModifierConverter.ConvertAll(fileJson);

                var validation = ModifierValidator.ValidateAll(definitions);
                if (!validation.IsValid)
                {
                    foreach (var error in validation.Errors)
                    {
                        result.Errors.Add($"{sourceName}: {error}");
                    }
                    return result;
                }

                foreach (var def in definitions)
                {
                    _registry.Register(def);
                    result.LoadedModifiers.Add(def.Id);
                }

                result.FilesProcessed++;
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"{sourceName}: JSON parse error - {ex.Message}");
            }

            return result;
        }
    }

    public class LoadResult
    {
        public int FilesProcessed { get; set; }
        public List<string> LoadedModifiers { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool IsSuccess => !HasErrors && LoadedModifiers.Count > 0;

        public void LogToConsole()
        {
            if (IsSuccess)
            {
                Debug.Log($"[ModifierLoader] Loaded {LoadedModifiers.Count} modifiers from {FilesProcessed} file(s)");
            }

            foreach (var warning in Warnings)
            {
                Debug.LogWarning($"[ModifierLoader] {warning}");
            }

            foreach (var error in Errors)
            {
                Debug.LogError($"[ModifierLoader] {error}");
            }
        }
    }
}
