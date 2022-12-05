using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FivePebblesBadApple
{
    [BepInPlugin("forthbridge.five_pebbles_bad_apple", "FivePebblesBadApple", "0.1.0")]
    public class FivePebblesBadApple : BaseUnityPlugin
    {
        #region BepInEx Logger
        // Allows access to BepInEx logger from other classes, see: https://rainworldmodding.miraheze.org/wiki/Code_Environments
        private static WeakReference self;
        public static FivePebblesBadApple SELF => self?.Target as FivePebblesBadApple;
        public ManualLogSource Logger_p => Logger;
        public FivePebblesBadApple() => self = new WeakReference(this);
        #endregion

        public static List<string> frames = new List<string>();

        // The application of all hooks is delegated to a static class
        public void OnEnable() => Hooks.ApplyHooks();

        const string FRAMES_RESOURCE_PATH = "FivePebblesBadApple.Frames";

        // Credit to LB on the RW Discord!
        // Loads all frames into the 'frames' array, each frame is an embedded resource png located under the 'Frames' directory in the assembly
        public static void LoadFrames()
        {
            // Get all embedded resources from the assembly via reflection
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Declare a Unity Texture2D
            Texture2D frameTexture = new Texture2D(0, 0, TextureFormat.RGB24, false)
            {
                anisoLevel = 1,
                filterMode = 0
            };

            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                // Ensure that the resource is under the frames directory
                if (!resourceName.StartsWith(FRAMES_RESOURCE_PATH)) continue;

                // Get both the resource stream from the resource name and declare a new memory stream
                using Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
                using MemoryStream memoryStream = new MemoryStream();

                byte[] buffer = new byte[16384];
                int read;

                // Write the resource stream into a memory stream
                while ((read = resourceStream!.Read(buffer, 0, buffer.Length)) > 0) memoryStream.Write(buffer, 0, read);

                // Write the current memory stream onto the texture
                frameTexture.LoadImage(memoryStream.ToArray());

                string frameName = "FPBadApple_" + FAtlasManager._nextAtlasIndex;

                FAtlas atlas = new FAtlas(frameName, frameTexture, FAtlasManager._nextAtlasIndex);
                Futile.atlasManager.AddAtlas(atlas);
                FAtlasManager._nextAtlasIndex++;

                // Finally, add the texture to the list of frames
                frames.Add(frameName);

                SELF.Logger_p.LogInfo(resourceName);
                if (FAtlasManager._nextAtlasIndex > 500) break;
            }
        }
    }
}
