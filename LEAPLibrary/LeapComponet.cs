using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Leap;


namespace LeapLibrary
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class LeapComponet : Microsoft.Xna.Framework.DrawableGameComponent
    {

        //Leap Classes
        XNALeapListener listener;
        Controller controller;

        //internal drawing for debug
        SpriteBatch sb;

        public bool DrawDebug;
        string debugLine;
        public string DebugLine { get { return debugLine; } }

        Texture2D fingerPointTexture;
        List<Vector2> fingerPoints;
        FingerList  fingers;
        public List<Finger> Fingers { get { return fingers.ToList<Finger>(); } }

        GestureList gestures;
        public List<Gesture> Gestures { get { return gestures.ToList<Gesture>(); } }

        //first hand
        Hand hand;
        public Hand FirstHand { get { return hand; } }
        Vector2 firstHandLoc;       //for drawing firsthand

        float width, height;

        public LeapComponet(Game game)
            : base(game)
        {
            // TODO: Construct any child components here

            listener = new XNALeapListener();
            controller = new Controller();

            
            fingerPoints = new List<Vector2>();
            this.DrawDebug = true;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();
            controller.AddListener(listener);
        }

        protected override void LoadContent()
        {
            sb = new SpriteBatch(this.GraphicsDevice);
            width = this.GraphicsDevice.Viewport.Width;
            height = this.GraphicsDevice.Viewport.Height;
            fingerPointTexture = new Texture2D(this.GraphicsDevice, 5, 5);
            Color[] Fdata = new Color[5 * 5];
            for (int i = 0; i < Fdata.Length; ++i) Fdata[i] = Color.White;
            fingerPointTexture.SetData(Fdata);
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            // Remove the sample listener when done
            controller.RemoveListener(listener);
            controller.Dispose();
            base.UnloadContent();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here
            if (controller.IsConnected)
            {
                //clear fingers
                fingerPoints.Clear();
                var frame = controller.Frame();

                debugLine = "";
                
                SafeWriteLine("Frame id: " + frame.Id
                        + ", timestamp: " + frame.Timestamp
                        + ", hands: " + frame.Hands.Count
                        + ", fingers: " + frame.Fingers.Count
                        + ", tools: " + frame.Tools.Count
                        + ", gestures: " + frame.Gestures().Count);
                
                if (!frame.Hands.Empty)
                {
                    // Get the first hand
                    hand = frame.Hands[0];
                    
                    firstHandLoc = new Vector2(NormalizeWidth(hand.SphereCenter.x), NormalizeHeight( hand.SphereCenter.y));
                    // Check if the hand has any fingers
                    fingers = hand.Fingers;
                    if (!fingers.Empty)
                    {
                        // Calculate the hand's average finger tip position
                        Vector avgPos = Vector.Zero;
                        foreach (Finger finger in fingers)
                        {
                            fingerPoints.Add(new Vector2(
                                NormalizeWidth(finger.TipPosition.x),
                                NormalizeHeight(finger.TipPosition.y)
                                )
                            );
                            avgPos += finger.TipPosition;
                        }
                        avgPos /= fingers.Count;
                        
                        SafeWriteLine("Hand has " + fingers.Count
                                    + " fingers, average finger tip position: " + avgPos);
                         
                    }

                    // Get the hand's sphere radius and palm position
                    
                    SafeWriteLine("Hand sphere radius: " + hand.SphereRadius.ToString("n2")
                                + " mm, palm position: " + hand.PalmPosition);
                    
                    // Get the hand's normal vector and direction
                    Vector normal = hand.PalmNormal;
                    Vector direction = hand.Direction;

                    // Calculate the hand's pitch, roll, and yaw angles
                    
                    SafeWriteLine("Hand pitch: " + direction.Pitch * 180.0f / (float)Math.PI + " degrees, "
                                + "roll: " + normal.Roll * 180.0f / (float)Math.PI + " degrees, "
                                + "yaw: " + direction.Yaw * 180.0f / (float)Math.PI + " degrees");
                     
                }

                // Get gestures
                gestures = frame.Gestures();
                Gesture gesture;
                for (int i = 0; i < gestures.Count; i++)
                {
                    gesture = gestures[i];

                    switch (gesture.Type)
                    {
                        case Gesture.GestureType.TYPECIRCLE:
                            CircleGesture circle = new CircleGesture(gesture);

                            // Calculate clock direction using the angle between circle normal and pointable
                            String clockwiseness;
                            if (circle.Pointable.Direction.AngleTo(circle.Normal) <= Math.PI / 4)
                            {
                                //Clockwise if angle is less than 90 degrees
                                clockwiseness = "clockwise";
                            }
                            else
                            {
                                clockwiseness = "counterclockwise";
                            }

                            float sweptAngle = 0;

                            // Calculate angle swept since last frame
                            if (circle.State != Gesture.GestureState.STATESTART)
                            {
                                CircleGesture previousUpdate = new CircleGesture(controller.Frame(1).Gesture(circle.Id));
                                sweptAngle = (circle.Progress - previousUpdate.Progress) * 360;
                            }
                            
                            SafeWriteLine("Circle id: " + circle.Id
                                           + ", " + circle.State
                                           + ", progress: " + circle.Progress
                                           + ", radius: " + circle.Radius
                                           + ", angle: " + sweptAngle
                                           + ", " + clockwiseness);
                             
                            break;
                        case Gesture.GestureType.TYPESWIPE:
                            SwipeGesture swipe = new SwipeGesture(gesture);
                            
                            SafeWriteLine("Swipe id: " + swipe.Id
                                           + ", " + swipe.State
                                           + ", position: " + swipe.Position
                                           + ", direction: " + swipe.Direction
                                           + ", speed: " + swipe.Speed);
                             
                            break;
                        case Gesture.GestureType.TYPEKEYTAP:
                            KeyTapGesture keytap = new KeyTapGesture(gesture);
                            
                            SafeWriteLine("Tap id: " + keytap.Id
                                           + ", " + keytap.State
                                           + ", position: " + keytap.Position
                                           + ", direction: " + keytap.Direction);
                            
                            break;
                        case Gesture.GestureType.TYPESCREENTAP:
                            ScreenTapGesture screentap = new ScreenTapGesture(gesture);
                           
                            SafeWriteLine("Tap id: " + screentap.Id
                                           + ", " + screentap.State
                                           + ", position: " + screentap.Position
                                           + ", direction: " + screentap.Direction);
                             
                            break;
                        default:
                            SafeWriteLine("Unknown gesture type.");
                            break;
                    }
                }

                if (!frame.Hands.Empty || !frame.Gestures().Empty)
                {
                    //SafeWriteLine("");
                }
            }
            base.Update(gameTime);
        }

        private void SafeWriteLine(string p)
        {
            this.debugLine += "\n" + p;
        }

        protected float NormalizeWidth(float f)
        {
            //return f;
            return ( ((f * this.width * 2 )/this.width) + this.width/2); 
        }

        protected float NormalizeHeight(float f)
        {
            return this.height - f;
            //return ((f * -1) * this.width) / this.width;
        }

        public override void Draw(GameTime gameTime)
        {
            if (DrawDebug)
            {
                sb.Begin();
                foreach (Vector2 fingerLoc in fingerPoints)
                {
                    sb.Draw(fingerPointTexture, fingerLoc, Color.White);
                    sb.Draw(fingerPointTexture, firstHandLoc, Color.Red);
                }
                sb.End();
            }
            base.Draw(gameTime);
        }
    
    }

    
}
