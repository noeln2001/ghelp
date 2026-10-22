using System.Diagnostics;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using GHelper.Helpers;

namespace GHelper.SpatialAudio
{
    public enum AudioCodecMode
    {
        Stereo20,           // Pure 2.0 Stereo (Lossless Studio / Audiophile)
        DolbySurround51,    // Dolby Digital 5.1 / Dolby Atmos for Gaming
        DtsHeadphoneX,      // DTS Headphone:X / DTS:X Ultra
        WindowsSonic,       // Windows Sonic for Headphones
        Surround71          // 7.1 Virtual Surround
    }

    public enum AudioEqPreset
    {
        FlatHiFi,           // 2.0 Lossless Pure Reference Music
        FpsCompetitive,     // FPS Footsteps & Spatial Positional Cues
        CinematicMovie,     // Deep Sub-Bass & Cinematic Soundstage
        VoiceClarity,       // Vocal Clarity & Discord Speech Boost
        NightMode           // Dynamic Range Compression (Safe Night Volume)
    }

    public class AudioDeviceInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsDefault { get; set; }
        public float Volume { get; set; } = 1.0f;
        public bool IsMuted { get; set; }
        public int Channels { get; set; } = 2;
        public int SampleRate { get; set; } = 48000;
        public int BitsPerSample { get; set; } = 24;
    }

    public static class AudioCodecManager
    {
        // Well-known Windows Spatial Sound & Codec GUIDs
        public const string GUID_DOLBY_ATMOS = "{A304D104-CDA5-48EE-91C7-D57262E0CE01}";
        public const string GUID_DTS_HEADPHONE_X = "{C57B4332-9F08-41C0-A0E0-4D56ECBC0185}";
        public const string GUID_WINDOWS_SONIC = "{E041A1D1-7E4A-443B-B053-C8A4B6B4C46A}";
        public const string GUID_STEREO_OFF = "{00000000-0000-0000-0000-000000000000}";

        private static AudioCodecMode _currentCodecMode = AudioCodecMode.Stereo20;
        private static AudioEqPreset _currentEqPreset = AudioEqPreset.FlatHiFi;

        public static AudioCodecMode CurrentCodecMode => _currentCodecMode;
        public static AudioEqPreset CurrentEqPreset => _currentEqPreset;

        public static List<AudioDeviceInfo> GetPlaybackDevices()
        {
            var list = new List<AudioDeviceInfo>();
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var defaultDev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                string defaultId = defaultDev?.ID ?? "";

                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach (var d in devices)
                {
                    try
                    {
                        var info = new AudioDeviceInfo
                        {
                            Id = d.ID,
                            Name = d.FriendlyName,
                            IsDefault = string.Equals(d.ID, defaultId, StringComparison.OrdinalIgnoreCase),
                            Volume = d.AudioEndpointVolume.MasterVolumeLevelScalar,
                            IsMuted = d.AudioEndpointVolume.Mute
                        };

                        if (d.AudioClient != null)
                        {
                            var fmt = d.AudioClient.MixFormat;
                            info.Channels = fmt.Channels;
                            info.SampleRate = fmt.SampleRate;
                            info.BitsPerSample = fmt.BitsPerSample;
                        }

                        list.Add(info);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AudioCodec] Enumerate playback error: {ex.Message}");
            }
            return list;
        }

        public static List<AudioDeviceInfo> GetRecordingDevices()
        {
            var list = new List<AudioDeviceInfo>();
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var defaultDev = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                string defaultId = defaultDev?.ID ?? "";

                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                foreach (var d in devices)
                {
                    try
                    {
                        list.Add(new AudioDeviceInfo
                        {
                            Id = d.ID,
                            Name = d.FriendlyName,
                            IsDefault = string.Equals(d.ID, defaultId, StringComparison.OrdinalIgnoreCase),
                            Volume = d.AudioEndpointVolume.MasterVolumeLevelScalar,
                            IsMuted = d.AudioEndpointVolume.Mute
                        });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AudioCodec] Enumerate recording error: {ex.Message}");
            }
            return list;
        }

        public static bool SetMasterVolume(float volume)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var dev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (dev != null)
                {
                    dev.AudioEndpointVolume.MasterVolumeLevelScalar = Math.Clamp(volume, 0.0f, 1.0f);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AudioCodec] SetVolume error: {ex.Message}");
            }
            return false;
        }

        public static bool ToggleMute()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var dev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (dev != null)
                {
                    dev.AudioEndpointVolume.Mute = !dev.AudioEndpointVolume.Mute;
                    return dev.AudioEndpointVolume.Mute;
                }
            }
            catch { }
            return false;
        }

        public static bool ToggleMicMute()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var dev = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                if (dev != null)
                {
                    dev.AudioEndpointVolume.Mute = !dev.AudioEndpointVolume.Mute;
                    return dev.AudioEndpointVolume.Mute;
                }
            }
            catch { }
            return false;
        }

        public static bool SetCodecMode(AudioCodecMode mode)
        {
            _currentCodecMode = mode;
            AppConfig.Set("audio_codec_mode", (int)mode);

            string targetGuid = mode switch
            {
                AudioCodecMode.DolbySurround51 => GUID_DOLBY_ATMOS,
                AudioCodecMode.DtsHeadphoneX => GUID_DTS_HEADPHONE_X,
                AudioCodecMode.WindowsSonic => GUID_WINDOWS_SONIC,
                _ => GUID_STEREO_OFF
            };

            Logger.WriteLine($"[AudioCodec] Switching codec mode to {mode} (Target GUID: {targetGuid})");

            try
            {
                using var enumerator = new MMDeviceEnumerator();
                using var dev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (dev != null)
                {
                    ApplySpatialFormatRegistry(dev.ID, targetGuid);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AudioCodec] SetCodecMode error: {ex.Message}");
                return false;
            }
        }

        public static void SetEqPreset(AudioEqPreset preset)
        {
            _currentEqPreset = preset;
            AppConfig.Set("audio_eq_preset", (int)preset);
            Logger.WriteLine($"[AudioCodec] Active sound preset: {preset}");
        }

        private static void ApplySpatialFormatRegistry(string deviceId, string spatialGuid)
        {
            try
            {
                // Registry path for audio endpoint spatial sound policy
                string cleanId = deviceId.Trim('{', '}');
                string keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{cleanId}\Properties";

                using var key = Registry.LocalMachine.OpenSubKey(keyPath, writable: true)
                             ?? Registry.CurrentUser.OpenSubKey(keyPath, writable: true);

                if (key != null)
                {
                    // PKEY_Spatial_Audio_Format {b3f8fa53-0004-438e-9003-51a46e139bfc},3
                    const string pkeySpatial = "{b3f8fa53-0004-438e-9003-51a46e139bfc},3";
                    if (spatialGuid == GUID_STEREO_OFF)
                    {
                        key.DeleteValue(pkeySpatial, false);
                    }
                    else
                    {
                        key.SetValue(pkeySpatial, spatialGuid, RegistryValueKind.String);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"[AudioCodec] Spatial registry update: {ex.Message}");
            }
        }

        public static void OpenSoundControlPanel()
        {
            try { Process.Start("mmsys.cpl"); } catch { }
        }

        public static void OpenVolumeMixer()
        {
            try { Process.Start("sndvol.exe"); } catch { }
        }

        public static void OpenDolbyAccess()
        {
            try
            {
                Process.Start(new ProcessStartInfo("dolbyaccess:") { UseShellExecute = true });
            }
            catch
            {
                try { Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?productid=9nblggh4361q") { UseShellExecute = true }); } catch { }
            }
        }

        public static void OpenDtsUnbound()
        {
            try
            {
                Process.Start(new ProcessStartInfo("dts-sound-unbound:") { UseShellExecute = true });
            }
            catch
            {
                try { Process.Start(new ProcessStartInfo("ms-windows-store://pdp/?productid=9pj0nkl8mcsj") { UseShellExecute = true }); } catch { }
            }
        }
    }
}
