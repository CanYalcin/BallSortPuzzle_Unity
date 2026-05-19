//
//  AppLovinBuildPostProcessor.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 8/29/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if UNITY_ANDROID

using System.IO;
using UnityEditor.Android;

namespace AppLovinMax.Scripts.IntegrationManager.Editor
{
    /// <summary>
    /// Adds Quality Service plugin to the Gradle project once the project has been exported. See <see cref="AppLovinProcessGradleBuildFile"/> for more details.
    /// </summary>
    public class AppLovinPostProcessGradleProject : AppLovinProcessGradleBuildFile, IPostGenerateGradleAndroidProject
    {
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!AppLovinSettings.Instance.QualityServiceEnabled) return;

            var failedToAddPlugin = !AddQualityServiceToRootGradleFile(path);
            if (failedToAddPlugin)
            {
                MaxSdkLogger.UserWarning("Failed to add AppLovin Quality Service plugin to the gradle project.");
                return;
            }

            // The plugin needs to be added to the application module (named launcher)
            var applicationGradleBuildFilePath = Path.Combine(path, "../launcher/build.gradle");

            if (!File.Exists(applicationGradleBuildFilePath))
            {
                MaxSdkLogger.UserWarning("Couldn't find build.gradle file. Failed to add AppLovin Quality Service plugin to the gradle project.");
                return;
            }

            AddAppLovinQualityServicePlugin(applicationGradleBuildFilePath);
        }

        public int callbackOrder
        {
            get { return CallbackOrder; }
        }
    }
}

#endif
