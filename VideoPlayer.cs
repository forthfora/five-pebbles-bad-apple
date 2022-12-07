using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static SlugcatStats;
using static System.Net.Mime.MediaTypeNames;

namespace FivePebblesBadApple
{
    public static class VideoPlayer
    {
        // Toggles frame debug messages to Bepinex's console
        // I have no idea if these affect performance, but better safe than sorry!
        private const bool DEBUG_MESSAGES = false;

        // How long until the video starts playing in seconds from when PlayVideo is called
        private const float START_DELAY = 0.0f;

        // How many frames are displayed each second
        private const int FRAME_RATE = 30;

        // Which frame the video should end on;
        private const int FINAL_FRAME = 6572;

        // How many frames should we wait until the projected image is destroyed?
        // This is necessary to prevent the projected image flickering, and I have zero clue why!
        // Just know that 3 frames delay is the minimum to avoid flicker
        // Also do not set this to a ridiculously high value otherwise you will run out of memory before the video finishes :(
        private const int IMAGE_DESTRUCTION_FRAME_DELAY = 3;

        // We need to delay the destruction of a projected image's corresponding atlas, otherwise the game will freeze as the image will be missing its texture!
        // We only need to wait a single frame, this ensures that the projected image is destroyed before the atlas
        private const int ATLAS_DESTRUCTION_FRAME_DELAY = 1;

        // Five Pebbles' Chamber Bounds
        private const int PROJECTOR_MIN_X = 200;
        private const int PROJECTOR_MAX_X = 780;
        private const int PROJECTOR_MIN_Y = 60;
        private const int PROJECTOR_MAX_Y = 640;

        // We'll offset the images to the left slightly so they display a bit better in the chamber
        private const float PROJECTION_OFFSET_X = 17.5f;

        // Convenient methods for getting the center of the chamber
        public static int projectorMidX => PROJECTOR_MIN_X + ((PROJECTOR_MAX_X - PROJECTOR_MIN_X) / 2);
        public static int projectorMidY => PROJECTOR_MIN_Y + ((PROJECTOR_MAX_Y - PROJECTOR_MIN_Y) / 2);

        private static float? startTimer;

        // The index of the current frame, and a timer
        private static int currentFrame = 0;
        public static float frameTimer;

        private static float missedFramesTimer = 0;

        // Queues are used to delay the deletion of the projected images and the atlases
        private static Queue<ProjectedImage> projectedImageQueue = new Queue<ProjectedImage>();
        private static Queue<string> atlasQueue = new Queue<string>();

        public static bool isVideoStarted = false;
        public static bool isVideoFinished = false;

        private static float fadePalette = 0.0f;
        private static bool isBackgroundBlack = false;

        // Should have used a non-static class to make it easier to reset everything but whatever
        public static void ResetVideo(SSOracleBehavior self)
        {
            isVideoFinished = false;
            isVideoStarted = false;
            isBackgroundBlack = false;

            startTimer = null;
            currentFrame = 0;
            missedFramesTimer = 0;
        }

        public static void PlayVideo(SSOracleBehavior self)
        {
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

                FivePebblesBadApple.SELF.Logger_p.LogInfo("Started playing!");
                self.oracle.room.PlaySound(EnumExt_Snd.BadAppleMusic, self.oracle.firstChunk.pos);
                isVideoStarted = true;
                frameTimer = Time.time;
                GatherPearls(self);
            }

            // Update the pearls every frame while the video is playing
            UpdatePearls();

            // We do this before the frame timer because it must update every frame to override the game's palette changing
            if (isBackgroundBlack)
            {
                FadeToBlack(self);
            }
            else
            {
                FadeToWhite(self);
            }

            // Wait until a frame in time has passed
            if (Time.time - frameTimer < 1.0f / FRAME_RATE) return;

            missedFramesTimer += (Time.time - frameTimer) * FRAME_RATE - 1.0f;

            // Skip frames when necessary to keep in time, i.e. when the missed frames timer hits 1 or above
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

            int blackPixelCount = 0;
            int whitePixelCount = 0;

            const int PIXEL_POLL_INTERVAL = 100;

            // Poll the image for black and white pixels, using a large interval to save performance
            // https://answers.unity.com/questions/1321767/check-if-every-pixel-of-a-texture-is-transparent.html
            for (int x = 0; x < frameTexture.width; x += PIXEL_POLL_INTERVAL)
            {
                for (int y = 0; y < frameTexture.height; y += PIXEL_POLL_INTERVAL)
                {
                    if (frameTexture.GetPixel(x, y).r <= 0.5f)
                    {
                        blackPixelCount += 1;
                    }
                    else
                    {
                        whitePixelCount += 1;
                    }
                }
            }

            // Depending on which is greater, we can fade the background to either dark or light
            isBackgroundBlack = blackPixelCount > whitePixelCount;



            // Using the atlas we can create a projected image, which is what is used to display images in Pebbles' chamber
            ProjectedImage projectedImage = new ProjectedImageFromMemory(new List<Texture2D> { frameTexture }, new List<string> { frameName }, 0);

            // Add the image to the screen and set its position correctly (the center of the chamber)
            self.oracle.myScreen.room.AddObject(projectedImage);

            // There is also this offset to 'match background', but I am not sure why it is needed: new Vector2(-7.5f, 15.5f);
            projectedImage.pos = new Vector2(projectorMidX - PROJECTION_OFFSET_X, projectorMidY);


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

            // Add the image and atlas to the destruction queue
            projectedImageQueue.Enqueue(projectedImage);
            atlasQueue.Enqueue(frameName);

            currentFrame++;

            if (DEBUG_MESSAGES && skippedFrames != 0) FivePebblesBadApple.SELF.Logger_p.LogInfo("Skipped Frames: " + skippedFrames);
            if (DEBUG_MESSAGES) FivePebblesBadApple.SELF.Logger_p.LogInfo("Displaying Frame " + currentFrame + ": " + frameName);
        }

        private static List<PhysicalObject> pearls = new List<PhysicalObject>();

        // Credit to woutkolkman and their FivePebblesPong plugin which showed me that this was even possible!
        public static void GatherPearls(SSOracleBehavior self)
        {
            pearls = new List<PhysicalObject>();

            // Gather pearls from the room
            for (int i = 0; i < self.oracle.room.physicalObjects.Length; i++)
                for (int j = 0; j < self.oracle.room.physicalObjects[i].Count; j++)
                    if (self.oracle.room.physicalObjects[i][j] is PebblesPearl && self.oracle.room.physicalObjects[i][j].grabbedBy.Count <= 0)
                        pearls.Add(self.oracle.room.physicalObjects[i][j]);


            // Prevent Pebbles' normal interruption dialogue when picking up a pearl
            self.pearlPickupReaction = false;
        }

        private const int PEARL_ORBIT_RADIUS = 170;
        private const float PEARL_ORBIT_SPEED = 4.0f;

        private static int pearlUpdateCounter = 0;

        private static List<Vector2> GetPearlPositions()
        {
            List<Vector2> positions = new List<Vector2>();

            // This constructs a circle of pearls using sin(x) cos(y), credit to woutkolkman for the original code!
            for (int i = 0; i < pearls.Count; i++)
            {
                float time = ((i * 2.0f / (pearls.Count - 1)) + pearlUpdateCounter / 2000.0f);
                double formX = Math.Sin(PEARL_ORBIT_SPEED * time);
                double formY = Math.Cos(PEARL_ORBIT_SPEED * time);
                float x = projectorMidX + (PEARL_ORBIT_RADIUS * (float)formX) - PROJECTION_OFFSET_X;
                float y = projectorMidY + (PEARL_ORBIT_RADIUS * (float)formY);
                positions.Add(new Vector2(x, y));
            }

            return positions;
        }

        private const bool PEARL_TELEPORTING = false;
        const float MIN_DAMPING = 1.1f;
        const float MAX_DAMPING = 1.9f;
        const float MULTIPLIER = 1.5f;

        // Thanks to woutkolkman for this pearl motion damping code!
        private static void UpdatePearls()
        {
            List<Vector2> positions = GetPearlPositions();

            for (int i = 0; i < pearls.Count && i < positions.Count; i++)
            {
                // If the distance becomes too small, we snap the pearls to their target position to prevent jitter
                float dist = Vector2.Distance(pearls[i].firstChunk.pos, positions[i]);
                if (dist < 3f || PEARL_TELEPORTING)
                {
                    pearls[i].firstChunk.pos = positions[i];
                    pearls[i].firstChunk.vel = new Vector2();
                    continue;
                }

                // Set the velocity of the pearls, with damping, so they travel to their target position smoothly
                float damping = MAX_DAMPING - dist / 10;
                if (damping < MIN_DAMPING) damping = MIN_DAMPING;
                pearls[i].firstChunk.vel.x /= (damping);
                pearls[i].firstChunk.vel.y /= (damping);
                pearls[i].firstChunk.vel += MULTIPLIER * Custom.DirVec(pearls[i].firstChunk.pos, positions[i]);
            }

            pearlUpdateCounter++;
        }

        private static void FadeToBlack(SSOracleBehavior self)
        {
            // Change Palette
            for (int n = 0; n < self.oracle.room.game.cameras.Length; n++)
            {
                if (self.oracle.room.game.cameras[n].room == self.oracle.room && !self.oracle.room.game.cameras[n].AboutToSwitchRoom)
                {
                    if (fadePalette < 1.0f) fadePalette += 0.05f;
                    if (fadePalette > 1.0f) fadePalette = 1.0f;
                    self.oracle.room.game.cameras[n].ChangeBothPalettes(25, 26, fadePalette);
                }
            }
        }

        private static void FadeToWhite(SSOracleBehavior self)
        {
            // Change Palette
            for (int n = 0; n < self.oracle.room.game.cameras.Length; n++)
            {
                if (self.oracle.room.game.cameras[n].room == self.oracle.room && !self.oracle.room.game.cameras[n].AboutToSwitchRoom)
                {
                    if (fadePalette > 0.0f) fadePalette -= 0.05f;
                    if (fadePalette < 0.0f) fadePalette = 0f;
                    self.oracle.room.game.cameras[n].ChangeBothPalettes(25, 26, fadePalette);
                }
            }
        }
    }
}
