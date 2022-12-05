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

            //  On.RainWorldGame.ContinuePaused += ContinuePausedHook;

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

        //private static void ContinuePausedHook(On.RainWorldGame.orig_ContinuePaused orig, RainWorldGame self)
        //{
        //    orig(self);
        //    frameTimer = Time.time;
        //}

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

        // Toggles frame debug messages to Bepinex's console
        // I have no idea if these affect performance, but better safe than sorry!
        const bool DEBUG_MESSAGES = true;

        // How long until the video starts playing in seconds from when slugcat enters the room
        const float START_DELAY = 3.0f;

        // How many frames are displayed each second
        const int FRAME_RATE = 30;

        // Which frame the video should end on;
        const int FINAL_FRAME = 6572;

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

        private static float? startTimer;

        // The index of the current frame, and a timer
        private static int currentFrame = 0;
        private static float frameTimer;

        private static float missedFramesTimer;

        private static Queue<ProjectedImage> projectedImageQueue = new Queue<ProjectedImage>();
        private static Queue<string> atlasQueue = new Queue<string>();

        private static bool isVideoStarted = false;
        private static bool isVideoFinished = false;

        private static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

            if (isVideoFinished) return;

            if (!isVideoStarted)
            {
                if (startTimer == null)
                {
                    startTimer = Time.time;
                    FivePebblesBadApple.SELF.Logger_p.LogInfo("Waiting for start delay...");
                }

                // Wait for start delay
                if (Time.time - startTimer <= START_DELAY) return;

                //for (int n = 0; n < self.oracle.room.game.cameras.Length; n++)
                //{
                //    if (self.oracle.room.game.cameras[n].room == self.oracle.room && !self.oracle.room.game.cameras[n].AboutToSwitchRoom)
                //    {
                //        self.oracle.room.game.cameras[n].ChangeBothPalettes(26, 25, self.working);
                //    }
                //}
                FivePebblesBadApple.SELF.Logger_p.LogInfo("Started playing!");
                isVideoStarted = true;
                frameTimer = Time.time;
                self.action = EnumExt_FPBA.Degeneracy_BadApple;

                //self.oracle.room.PlaySound(EnumExt_FPBA.Bad_Apple, self.oracle.firstChunk);
            }
            else
            {
                // Wait until a frame in time has passed
                if (Time.time - frameTimer < 1.0f / FRAME_RATE) return;
            }

            missedFramesTimer += (Time.time - frameTimer) * FRAME_RATE - 1.0f;

            // Skip frames when necessary to keep in time
            int skippedFrames = (int)missedFramesTimer;
            currentFrame += skippedFrames;
            missedFramesTimer -= skippedFrames;

            // Stop the video when we pass the final frame
            if (currentFrame > FINAL_FRAME)
            {
                isVideoFinished = true;

                while (projectedImageQueue.Count > 0)
                {
                    FivePebblesBadApple.SELF.Logger_p.LogInfo("Video has ended! Removing Final Images: " + projectedImageQueue.Peek());
                    self.oracle.myScreen.room.RemoveObject(projectedImageQueue.Dequeue());
                }
                return;
            }
            frameTimer = Time.time;

            string frameName = FivePebblesBadApple.frames.ElementAt(currentFrame).Key;
            byte[] textureBytes = FivePebblesBadApple.frames.ElementAt(currentFrame).Value;

            if (DEBUG_MESSAGES) FivePebblesBadApple.SELF.Logger_p.LogInfo("Current Frame: " + currentFrame);

            // Declare a Unity Texture2D and write the current byte array to the texture
            Texture2D frameTexture = new Texture2D(0, 0, TextureFormat.RGB24, true);
            frameTexture.LoadImage(textureBytes);

            // Using the atlas we can create a projected image, which is what is used to display images in Pebbles' chamber
            ProjectedImage projectedImage = new ProjectedImageFromMemory(new List<Texture2D> { frameTexture }, new List<string> { frameName }, 0);

            // Add the image to the screen and set its position correctly (the center of the chamber)
            self.oracle.myScreen.room.AddObject(projectedImage);

            // There is also this offset to 'match background', but I am not sure why it is needed: new Vector2(-7.5f, 15.5f);
            // We'll offset the images to the left slightly so they display a bit better in the chamber
            projectedImage.pos = new Vector2(midX - 17.5f, midY);


            if (projectedImageQueue.Count > IMAGE_DESTRUCTION_FRAME_DELAY)
            {
                if (DEBUG_MESSAGES) FivePebblesBadApple.SELF.Logger_p.LogInfo("Removed Image: " + projectedImageQueue.Peek());
                self.oracle.myScreen.room.RemoveObject(projectedImageQueue.Dequeue());
            }

            if (atlasQueue.Count > ATLAS_DESTRUCTION_FRAME_DELAY + IMAGE_DESTRUCTION_FRAME_DELAY)
            {
                if (DEBUG_MESSAGES) FivePebblesBadApple.SELF.Logger_p.LogInfo("Removed Atlas: " + atlasQueue.Peek());
                Futile.atlasManager.ActuallyUnloadAtlasOrImage(atlasQueue.Dequeue());
            }

            projectedImageQueue.Enqueue(projectedImage);
            atlasQueue.Enqueue(frameName);
            currentFrame++;

            if (skippedFrames != 0) FivePebblesBadApple.SELF.Logger_p.LogInfo("Skipped Frames: " + skippedFrames);
            if (DEBUG_MESSAGES) FivePebblesBadApple.SELF.Logger_p.LogInfo("Displaying Frame " + currentFrame + ": " + frameName);
        }
    }
}
