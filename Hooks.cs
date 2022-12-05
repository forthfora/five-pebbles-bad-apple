using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FivePebblesBadApple
{
    static class Hooks
    {
        public static void ApplyHooks()
        {
            // Rain World Startup
            On.RainWorld.Start += RainWorldStartHook;

            On.SoundLoader.LoadSounds += SoundLoaderLoadSoundsHook;

            //ProjectedImage constructor hook for hiding LoadFile()
            On.ProjectedImage.ctor += ProjectedImageCtorHook;

            // Five Pebbles Update
            On.SSOracleBehavior.Update += SSOracleBehaviorUpdateHook;
        }

        private static void RainWorldStartHook(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            FivePebblesBadApple.SELF.Logger_p.LogInfo("Loading Frames...");
            FivePebblesBadApple.LoadFrames();
            FivePebblesBadApple.SELF.Logger_p.LogInfo("Finished Loading Frames!");
        }

        private static void SoundLoaderLoadSoundsHook(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
        {
            orig(self);

            // self.soundTriggers[(int)EnumExt_FPBA.Bad_Apple] = new SoundLoader.SoundTrigger(EnumExt_FPBA.Bad_Apple, null, 1.0f, self, new string[] { });
        }

        // ProjectedImage constructor hook for hiding LoadFile() (function cannot be overridden or hidden for ProjectedImage class)
        static void ProjectedImageCtorHook(On.ProjectedImage.orig_ctor orig, ProjectedImage self, List<string> imageNames, int cycleTime)
        {
            //remove LoadFile() call from constructor, so no .PNG file is required
            if (self is ProjectedImageFromMemory)
            {
                self.imageNames = imageNames;
                self.cycleTime = cycleTime;
                self.setAlpha = new float?(1f);
                return;
            }
            orig(self, imageNames, cycleTime);
        }

        // How many frames are displayed each second
        const int FRAME_RATE = 30;

        // How many frames should we wait until the projected image is destroyed?
        // This is necessary to prevent the projected image flickering, and I have zero clue why!
        // Just know that 3 frames delay is the minimum to avoid flicker
        const int IMAGE_DESTRUCTION_FRAME_DELAY = 3;

        // We need to delay the destruction of a projected image's corresponding atlas, otherwise the game will freeze as the image will be missing its texture!
        // We only need to wait a single frame, this ensures that the projected image is destroyed before the atlas
        const int ATLAS_DESTRUCTION_FRAME_DELAY = 1;

        // Five Pebbles' Chamber Bounds
        const int minX = 200;
        const int maxX = 780;
        const int minY = 60;
        const int maxY = 640;

        // Convenient methods for getting the center of the chamber
        public static int midY => minY + ((maxY - minY) / 2);
        public static int midX => minX + ((maxX - minX) / 2);


        // The index of the current frame, and a timer
        private static int currentFrame = 0;
        private static float frameTimer;

        private static Dictionary<int, ProjectedImage> projectedImageBuffer = new Dictionary<int, ProjectedImage>();

        private static bool isVideoPlaying = false;

        private static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

            if (!isVideoPlaying)
            {
                isVideoPlaying = true;
                frameTimer = Time.time;
                self.action = EnumExt_FPBA.Degeneracy_BadApple;

                //self.oracle.room.PlaySound(EnumExt_FPBA.Bad_Apple, self.oracle.firstChunk);
            }

            if (Time.time - frameTimer < 1.0f / FRAME_RATE) return;
            frameTimer = Time.time;

            string frameName = FivePebblesBadApple.frames.ElementAt(currentFrame).Key;
            byte[] textureBytes = FivePebblesBadApple.frames.ElementAt(currentFrame).Value;

            // Declare a Unity Texture2D and write the current byte array to the texture
            Texture2D frameTexture = new Texture2D(0, 0, TextureFormat.RGB24, true);
            frameTexture.LoadImage(textureBytes);

            // Using the atlas we can create a projected image, which is what is used to display images in Pebbles' chamber
            ProjectedImage projectedImage = new ProjectedImageFromMemory(new List<Texture2D> { frameTexture }, new List<string> { frameName }, 0);

            // Add the image to the screen and set its position correctly (the center of the chamber)
            self.oracle.myScreen.room.AddObject(projectedImage);

            // There is also this offset to 'match background', but I am not sure why it is needed: new Vector2(-7.5f, 15.5f);
            projectedImage.pos = new Vector2(midX, midY);


            if (currentFrame - IMAGE_DESTRUCTION_FRAME_DELAY >= 0)
            {
                int targetFrame = currentFrame - IMAGE_DESTRUCTION_FRAME_DELAY;
                self.oracle.myScreen.room.RemoveObject(projectedImageBuffer[targetFrame]);
                FivePebblesBadApple.SELF.Logger_p.LogInfo("Removed Image: " + projectedImageBuffer[targetFrame]);
            }

            if (currentFrame - IMAGE_DESTRUCTION_FRAME_DELAY - ATLAS_DESTRUCTION_FRAME_DELAY >= 0)
            {
                int targetFrame = currentFrame - IMAGE_DESTRUCTION_FRAME_DELAY - ATLAS_DESTRUCTION_FRAME_DELAY;
                Futile.atlasManager.ActuallyUnloadAtlasOrImage(FivePebblesBadApple.frames.ElementAt(targetFrame).Key);
                FivePebblesBadApple.SELF.Logger_p.LogInfo("Removed Atlas: " + FivePebblesBadApple.frames.ElementAt(targetFrame).Key);
            }

            projectedImageBuffer[currentFrame] = projectedImage;
            currentFrame++;

            FivePebblesBadApple.SELF.Logger_p.LogInfo("Displaying Frame: " + frameName);
        }
    }
}
