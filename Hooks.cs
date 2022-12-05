using System;
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

        //private static ProjectedImage image;
        //private static bool isVideoPlaying = false;

        //private static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        //{
        //    orig(self, eu);

        //    if (isVideoPlaying)
        //    {
        //        frameTimer += Time.deltaTime;

        //        if (frameTimer <= 1.0f / FRAME_RATE) return;
        //        frameTimer = 0.0f;

        //        image.Update(eu);
        //        return;
        //    }
        //    isVideoPlaying = true;

        //    image = new ProjectedImage(FivePebblesBadApple.frames, 2);
        //    image.setPos = new Vector2(midX, midY);

        //    self.oracle.myScreen.images.Add(image);
        //    self.oracle.myScreen.room.AddObject(image);

        //}

        const int FRAME_RATE = 5;

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
        private static float frameTimer = 0.0f;

        // The image that was projected last frame
        private static ProjectedImage lastImage;

        private static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

            frameTimer += Time.deltaTime;

            if (frameTimer <= 1.0f / FRAME_RATE) return;
            frameTimer = 0.0f;

            ProjectedImage image = new ProjectedImage(new List<string> { FivePebblesBadApple.frames[currentFrame] }, 0);
            image.setPos = new Vector2(midX, midY);

            if (lastImage != null) self.oracle.myScreen.room.RemoveObject(lastImage);

            self.oracle.myScreen.room.AddObject(image);

            lastImage = image;
            currentFrame++;

            FivePebblesBadApple.SELF.Logger_p.LogInfo(FivePebblesBadApple.frames[currentFrame]);
        }
    }
}
