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
        //Initial values
        float MaxTorqueMult = 2.75f;
        bool LaunchControl = false;



        public InverseTorque()
        {
            Tick += OnTick;
            EventHandlers["onClientResourceStart"] += new Action<string>(OnClientResourceStart);
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (GetCurrentResourceName() != resourceName) return;


            //Client commands to edit IT
            RegisterCommand("itlaunch", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (args.Any())
                {
                    if (args.First().ToString() == "on") LaunchControl = true;
                    if (args.First().ToString() == "off") LaunchControl = false;
                }
                else
                {
                    LaunchControl = !LaunchControl;
                }

                if (LaunchControl) Notify("~b~[InverseTorque]~w~: LaunchControl is ~g~on."); else Notify("~b~[InverseTorque]~w~: LaunchControl is ~o~off.");

            }), false);
            RegisterCommand("itscale", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (args.Any())
                {
                    float.TryParse(args.First().ToString(), out MaxTorqueMult);
                    Notify("~b~[InverseTorque]~w~: Torque Multiplier set to ~g~" + MaxTorqueMult);
                }
            }), false);

            //Suggestions for the commands
            TriggerEvent("chat:addSuggestion", "/itscale", "Ex: /itscale 4 - Sets the multiplier at 90º for InverseTorque.", new[]
            {
                new { name="X", help="Decimals like 2.5 are allowed." },
            });
            TriggerEvent("chat:addSuggestion", "/itlaunch", "Ex: /itlaunch on/off - Toggles experimental launch control.", new[]
            {
                new { name="X", help="on / off" },
            });


        }


        private async Task OnTick()
        {
            await HandleInverseTorque();
        }


        private async Task HandleInverseTorque()
        {
            Vehicle v = Game.Player.Character.CurrentVehicle;
            if (Exists(v) && v.CurrentGear > 0)
            {
                Vector3 dir = Vector3.Normalize(v.Velocity);
                Vector3 aim = v.ForwardVector;

                float rAngle = (float)Math.Round(Math.Abs(AngleBetween(dir, aim)), 3);


                float penalty = map(v.Velocity.Length(), 0, 4, 0, 1, true);
                float grip = GetVehicleMaxTraction(v.Handle);

                float mult = 1f;
                if (GetEntitySpeedVector(v.Handle, true).Y > 0f) mult = (float)Math.Round(map(rAngle, 5f, 90f, 1f, (MaxTorqueMult * grip) * penalty, true), 2);
                else mult = (float)Math.Round(map(rAngle, 180f, 90f, 1f, (MaxTorqueMult * grip) * penalty, true), 2);

                if (mult > 1.0f) v.EngineTorqueMultiplier = mult;
                else if (rAngle < 5f && LaunchControl)
                {
                    float rwd = 1 - GetVehicleHandlingFloat(v.Handle, "CHandlingData", "fDriveBiasFront");

                    float gripPerWheel = grip / GetVehicleNumberOfWheels(v.Handle);
                    float force = GetVehicleAcceleration(v.Handle);
                    int gear = GetVehicleCurrentGear(v.Handle);

                    if (gear == 1) force *= 3.33f;

                    if (rwd > 0.5f) gripPerWheel *= map(rwd, 1, 0.5f, 2, 4, true); else map(rwd, 0, 0.5f, 2, 4, true);
                    float percent = ((gripPerWheel) / force) * 100;

                    mult = (float)Math.Round((percent - 10) / 100, 2);
                    if (mult < 1.0f)
                    {
                        v.EngineTorqueMultiplier = mult;
                    }
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
        static public void DisplayHelpTextTimed(string text, int time)
        {
            SetTextChatEnabled(false);
            BeginTextCommandDisplayHelp("STRING");
            AddTextComponentString(text);
            DisplayHelpTextFromStringLabel(0, false, true, time);
        }

    }
}
