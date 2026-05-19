//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 9/3/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_ANDROID

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AppLovinMax.Internal;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    [Serializable]
    public class AppLovinQualityServiceData
    {
        // ReSharper disable once InconsistentNaming - Need to keep name for response data
        public string api_key;
    }

    /// <summary>
    /// Adds or updates the AppLovin Quality Service plugin to the provided build.gradle file.
    /// If the gradle file already has the plugin, the API key is updated.
    /// </summary>
    public abstract class AppLovinProcessGradleBuildFile : AppLovinPreProcess
    {
        private static readonly Regex TokenBuildScriptRepositories = new Regex(".*repositories.*");
        private static readonly Regex TokenBuildScriptDependencies = new Regex(".*classpath \'com.android.tools.build:gradle.*");
        private static readonly Regex TokenApplicationPlugin = new Regex(".*apply plugin: \'com.android.application\'.*");
        private static readonly Regex TokenApiKey = new Regex(".*apiKey.*");
        private static readonly Regex TokenAppLovinPlugin = new Regex(".*apply plugin:.+?(?=applovin-quality-service).*");

        private const string PluginsMatcher = "plugins";
        private const string PluginManagementMatcher = "pluginManagement";
        private const string QualityServicePluginRoot = "    id 'com.applovin.quality' version '+' apply false // NOTE: Requires version 4.8.3+ for Gradle version 7.2+";

        private const string BuildScriptMatcher = "buildscript";
        private const string QualityServiceMavenRepo = "maven { url 'https://artifacts.applovin.com/android'; content { includeGroupByRegex 'com.applovin.*' } }";
        private const string QualityServiceDependencyClassPath = "classpath 'com.applovin.quality:AppLovinQualityServiceGradlePlugin:+'";
        private const string QualityServiceApplyPlugin = "apply plugin: 'applovin-quality-service'";
        private const string QualityServicePlugin = "applovin {";
        private const string QualityServiceApiKey = "    apiKey '{0}'";
        private const string QualityServiceBintrayMavenRepo = "https://applovin.bintray.com/Quality-Service";
        private const string QualityServiceNoRegexMavenRepo = "maven { url 'https://artifacts.applovin.com/android' }";

        // Legacy plugin detection variables
        private const string QualityServiceDependencyClassPathV3 = "classpath 'com.applovin.quality:AppLovinQualityServiceGradlePlugin:3.+'";
        private static readonly Regex TokenSafeDkLegacyApplyPlugin = new Regex(".*apply plugin:.+?(?=safedk).*");
        private const string SafeDkLegacyPlugin = "safedk {";
        private const string SafeDkLegacyMavenRepo = "http://download.safedk.com";
        private const string SafeDkLegacyDependencyClassPath = "com.safedk:SafeDKGradlePlugin:";

        /// <summary>
        /// Adds the Quality Service plugin to the root gradle file.
        /// </summary>
        /// <param name="path">The path to the unityLibrary's module.</param>
        /// <returns>True if the plugin was added successfully, otherwise return false</returns>
        protected static bool AddQualityServiceToRootGradleFile(string path)
        {
            var rootGradleBuildFilePath = Path.Combine(path, "../build.gradle");
            var shouldAddQualityServiceToDependencies = ShouldAddQualityServiceToDependencies(rootGradleBuildFilePath);

            if (shouldAddQualityServiceToDependencies)
            {
                // Add the Quality Service Plugin to the dependencies block in the root build.gradle file
                return AddQualityServiceBuildScriptLines(rootGradleBuildFilePath);
            }

            // Add the Quality Service Plugin to the plugin block in the root build.gradle file
            var rootSettingsGradleFilePath = Path.Combine(path, "../settings.gradle");
            var qualityServiceAdded = AddPluginToRootGradleBuildFile(rootGradleBuildFilePath);
            var appLovinRepositoryAdded = AddAppLovinRepository(rootSettingsGradleFilePath);
            return qualityServiceAdded && appLovinRepositoryAdded;
        }

        /// <summary>
        /// Determines whether the AppLovin Quality Service plugin should be added to the 
        /// dependencies block in the root build.gradle file or to the plugins block.
        ///
        /// Gradle's required structure for including plugins varies by version:
        /// - Older versions of Gradle require the plugin to be added to the dependencies block.
        ///    Example:
        ///        dependencies {
        ///            classpath 'com.android.tools.build:gradle:4.0.1'
        ///            classpath 'com.applovin.quality:AppLovinQualityServiceGradlePlugin:+'
        ///        }
        ///
        /// - Newer versions of gradle require the plugin to be added to the plugins block.
        ///    Example:
        ///        plugins {
        ///            id 'com.android.application' version '7.4.2' apply false
        ///            id 'com.android.library' version '7.4.2' apply false
        ///            id 'com.applovin.quality' version '+' apply false
        ///        }
        ///
        /// Since Unity projects may use custom Gradle versions depending on the Unity version or 
        /// user modifications, this check ensures proper integration of the AppLovin plugin.
        /// </summary>
        /// <param name="rootGradleBuildFile">The path to project's root build.gradle file.</param>
        /// <returns><c>true</c> if the file contains a `dependencies` block, indicating an older Gradle version</returns>
        private static bool ShouldAddQualityServiceToDependencies(string rootGradleBuildFile)
        {
            var lines = File.ReadAllLines(rootGradleBuildFile).ToList();
            return lines.Any(line => TokenBuildScriptDependencies.IsMatch(line));
        }

        /// <summary>
        /// Updates the provided Gradle script to add Quality Service plugin.
        /// </summary>
        /// <param name="applicationGradleBuildFilePath">The gradle file to update.</param>
        protected static void AddAppLovinQualityServicePlugin(string applicationGradleBuildFilePath)
        {
            if (!AppLovinSettings.Instance.QualityServiceEnabled) return;

            var sdkKey = AppLovinSettings.Instance.SdkKey;
            if (string.IsNullOrEmpty(sdkKey))
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. SDK Key is empty. Please enter the AppLovin SDK Key in the Integration Manager.");
                return;
            }

            // Retrieve the API Key using the SDK Key.
            var qualityServiceData = RetrieveQualityServiceData(sdkKey);
            var apiKey = qualityServiceData.api_key;
            if (string.IsNullOrEmpty(apiKey))
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. API Key is empty.");
                return;
            }

            // Generate the updated Gradle file that needs to be written.
            var lines = File.ReadAllLines(applicationGradleBuildFilePath).ToList();
            var sanitizedLines = RemoveLegacySafeDkPlugin(lines);
            var outputLines = GenerateUpdatedBuildFileLines(
                sanitizedLines,
                apiKey,
                false // The buildscript closure related lines will to be added to the root build.gradle file.
            );
            // outputLines can be null if we couldn't add the plugin. 
            if (outputLines == null) return;

            try
            {
                File.WriteAllText(applicationGradleBuildFilePath, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Gradle file write failed.");
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// Adds AppLovin Quality Service plugin DSL element to the project's root build.gradle file.
        /// Sample build.gradle file after adding quality service:
        /// plugins {
        ///     id 'com.android.application' version '7.4.2' apply false
        ///     id 'com.android.library' version '7.4.2' apply false
        ///     id 'com.applovin.quality' version '+' apply false
        /// }
        /// tasks.register('clean', Delete) {
        ///     delete rootProject.layout.buildDirectory
        /// }
        ///
        /// </summary>
        /// <param name="rootGradleBuildFile">The path to project's root build.gradle file.</param>
        /// <returns><c>true</c> when the plugin was added successfully.</returns>
        private static bool AddPluginToRootGradleBuildFile(string rootGradleBuildFile)
        {
            var lines = File.ReadAllLines(rootGradleBuildFile).ToList();

            // Check if the plugin is already added to the file.
            var pluginAdded = lines.Any(line => line.Contains(QualityServicePluginRoot));
            if (pluginAdded) return true;

            var outputLines = new List<string>();
            var insidePluginsClosure = false;
            foreach (var line in lines)
            {
                if (line.Contains(PluginsMatcher))
                {
                    insidePluginsClosure = true;
                }

                if (!pluginAdded && insidePluginsClosure && line.Contains("}"))
                {
                    outputLines.Add(QualityServicePluginRoot);
                    pluginAdded = true;
                    insidePluginsClosure = false;
                }

                outputLines.Add(line);
            }

            if (!pluginAdded) return false;

            try
            {
                File.WriteAllText(rootGradleBuildFile, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Root Gradle file write failed.");
                Console.WriteLine(exception);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds the AppLovin maven repository to the project's settings.gradle file.
        /// Sample settings.gradle file after adding AppLovin Repository:
        /// pluginManagement {
        ///     repositories {
        ///         maven { url 'https://artifacts.applovin.com/android'; content { includeGroupByRegex 'com.applovin.*' } }
        ///
        ///         gradlePluginPortal()
        ///         google()
        ///         mavenCentral()
        ///     }
        /// }
        /// ...
        ///
        /// </summary>
        /// <param name="settingsGradleFile">The path to the project's settings.gradle file.</param>
        /// <returns><c>true</c> if the repository was added successfully.</returns>
        private static bool AddAppLovinRepository(string settingsGradleFile)
        {
            var lines = File.ReadLines(settingsGradleFile).ToList();
            var outputLines = new List<string>();
            var mavenRepoAdded = false;
            var pluginManagementClosureDepth = 0;
            var insidePluginManagementClosure = false;
            var pluginManagementMatched = false;
            foreach (var line in lines)
            {
                outputLines.Add(line);

                if (!pluginManagementMatched && line.Contains(PluginManagementMatcher))
                {
                    pluginManagementMatched = true;
                    insidePluginManagementClosure = true;
                }

                if (insidePluginManagementClosure)
                {
                    if (line.Contains("{"))
                    {
                        pluginManagementClosureDepth++;
                    }

                    if (line.Contains("}"))
                    {
                        pluginManagementClosureDepth--;
                    }

                    if (pluginManagementClosureDepth == 0)
                    {
                        insidePluginManagementClosure = false;
                    }
                }

                if (insidePluginManagementClosure)
                {
                    if (!mavenRepoAdded && TokenBuildScriptRepositories.IsMatch(line))
                    {
                        outputLines.Add(GetFormattedBuildScriptLine(QualityServiceMavenRepo));
                        mavenRepoAdded = true;
                    }
                }
            }

            if (!mavenRepoAdded) return false;

            try
            {
                File.WriteAllText(settingsGradleFile, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Setting Gradle file write failed.");
                Console.WriteLine(exception);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds the necessary AppLovin Quality Service dependency and maven repo lines to the provided root build.gradle file.
        /// Sample build.gradle file after adding quality service:
        /// allprojects {
        ///     buildscript {
        ///         repositories {
        ///             maven { url 'https://artifacts.applovin.com/android'; content { includeGroupByRegex 'com.applovin.*' } }
        ///             google()
        ///             jcenter()
        ///         }
        ///
        ///         dependencies {
        ///             classpath 'com.android.tools.build:gradle:4.0.1'
        ///             classpath 'com.applovin.quality:AppLovinQualityServiceGradlePlugin:+'
        ///         }
        ///     ...
        ///
        /// </summary>
        /// <param name="rootGradleBuildFile">The root build.gradle file path</param>
        /// <returns><c>true</c> if the build script lines were applied correctly.</returns>
        private static bool AddQualityServiceBuildScriptLines(string rootGradleBuildFile)
        {
            var lines = File.ReadAllLines(rootGradleBuildFile).ToList();
            var outputLines = GenerateUpdatedBuildFileLines(lines, null, true);

            // outputLines will be null if we couldn't add the build script lines.
            if (outputLines == null) return false;

            try
            {
                File.WriteAllText(rootGradleBuildFile, string.Join("\n", outputLines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to install AppLovin Quality Service plugin. Root Gradle file write failed.");
                Console.WriteLine(exception);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Removes the AppLovin Quality Service Plugin or Legacy SafeDK plugin from the given gradle template file if either of them are present.
        /// </summary>
        /// <param name="gradleTemplateFile">The gradle template file from which to remove the plugin from</param>
        protected static void RemoveAppLovinQualityServiceOrSafeDkPlugin(string gradleTemplateFile)
        {
            var lines = File.ReadAllLines(gradleTemplateFile).ToList();
            lines = RemoveLegacySafeDkPlugin(lines);
            lines = RemoveAppLovinQualityServicePlugin(lines);

            try
            {
                File.WriteAllText(gradleTemplateFile, string.Join("\n", lines.ToArray()) + "\n");
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to remove AppLovin Quality Service Plugin from mainTemplate.gradle. Please remove the Quality Service plugin from the mainTemplate.gradle manually.");
                Console.WriteLine(exception);
            }
        }

        private static AppLovinQualityServiceData RetrieveQualityServiceData(string sdkKey)
        {
            var webRequestConfig = new WebRequestConfig()
            {
                JsonString = string.Format("{{\"sdk_key\" : \"{0}\"}}", sdkKey),
                EndPoint = "https://api2.safedk.com/v1/build/cred",
                RequestType = WebRequestType.Post,
            };

            webRequestConfig.Headers.Add("Content-Type", "application/json");

            var maxWebRequest = new MaxWebRequest(webRequestConfig);
            var webResponse = maxWebRequest.SendSync();

            if (!webResponse.IsSuccess)
            {
                MaxSdkLogger.UserError("Failed to retrieve API Key for SDK Key: " + sdkKey + "with error: " + webResponse.ErrorMessage);
                return new AppLovinQualityServiceData();
            }

            try
            {
                return JsonUtility.FromJson<AppLovinQualityServiceData>(webResponse.ResponseMessage);
            }
            catch (Exception exception)
            {
                MaxSdkLogger.UserError("Failed to parse API Key." + exception);
                return new AppLovinQualityServiceData();
            }
        }

        private static List<string> RemoveLegacySafeDkPlugin(List<string> lines)
        {
            return RemovePlugin(lines, SafeDkLegacyPlugin, SafeDkLegacyMavenRepo, SafeDkLegacyDependencyClassPath, TokenSafeDkLegacyApplyPlugin);
        }

        private static List<string> RemoveAppLovinQualityServicePlugin(List<string> lines)
        {
            return RemovePlugin(lines, QualityServicePlugin, QualityServiceMavenRepo, QualityServiceDependencyClassPath, TokenAppLovinPlugin);
        }

        private static List<string> RemovePlugin(List<string> lines, string pluginLine, string mavenRepo, string dependencyClassPath, Regex applyPluginToken)
        {
            var sanitizedLines = new List<string>();
            var legacyRepoRemoved = false;
            var legacyDependencyClassPathRemoved = false;
            var legacyPluginRemoved = false;
            var legacyPluginMatched = false;
            var insideLegacySafeDkClosure = false;
            foreach (var line in lines)
            {
                if (!legacyPluginMatched && line.Contains(pluginLine))
                {
                    legacyPluginMatched = true;
                    insideLegacySafeDkClosure = true;
                }

                if (insideLegacySafeDkClosure && line.Contains("}"))
                {
                    insideLegacySafeDkClosure = false;
                    continue;
                }

                if (insideLegacySafeDkClosure)
                {
                    continue;
                }

                if (!legacyRepoRemoved && line.Contains(mavenRepo))
                {
                    legacyRepoRemoved = true;
                    continue;
                }

                if (!legacyDependencyClassPathRemoved && line.Contains(dependencyClassPath))
                {
                    legacyDependencyClassPathRemoved = true;
                    continue;
                }

                if (!legacyPluginRemoved && applyPluginToken.IsMatch(line))
                {
                    legacyPluginRemoved = true;
                    continue;
                }

                sanitizedLines.Add(line);
            }

            return sanitizedLines;
        }

        private static List<string> GenerateUpdatedBuildFileLines(List<string> lines, string apiKey, bool addBuildScriptLines)
        {
            // Check if the plugin exists, if so, update the SDK Key.
            var pluginExists = lines.Any(line => TokenAppLovinPlugin.IsMatch(line));
            return pluginExists ? UpdateExistingPlugin(lines, apiKey) : AddPluginAndBuildScript(lines, apiKey, addBuildScriptLines);
        }

        private static List<string> UpdateExistingPlugin(List<string> lines, string apiKey)
        {
            // A sample of the template file.
            // ...
            // allprojects {
            //     repositories {**ARTIFACTORYREPOSITORY**
            //         google()
            //         jcenter()
            //         flatDir {
            //             dirs 'libs'
            //         }
            //     }
            // }
            //
            // apply plugin: 'com.android.application'
            //     **APPLY_PLUGINS**
            //
            // dependencies {
            //     implementation fileTree(dir: 'libs', include: ['*.jar'])
            //     **DEPS**}
            // ...
            var outputLines = new List<string>();
            var pluginMatched = false;
            var insideAppLovinClosure = false;
            var updatedApiKey = false;
            var mavenRepoUpdated = false;
            var dependencyClassPathUpdated = false;
            foreach (var line in lines)
            {
                // Bintray maven repo is no longer being used. Update to s3 maven repo with regex check
                if (!mavenRepoUpdated && (line.Contains(QualityServiceBintrayMavenRepo) || line.Contains(QualityServiceNoRegexMavenRepo)))
                {
                    outputLines.Add(GetFormattedBuildScriptLine(QualityServiceMavenRepo));
                    mavenRepoUpdated = true;
                    continue;
                }

                // We no longer use version specific dependency class path. Just use + for version to always pull the latest.
                if (!dependencyClassPathUpdated && line.Contains(QualityServiceDependencyClassPathV3))
                {
                    outputLines.Add(GetFormattedBuildScriptLine(QualityServiceDependencyClassPath));
                    dependencyClassPathUpdated = true;
                    continue;
                }

                if (!pluginMatched && line.Contains(QualityServicePlugin))
                {
                    insideAppLovinClosure = true;
                    pluginMatched = true;
                }

                if (insideAppLovinClosure && line.Contains("}"))
                {
                    insideAppLovinClosure = false;
                }

                // Update the API key.
                if (insideAppLovinClosure && !updatedApiKey && TokenApiKey.IsMatch(line))
                {
                    outputLines.Add(string.Format(QualityServiceApiKey, apiKey));
                    updatedApiKey = true;
                }
                // Keep adding the line until we find and update the plugin.
                else
                {
                    outputLines.Add(line);
                }
            }

            return outputLines;
        }

        private static List<string> AddPluginAndBuildScript(List<string> lines, string apiKey, bool addBuildScriptLines)
        {
            var shouldAddPlugin = MaxSdkUtils.IsValidString(apiKey);
            if (shouldAddPlugin)
            {
                lines = AddPlugin(lines, apiKey);
                if (lines == null) return null;
            }

            if (!addBuildScriptLines) return lines;

            lines = AddBuildScript(lines);
            return lines;
        }

        private static List<string> AddBuildScript(List<string> lines)
        {
            var outputLines = new List<string>();
            var buildScriptClosureDepth = 0;
            var insideBuildScriptClosure = false;
            var buildScriptMatched = false;
            var qualityServiceRepositoryAdded = false;
            var qualityServiceDependencyClassPathAdded = false;
            foreach (var line in lines)
            {
                // Add the line to the output lines.
                outputLines.Add(line);

                if (!buildScriptMatched && line.Contains(BuildScriptMatcher))
                {
                    buildScriptMatched = true;
                    insideBuildScriptClosure = true;
                }

                // Match the parenthesis to track if we are still inside the buildscript closure.
                if (insideBuildScriptClosure)
                {
                    if (line.Contains("{"))
                    {
                        buildScriptClosureDepth++;
                    }

                    if (line.Contains("}"))
                    {
                        buildScriptClosureDepth--;
                    }

                    if (buildScriptClosureDepth == 0)
                    {
                        insideBuildScriptClosure = false;

                        // There may be multiple buildscript closures and we need to keep looking until we added both the repository and classpath.
                        buildScriptMatched = qualityServiceRepositoryAdded && qualityServiceDependencyClassPathAdded;
                    }
                }

                if (insideBuildScriptClosure)
                {
                    // Add the build script dependency repositories.
                    if (!qualityServiceRepositoryAdded && TokenBuildScriptRepositories.IsMatch(line))
                    {
                        outputLines.Add(GetFormattedBuildScriptLine(QualityServiceMavenRepo));
                        qualityServiceRepositoryAdded = true;
                    }
                    // Add the build script dependencies.
                    else if (!qualityServiceDependencyClassPathAdded && TokenBuildScriptDependencies.IsMatch(line))
                    {
                        outputLines.Add(GetFormattedBuildScriptLine(QualityServiceDependencyClassPath));
                        qualityServiceDependencyClassPathAdded = true;
                    }
                }
            }

            if (!qualityServiceRepositoryAdded || !qualityServiceDependencyClassPathAdded)
            {
                return null;
            }

            return outputLines;
        }

        private static List<string> AddPlugin(List<string> lines, string apiKey)
        {
            var outputLines = new List<string>();
            var qualityServicePluginAdded = false;
            foreach (var line in lines)
            {
                outputLines.Add(line);

                // Add the plugin.
                if (qualityServicePluginAdded || !TokenApplicationPlugin.IsMatch(line)) continue;

                outputLines.Add(QualityServiceApplyPlugin);
                outputLines.AddRange(GenerateAppLovinPluginClosure(apiKey));
                qualityServicePluginAdded = true;
            }

            return qualityServicePluginAdded ? outputLines : null;
        }

        public static string GetFormattedBuildScriptLine(string buildScriptLine)
        {
#if UNITY_2022_2_OR_NEWER
            return "        "
#else
            return "            "
#endif
                   + buildScriptLine;
        }

        private static IEnumerable<string> GenerateAppLovinPluginClosure(string apiKey)
        {
            // applovin {
            //     // NOTE: DO NOT CHANGE - this is NOT your AppLovin MAX SDK key - this is a derived key.
            //     apiKey "456...a1b"
            // }
            var linesToInject = new List<string>(5);
            linesToInject.Add("");
            linesToInject.Add("applovin {");
            linesToInject.Add("    // NOTE: DO NOT CHANGE - this is NOT your AppLovin MAX SDK key - this is a derived key.");
            linesToInject.Add(string.Format(QualityServiceApiKey, apiKey));
            linesToInject.Add("}");

            return linesToInject;
        }
    }
}

#endif
