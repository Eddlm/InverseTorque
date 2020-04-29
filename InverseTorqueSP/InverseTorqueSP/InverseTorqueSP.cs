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
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        Settings = ScriptSettings.Load(@"scripts\InverseTorque\Options.ini");
        Scaler = Settings.GetValue<float>("SETTINGS", "Scaler", 2f);

        //UI.Notify("Scaler: " + Scaler);
    }


    float Scaler = 1f;
    bool InverseTorqueDebug = false;
    void OnTick(object sender, EventArgs e)
    {
        if (WasCheatStringJustEntered("itdebug"))
        {
            if (!Settings.GetValue<bool>("SETTINGS", "Enabled", true)) UI.Notify("~y~Inverse Torque is disabled in Options.ini.");
            else InverseTorqueDebug = !InverseTorqueDebug;
        }
        if (WasCheatStringJustEntered("itscale"))
        {
            if (!Settings.GetValue<bool>("SETTINGS", "Enabled", true)) UI.Notify("~y~Inverse Torque is disabled in Options.ini.");
            else
            {
                UI.Notify("~y~Inverse Torque~w~~n~Current Scaler: ~b~x" + Scaler);
                string m = Game.GetUserInput(5);
                if (float.TryParse(m, out Scaler))
                {
                    UI.Notify("~y~Inverse Torque~w~~n~Scaler set: ~b~x" + Scaler + " ~w~ at fTractionCurveLateral");
                }
            }

        }

        if (!Settings.GetValue<bool>("SETTINGS", "Enabled", true)) return;

        Vehicle v = Game.Player.Character.CurrentVehicle;
        if (CanWeUse(v)  && v.Driver == Game.Player.Character && v.Model.IsCar) {
            float trcurve = rad2deg(GetTRCurveLat(v));
            float angle = Vector3.Angle(v.ForwardVector, v.Velocity.Normalized);
            float mult = (float)Math.Round(map(angle, trcurve*0.1f, trcurve*0.5f, 1f, Scaler, true), 2);

            if (mult > 1f) v.EngineTorqueMultiplier = mult; 
            if(InverseTorqueDebug) if(mult>1.0f) UI.ShowSubtitle("Angle: "+Math.Round(angle,1)+ "º /"+trcurve+"º~n~~b~x" + mult.ToString(), 500); else UI.ShowSubtitle("Angle: " + Math.Round(angle, 1) + "º /" + trcurve + "º~n~~w~x" + mult.ToString(), 500);

        }
    }


    public static unsafe ulong GetHandlingPtr(Vehicle v)
    {
        if (!CanWeUse(v)) return (ulong)0;

        var address = (ulong)v.MemoryAddress;
        ulong offset = 0x918;
        return *((ulong*)(address + offset));
    }
    public static float rad2deg(float rad)
    {
        return (rad * (180.0f / (float)Math.PI)); //3.14159265358979323846264338327950288f));
    }

    public static unsafe float GetTRCurveLat(Vehicle v)
    {

        if (!CanWeUse(v)) return 0f;
        ulong handlingAddress = GetHandlingPtr(v);
        ulong tractionCurveMaxOffset = 0x0098;
        if (handlingAddress < 1) return 0f;
        float result = *(float*)(handlingAddress + tractionCurveMaxOffset);
        return result;
    }
    void OnKeyDown(object sender, KeyEventArgs e)
    {

    }
    void OnKeyUp(object sender, KeyEventArgs e)
    {

    }
    protected override void Dispose(bool dispose)
    {


        base.Dispose(dispose);
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
    
    

    public unsafe static byte* FindPattern(string pattern, string mask)
    {
        ProcessModule module = Process.GetCurrentProcess().MainModule;

        ulong address = (ulong)module.BaseAddress.ToInt64();
        ulong endAddress = address + (ulong)module.ModuleMemorySize;

        for (; address < endAddress; address++)
        {
            for (int i = 0; i < pattern.Length; i++)
            {
                if (mask[i] != '?' && ((byte*)address)[i] != pattern[i])
                {
                    break;
                }
                else if (i + 1 == pattern.Length)
                {
                    return (byte*)address;
                }
            }
        }

        return null;
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
