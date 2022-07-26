using GTA;
using GTA.Math;
using GTA.Native;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;



public class InverseTorqueSP : Script
{
    string ScriptName = "";
    string ScriptVer = "0.1";
    new ScriptSettings Settings;

    public InverseTorqueSP()
    {
        Tick += OnTick;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        Settings = ScriptSettings.Load(@"scripts\InverseTorque\Options.ini");
        
        TopMult = Settings.GetValue<float>("SETTINGS", "TopMult", 5f);
        DeadZone = Settings.GetValue<float>("SETTINGS", "DeadZone", 5f);

        PowerScaling = Settings.GetValue<float>("SETTINGS", "PowerScaling", 1f);
        GripScaling = Settings.GetValue<float>("SETTINGS", "GripScaling", 1f);
        GearScaling = Settings.GetValue<float>("SETTINGS", "GearScaling", 1f);

    }

    
    float TopMult = 1f;
    float DeadZone = 0f;

    float PowerScaling = 1;
    float GripScaling = 1;
    float GearScaling = 1;
    
    bool InverseTorqueDebug = false;
    bool Active = true;
    void OnTick(object sender, EventArgs e)
    {
        if (WasCheatStringJustEntered("itdebug"))
        {
            if (!Settings.GetValue<bool>("SETTINGS", "Enabled", true)) UI.Notify("~y~Inverse Torque is disabled in Options.ini.");
            else InverseTorqueDebug = !InverseTorqueDebug;
        }
        if (WasCheatStringJustEntered("itscale")|| WasCheatStringJustEntered("itmult") || WasCheatStringJustEntered("ittopmult"))
        {
            if (!Settings.GetValue<bool>("SETTINGS", "Enabled", true)) UI.Notify("~y~Inverse Torque is disabled in Options.ini.");
            else
            {
                UI.Notify("~y~Inverse Torque~w~~n~Current Scaler: ~b~x" + TopMult);
                string m = Game.GetUserInput(5);
                
                if (float.TryParse(m, out TopMult))
                {
                    UI.Notify("~y~Inverse Torque~w~~n~TopMult set: ~b~x" + TopMult + "");
                }
                else UI.Notify("~y~Inverse Torque~w~~n~Invalid value: ~o~" + m);
            }
        }
        if (WasCheatStringJustEntered("itdeadzone"))
        {
            if (!Settings.GetValue<bool>("SETTINGS", "Enabled", true)) UI.Notify("~y~Inverse Torque is disabled in Options.ini.");
            else
            {
                UI.Notify("~y~Inverse Torque~w~~n~Dead Zone: ~b~" + DeadZone+"º");
                string m = Game.GetUserInput(5);
                if (float.TryParse(m, out DeadZone))
                {
                    UI.Notify("~y~Inverse Torque~w~~n~Dead Zone set: ~b~" + DeadZone + "º");
                }
                else UI.Notify("~y~Inverse Torque~w~~n~Invalid value: ~o~" + m);
            }

        }
        if (WasCheatStringJustEntered("itreload")) LoadSettings();
        if(WasCheatStringJustEntered("iton")) { Active = true; UI.Notify("~y~Inverse Torque~w~~n~Active."); };
        if(WasCheatStringJustEntered("itoff")) { Active = false; UI.Notify("~y~Inverse Torque~w~~n~Inactive."); };

        if (!Active) return;

        Vehicle v = Game.Player.Character.CurrentVehicle;
        if (CanWeUse(v)  && v.Driver == Game.Player.Character && v.Model.IsCar &&v.CurrentGear>0) {
            
            float grip= Function.Call<float>((Hash)0xA132FB5370554DB0, v);
            float angle = Math.Abs(Vector3.SignedAngle(v.ForwardVector, v.Velocity.Normalized, v.UpVector));
            float mult = 0f;

            //float maxMult = map(angle, 5f, 90f, 1f, 10f, true);
            //mult = map(angle, 5f, 90f, 1f, Scaler * grip, true);
            
            if (angle > DeadZone)
            {
                //mult = 1 + ((angle-DeadZone) * (Scaler * 0.1f));
                mult = map(angle, DeadZone, 90f, 1f, TopMult  * (1+(PowerScaling * (1+GetPower(v)))) * (1+(GripScaling*grip)) * (1+(GearScaling*v.CurrentGear)), true);
            }
            

            //Reduce when stationary
            mult *= (float)Math.Round(map(v.Velocity.Length(), 1, 5, 0, 1, true), 2);

            if (mult > 1f) v.EngineTorqueMultiplier = mult;

            mult = (float)Math.Round(mult, 2);
            if(InverseTorqueDebug)
            {
                if (mult > 1.0f)
                {
                    UI.ShowSubtitle("Angle: " + Math.Round(angle, 1) + "º ~n~~b~x" + mult.ToString(), 500);
                }
                else
                {
                    UI.ShowSubtitle("Angle: " + Math.Round(angle, 1) + "º ~n~~w~x" + mult.ToString(), 500);
                }
            }
            
        }
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
    
    public float GetPower(Vehicle v)
    {
        if (!CanWeUse(v)) return 0;
        return Function.Call<float>(Hash.GET_VEHICLE_ACCELERATION, v);
    }

    /// TOOLS ///
    void LoadSettings()
    {
        if (File.Exists(@"scripts\\SCRIPTNAME.ini"))
        {

            ScriptSettings config = ScriptSettings.Load(@"scripts\SCRIPTNAME.ini");
            // = config.GetValue<bool>("GENERAL_SETTINGS", "NAME", true);
        }
        else
        {
            WarnPlayer(ScriptName + " " + ScriptVer, "SCRIPT RESET", "~g~Towing Service has been cleaned and reset succesfully.");
        }


        if (!Settings.GetValue<bool>("SETTINGS", "Enabled", true)) Active = false;
    }

    void WarnPlayer(string script_name, string title, string message)
    {
        Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "STRING");
        Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, message);
        Function.Call(Hash._SET_NOTIFICATION_MESSAGE, "CHAR_SOCIAL_CLUB", "CHAR_SOCIAL_CLUB", true, 0, title, "~b~" + script_name);
    }

   public static bool CanWeUse(Entity entity)
    {
        return entity != null && entity.Exists();
    }

    void DisplayHelpTextThisFrame(string text)
    {
        Function.Call(Hash._SET_TEXT_COMPONENT_FORMAT, "STRING");
        Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, text);
        Function.Call(Hash._DISPLAY_HELP_TEXT_FROM_STRING_LABEL, 0, false, true, -1);
    }


    public static bool WasCheatStringJustEntered(string cheat)
    {
        return Function.Call<bool>(Hash._0x557E43C447E700A8, Game.GenerateHash(cheat));
    }

}
