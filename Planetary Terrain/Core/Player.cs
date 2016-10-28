using System;
using SharpDX;
using DInput = SharpDX.DirectInput;
using SharpDX.Mathematics.Interop;

namespace Planetary_Terrain {
    class Player : PhysicsBody, IDisposable {
        public Ship Vehicle;
        public Vector3 CameraEuler;
        public Camera Camera;
        public bool FirstPerson = false;

        public Player() : base(62) {
            Drag = .2;
        }

        public void Teleport(Vector3d pos) {
            if (Vehicle != null)
                Vehicle.Position = pos;
            
            Position = pos;
        }

        public void UpdateInput(UI.InputState InputState, double deltaTime) {
            if (InputState.ks.IsPressed(DInput.Key.F) && !InputState.lastks.IsPressed(DInput.Key.F)) {
                if (Vehicle != null) {
                    Vehicle = null;
                    DisablePhysics = false;
                } else {
                    //TODO: enter vehicle
                }
            }
            if (InputState.ks.IsPressed(DInput.Key.T) && !InputState.lastks.IsPressed(DInput.Key.T)) {
                FirstPerson = !FirstPerson;
                if (FirstPerson)
                    System.Windows.Forms.Cursor.Hide();
                else
                    System.Windows.Forms.Cursor.Show();
            }

            Vector3d move = Vector3.Zero;
            if (InputState.ks.IsPressed(DInput.Key.W))
                move.Z -= -1;
            else if (InputState.ks.IsPressed(DInput.Key.S))
                move.Z += -1;
            if (InputState.ks.IsPressed(DInput.Key.A))
                move.X -= 1;
            else if (InputState.ks.IsPressed(DInput.Key.D))
                move.X += 1;
            if (InputState.ks.IsPressed(DInput.Key.Q))
                move.Y += 1;
            if (InputState.ks.IsPressed(DInput.Key.E))
                move.Y -= 1;

            if (Vehicle != null) {
                // move vehicle
                if (InputState.ks.IsPressed(DInput.Key.LeftShift))
                    Vehicle.Throttle += deltaTime * .5;
                else if (InputState.ks.IsPressed(DInput.Key.LeftControl))
                    Vehicle.Throttle -= deltaTime * .5;
                Vehicle.Throttle = MathTools.Clamp01(Vehicle.Throttle);

                Vehicle.AngularVelocity = Vector3d.Lerp(Vehicle.AngularVelocity, new Vector3d(move.Z, move.X, move.Y) * .03, deltaTime);
            } else {
                CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
                Vector3d d = Vector3d.Normalize(Position - b.Position);
                Vector3d planetUp = Vector3.Up;
                if (b != null) {
                    Rotation = b.OrientationFromDirection(d);
                    planetUp = Rotation.Up;
                    Rotation *= Matrix.RotationAxis(Rotation.Up, CameraEuler.Y);
                }

                // walk around
                move.Y = 0;
                double l = move.Length();
                if (l > 0) {
                    move /= l;
                    Vector3d n = b.GetNormal(d);

                    move = Vector3d.Transform(move, (Matrix3x3)Camera.Rotation);

                    Vector3d delta = move - Velocity;
                    delta *= .2;
                    delta -= planetUp * Vector3d.Dot(delta, n);
                    if (delta.LengthSquared() > .1)
                        AddForce(delta * 3.0 * Mass, Vector3.Zero);
                }

            }

            // Mouse look
            if (FirstPerson)
                CameraEuler += new Vector3(InputState.ms.Y, InputState.ms.X, 0) * .003f;
            else {
                if (InputState.ms.Buttons[0])
                    CameraEuler += new Vector3(InputState.mousePos.Y - InputState.lastMousePos.Y, InputState.mousePos.X - InputState.lastMousePos.X, 0) * .003f;
            }
            CameraEuler.X = MathUtil.Clamp(CameraEuler.X, -MathUtil.PiOverTwo, MathUtil.PiOverTwo);
        }
        
        public override void PostUpdate() {
            if (Vehicle != null) {
                Position = Vehicle.Position;
                Velocity = Vehicle.Velocity;
                AngularVelocity = Vehicle.AngularVelocity;
                Rotation = Vehicle.Rotation;
            }

            Camera.Rotation = Rotation * Matrix.RotationAxis(Rotation.Up, CameraEuler.Y);
            Camera.Rotation *= Matrix.RotationAxis(Camera.Rotation.Right, CameraEuler.X);

            if (FirstPerson)
                Camera.Position = Position + (Vector3d)Rotation.Up * .75;
            else
                Camera.Position = Position + (Vector3d)Camera.Rotation.Forward * 50;

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Camera.Position);
            Vector3d dir = Camera.Position - b.Position;
            double h = dir.Length();
            dir /= h;
            double t = b.GetHeight(dir) + .2;
            if (h < t)
                Camera.Position = b.Position + dir * t;
        }

        public override void Draw(Renderer renderer) {
            if (FirstPerson && Vehicle == null) {
                Shaders.ModelShader.Set(renderer);
                
                Models.GunModel.Draw(renderer,
                    Vector3d.Normalize(Position - StarSystem.ActiveSystem.GetStar().Position),
                    Matrix.Scaling(.02f) * (Matrix.Translation(new Vector3(.15f, -.1f, .2f)) * Camera.Rotation)); // TODO: Rotation is fucked
            }
        }

        public void DrawHUD(Renderer renderer) {
            double v = Velocity.Length();

            float xmid = renderer.ResolutionX * .5f;
            renderer.D2DContext.FillRectangle(
                new RawRectangleF(xmid - 150, 0, xmid + 150, 50),
                renderer.Brushes["White"]);

            renderer.SegoeUI24.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
            renderer.SegoeUI14.TextAlignment = SharpDX.DirectWrite.TextAlignment.Leading;

            renderer.D2DContext.DrawText(Physics.FormatSpeed(v), renderer.SegoeUI24,
                new RawRectangleF(xmid - 130, 0, xmid, 40),
                renderer.Brushes["Black"]);

            CelestialBody b = StarSystem.ActiveSystem.GetNearestBody(Position);
            if (b != null) {
                Vector3d dir = b.Position - Position;
                double h = dir.Length();
                dir /= h;

                renderer.D2DContext.DrawText(Physics.FormatDistance(h - b.Radius), renderer.SegoeUI24,
                    new RawRectangleF(xmid, 0, xmid + 150, 40),
                    renderer.Brushes["Black"]);
                renderer.D2DContext.DrawText("(" + b.Name + ")", renderer.SegoeUI14,
                    new RawRectangleF(xmid, 30, xmid + 150, 50),
                    renderer.Brushes["Black"]);

                if (b is Planet) {
                    #region surface info
                    if (h < b.Radius * 1.2) {
                        double temp = (b as Planet).GetTemperature(dir);
                        double humid = (b as Planet).GetHumidity(dir) * 100;

                        renderer.D2DContext.FillRectangle(
                            new RawRectangleF(xmid + 155, 0, xmid + 260, 80),
                            renderer.Brushes["White"]);

                        renderer.D2DContext.DrawText("Surface: ", renderer.SegoeUI14,
                            new RawRectangleF(xmid + 155, 3, xmid + 240, 10),
                            renderer.Brushes["Black"]);

                        renderer.D2DContext.DrawText(temp.ToString("F1") + "°C", renderer.SegoeUI14,
                            new RawRectangleF(xmid + 165, 15, xmid + 240, 30),
                            renderer.Brushes["Black"]);

                        renderer.D2DContext.DrawText(humid.ToString("F1") + "%", renderer.SegoeUI14,
                            new RawRectangleF(xmid + 165, 30, xmid + 240, 45),
                            renderer.Brushes["Black"]);
                    }
                    #endregion
                    #region atmosphere info
                    Atmosphere a = (b as Planet).Atmosphere;
                    if (a != null && h < a.Radius * 1.5) {
                        double temp;
                        double pressure;
                        double density;
                        double c;
                        a.MeasureProperties(dir, h, out pressure, out density, out temp, out c);
                        if (pressure > .1) {
                            renderer.D2DContext.FillRectangle(
                                new RawRectangleF(xmid - 260, 0, xmid - 155, 80),
                                renderer.Brushes["White"]);

                            renderer.D2DContext.DrawText("Atmosphere: ", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 250, 3, xmid - 155, 10),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText(temp.ToString("F1") + "°C", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 15, xmid - 155, 30),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText(pressure.ToString("F1") + " kPa", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 30, xmid - 155, 45),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText(density.ToString("F1") + " kg/m^3", renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 45, xmid - 155, 60),
                                renderer.Brushes["Black"]);

                            renderer.D2DContext.DrawText("Mach " + (Velocity.Length() / c).ToString("F2"), renderer.SegoeUI14,
                                new RawRectangleF(xmid - 240, 60, xmid - 155, 75),
                                renderer.Brushes["Black"]);
                        }
                    }
                    #endregion
                }
            }
        }

        public void Dispose() {
            Vehicle?.Dispose();
            Vehicle = null;
        }
    }
}
