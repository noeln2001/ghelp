using System.Runtime.InteropServices;
using GHelper.Helpers;

namespace GHelper.Input
{
    public static class MacroStudio
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        private static bool _rapidFireActive = false;
        private static System.Threading.Timer? _rapidFireTimer;
        private static int _cps = 15; // Clicks per second

        public static bool IsRapidFireActive => _rapidFireActive;
        public static int TargetCPS
        {
            get => _cps;
            set => _cps = Math.Clamp(value, 1, 50);
        }

        public static void ToggleRapidFire()
        {
            SetRapidFire(!_rapidFireActive);
        }

        public static void SetRapidFire(bool enable)
        {
            _rapidFireActive = enable;
            AppConfig.Set("rapid_fire_active", enable ? 1 : 0);
            Logger.WriteLine($"[MacroStudio] Rapid Fire Clicker set to: {enable} ({_cps} CPS)");

            if (enable)
            {
                int intervalMs = Math.Max(10, 1000 / _cps);
                _rapidFireTimer = new System.Threading.Timer(_ =>
                {
                    if (_rapidFireActive)
                    {
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        Thread.Sleep(5);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    }
                }, null, 0, intervalMs);

                Program.toast?.RunToast($"Rapid Fire ON ({_cps} CPS)", ToastIcon.Charger);
            }
            else
            {
                _rapidFireTimer?.Dispose();
                _rapidFireTimer = null;
                Program.toast?.RunToast("Rapid Fire OFF", ToastIcon.BrightnessDown);
            }
        }
    }
}
