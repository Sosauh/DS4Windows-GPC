using Gpc.Engine.Runtime;

namespace DS4Windows.DS4Control.GpcScripting
{
    /// <summary>Converts between DS4Windows' DS4State and the GPC engine's slots (GPC units).</summary>
    public static class GpcStateBridge
    {
        /// <summary>0..255 axis (128 centre) to -100..100.</summary>
        public static int AxisToGpc(byte raw)
        {
            int v = raw - 128;
            int scaled = v < 0 ? v * 100 / 128 : v * 100 / 127;
            return scaled < -100 ? -100 : scaled > 100 ? 100 : scaled;
        }

        public static byte AxisFromGpc(int v)
        {
            if (v < -100) v = -100;
            else if (v > 100) v = 100;
            return (byte)(v < 0 ? 128 + v * 128 / 100 : 128 + v * 127 / 100);
        }

        static int Btn(bool b) => b ? 100 : 0;

        static byte TriggerFromGpc(int v) => (byte)((v < 0 ? 0 : v > 100 ? 100 : v) * 255 / 100);

        public static void WriteInput(DS4State s, System.Span<int> slots)
        {
            slots.Clear();
            slots[Identifiers.LX] = AxisToGpc(s.LX);
            slots[Identifiers.LY] = AxisToGpc(s.LY);
            slots[Identifiers.RX] = AxisToGpc(s.RX);
            slots[Identifiers.RY] = AxisToGpc(s.RY);
            slots[Identifiers.L2] = s.L2 * 100 / 255;
            slots[Identifiers.R2] = s.R2 * 100 / 255;
            slots[Identifiers.PS] = Btn(s.PS);
            slots[Identifiers.SHARE] = Btn(s.Share);
            slots[Identifiers.OPTIONS] = Btn(s.Options);
            slots[Identifiers.R1] = Btn(s.R1);
            slots[Identifiers.R3] = Btn(s.R3);
            slots[Identifiers.L1] = Btn(s.L1);
            slots[Identifiers.L3] = Btn(s.L3);
            slots[Identifiers.UP] = Btn(s.DpadUp);
            slots[Identifiers.DOWN] = Btn(s.DpadDown);
            slots[Identifiers.LEFT] = Btn(s.DpadLeft);
            slots[Identifiers.RIGHT] = Btn(s.DpadRight);
            slots[Identifiers.TRIANGLE] = Btn(s.Triangle);
            slots[Identifiers.CIRCLE] = Btn(s.Circle);
            slots[Identifiers.CROSS] = Btn(s.Cross);
            slots[Identifiers.SQUARE] = Btn(s.Square);
            slots[Identifiers.TOUCH] = Btn(s.TouchButton);
            slots[Identifiers.MUTE] = Btn(s.Mute);
        }

        /// <summary>Applies the script's report onto <paramref name="dest"/>, which already holds a copy of the input (motion, touch and battery stay as they were).</summary>
        public static void ReadOutput(System.ReadOnlySpan<int> o, DS4State dest)
        {
            dest.LX = AxisFromGpc(o[Identifiers.LX]);
            dest.LY = AxisFromGpc(o[Identifiers.LY]);
            dest.RX = AxisFromGpc(o[Identifiers.RX]);
            dest.RY = AxisFromGpc(o[Identifiers.RY]);
            dest.L2 = TriggerFromGpc(o[Identifiers.L2]);
            dest.R2 = TriggerFromGpc(o[Identifiers.R2]);
            dest.L2Btn = dest.L2 > 0;
            dest.R2Btn = dest.R2 > 0;
            dest.PS = o[Identifiers.PS] > 0;
            dest.Share = o[Identifiers.SHARE] > 0;
            dest.Options = o[Identifiers.OPTIONS] > 0;
            dest.R1 = o[Identifiers.R1] > 0;
            dest.R3 = o[Identifiers.R3] > 0;
            dest.L1 = o[Identifiers.L1] > 0;
            dest.L3 = o[Identifiers.L3] > 0;
            dest.DpadUp = o[Identifiers.UP] > 0;
            dest.DpadDown = o[Identifiers.DOWN] > 0;
            dest.DpadLeft = o[Identifiers.LEFT] > 0;
            dest.DpadRight = o[Identifiers.RIGHT] > 0;
            dest.Triangle = o[Identifiers.TRIANGLE] > 0;
            dest.Circle = o[Identifiers.CIRCLE] > 0;
            dest.Cross = o[Identifiers.CROSS] > 0;
            dest.Square = o[Identifiers.SQUARE] > 0;
            dest.TouchButton = o[Identifiers.TOUCH] > 0;
            dest.OutputTouchButton = dest.TouchButton;
            dest.Mute = o[Identifiers.MUTE] > 0;
        }
    }
}
