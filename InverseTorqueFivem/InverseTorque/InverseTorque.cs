using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;


namespace InverseTorque
{
    public class InverseTorque : BaseScript
    {
        public InverseTorque()
        {
            Tick += OnTick;
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);

        }
        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;

            RegisterCommand("ittorque", new Action<int, List<object>, string>((source, args, raw) => {
                if (args.Any())
                {
                    float.TryParse(args.First().ToString(), out MaxTorqueMult);
                    Notify("~b~[InverseTorque]~w~: Max Torque Multiplier set to " + MaxTorqueMult);
                }
            }), false);
            TriggerEvent("chat:addSuggestion", "/ittorque", "Ex: /ittorque 4 - Sets the max multiplier for InverseTorque.", new[]
            {
                new { name="X", help="Decimals like 2.5 are allowed." },
            });
        }


        private async Task OnTick()
        {
            await HandleInverseTorque();
        }


        float MaxTorqueMult = 3f;
        private async Task HandleInverseTorque()
        {
            Vehicle v = Game.Player.Character.CurrentVehicle;
            if (Exists(v) && v.CurrentGear>0 && v.IsOnAllWheels && IsControlPressed(0, (int)Control.VehicleAccelerate))
            {
                Vector3 dir = Vector3.Normalize(v.Velocity);
                Vector3 aim = v.ForwardVector;

                float vDir = GetHeadingFromVector_2d(dir.X, dir.Y);
                float vAim = GetHeadingFromVector_2d(aim.X, aim.Y);
                float rAngle = (float)Math.Round(Math.Abs(AngleBetween(dir, aim)), 3);


                float initialAngle = GetVehicleHandlingFloat(v.Handle, "CHandlingData", "fTractionCurveLateral");
                float mult = (float)Math.Round(map(rAngle, initialAngle * 0.1f, initialAngle, 1f, 1f * v.CurrentGear, true), 2);
                if (mult > MaxTorqueMult) mult = MaxTorqueMult;


                if (mult > 1.0f)
                {
                    v.EngineTorqueMultiplier = mult;
                }


            }

        }


        static public bool Exists(Entity entity)
        {
            return entity != null && entity.Exists();
        }
        public static double AngleBetween(Vector3 vector1, Vector3 vector2)
        {
            double sin = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double cos = vector1.X * vector2.X + vector1.Y * vector2.Y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }

        public static float map(float x, float in_min, float in_max, float out_min, float out_max, bool clamp = false)
        {

            float r = (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
            if (clamp) r = Clamp(r, out_min, out_max);
            return r;
        }
        public static float Clamp(float val, float min, float max)
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
        void Notify(string msg, bool isImportant = false)
        {
            SetTextChatEnabled(false);
            SetNotificationTextEntry("STRING");
            AddTextComponentString(msg);
            DrawNotification(isImportant, false);
        }

    }
}
