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

            // When the game is unpaused
            On.RainWorldGame.ContinuePaused += ContinuePausedHook;

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

        // We need to know when the game is unpaused to account for the passage of time while the video isn't being played, due to how the frameskip works
        // Otherwise the video will skip forward when we unpause, as if the video wasn't paused in the first place!
        private static void ContinuePausedHook(On.RainWorldGame.orig_ContinuePaused orig, RainWorldGame self)
        {
            orig(self);
            VideoPlayer.frameTimer = Time.time;
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

        private static void SSOracleBehaviorUpdateHook(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);
            VideoPlayer.PlayVideo(self);

            if (VideoPlayer.isVideoStarted && !VideoPlayer.isVideoFinished) VideoPlayer.UpdatePearls();
        }
    }
}
