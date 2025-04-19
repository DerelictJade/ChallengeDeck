using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using MelonLoader;
using NeonLite;
using UnityEngine;

namespace ChallengeDeck
{
    internal class CustomGhosts
    {
        private static bool _patched = false;

        // Original ghost save
        static readonly MethodInfo ogsc = AccessTools.Method(typeof(GhostRecorder), "SaveCompressed");
        static readonly MethodInfo scprefixmi = typeof(CustomGhosts).GetMethod(nameof(PreSaveCompressed));
        static readonly HarmonyMethod scprefix = new HarmonyMethod(scprefixmi);

        // SaveLevelData postfix method
        static readonly MethodInfo ogsld = AccessTools.Method(typeof(GhostRecorder), "SaveLevelData");
        static readonly MethodInfo sldpostfixmi = typeof(CustomGhosts).GetMethod(nameof(PostSaveLevelData));
        static readonly HarmonyMethod sldpostfix = new HarmonyMethod(sldpostfixmi);

        // LoadLevelDataCompressed prefix, to overwrite ghost loading
        static readonly MethodInfo oglldc = AccessTools.Method(typeof(GhostUtils), "LoadLevelDataCompressed");
        //static readonly MethodInfo oglldc = AccessTools.Method(
        //    typeof(GhostUtils),
        //    "LoadLevelDataCompressed",
        //    new Type[] {typeof(GhostSave).MakeByRefType(), typeof(GhostUtils.GhostType), typeof(ulong), typeof(Action)}
        //);
        static readonly MethodInfo lldcprefixmi = typeof(CustomGhosts).GetMethod(nameof(LoadCustomGhost));
        static readonly HarmonyMethod lldcprefix = new HarmonyMethod(lldcprefixmi);

        // Save those frames
        private static readonly FieldInfo framesField = typeof(GhostRecorder).GetField("m_recordingFrames", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo indexField = typeof(GhostRecorder).GetField("m_recordingIndex", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Patch(bool apply)
        {
            if (_patched == apply)
                return;
            if (apply)
                DoPatch();
            else
                UndoPatch();
            _patched = apply;
        }
        private static void DoPatch()
        {
            ChallengeDeck.Harmony.Patch(ogsc, prefix: scprefix);
            ChallengeDeck.Harmony.Patch(ogsld, postfix: sldpostfix);
            ChallengeDeck.Harmony.Patch(oglldc, prefix: lldcprefix);
        }
        private static void UndoPatch()
        {
            ChallengeDeck.Harmony.Unpatch(ogsc, scprefixmi);
            ChallengeDeck.Harmony.Unpatch(ogsld, sldpostfixmi);
            ChallengeDeck.Harmony.Unpatch(oglldc, lldcprefixmi);
        }
        public static bool PreSaveCompressed() => false; // Prevents regular ghost saving
        public static void PostSaveLevelData()
        {
            SaveCustomGhost();
        }
        private static void SaveCustomGhost(string ghostName = null)
        {
            if (LevelRush.IsLevelRush()) return; // Base game behaviour, no rush ghosts smh

            GhostRecorder recorder = UnityEngine.Object.FindObjectOfType<GhostRecorder>();
            if (recorder == null)
                return;

            if (ghostName == null)
                ghostName = GetCustomGhostName();

            GhostFrame[] recordingFrames = (GhostFrame[])framesField.GetValue(recorder);
            int recordingIndex = (int)indexField.GetValue(recorder);
            string path = GetGhostDirectory() + Path.DirectorySeparatorChar.ToString() + ghostName;

            float totalTime = recordingFrames[recordingIndex - 1].time;
            if (File.Exists(path))
            {
                LoadLevelTotalTimeCompressedCustom(path, delegate (float result)
                {
                    if (result > totalTime) // compare previous ghost time against current run (in a stupid way)
                        SaveCustomGhostInternal(recordingFrames, recordingIndex, path, ghostName, totalTime);
                });
                return;
            }
            SaveCustomGhostInternal(recordingFrames, recordingIndex, path, ghostName, totalTime);
        }
        public static bool LoadCustomGhost(ref GhostSave ghostSave, GhostUtils.GhostType ghostType, Action callback)
        {
            if (ghostType != GhostUtils.GhostType.PersonalGhost)
                return true;

            ghostSave = new GhostSave();
            string text = GetGhostDirectory() + Path.DirectorySeparatorChar.ToString() + GetCustomGhostName();
            string data = "";

            if (File.Exists(text))
                data = File.ReadAllText(text);

            try
            {
                DeserializeLevelDataCompressed(ghostSave, data, callback);
            }
            catch
            {
                File.Delete(text);
            }

            if (callback != null)
            {
                callback();
            }
            return false;
        }
        private static string GetGhostDirectory(LevelData level = null)
        {
            if (level == null)
                level = ChallengeDeck.Game.GetCurrentLevel();
            string path = GhostRecorder.GetCompressedSavePathForLevel(level);
            path = Path.GetDirectoryName(path);
            return path;
        }
        private static string GetCustomGhostName()
        {
            string settingName = ChallengeDeck.Settings.CustomGhostName.Value;
            return settingName + ".phant";
        }
        public static void ValidateGhostName()
        {
            string name = ChallengeDeck.Settings.CustomGhostName.Value;
            if (string.IsNullOrEmpty(name))
                name = "default";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(name
                .Trim()
                .Select(c => invalidChars.Contains(c) ? '_' : c)
                .ToArray());
            name = string.IsNullOrWhiteSpace(sanitized) ? "default" : sanitized;
            ChallengeDeck.Settings.CustomGhostName.Value = name;
        }
        private static void SaveCustomGhostInternal(GhostFrame[] framesToSave, int index, string path, string ghostName, float totalTime)
        {
            GhostUtils.MakeSureFoldersExist();
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("2/");
            stringBuilder.Append("1/"); // Really feels like anything could go in these 2 lines

            stringBuilder.Append(Singleton<Game>.Instance.GetCurrentLevel().levelID + "/");
            stringBuilder.Append(totalTime.ToString("F5", CultureInfo.InvariantCulture.NumberFormat) + "/");
            stringBuilder.Append(0.033333335f.ToString("F5", CultureInfo.InvariantCulture.NumberFormat) + "$");
            framesToSave[0].time = 0f;
            for (int i = 0; i < index; i++)
            {
                GhostFrame ghostFrame = framesToSave[i];
                if (i == 0)
                {
                    stringBuilder.Append(string.Concat(new string[]
                    {
                    "a0b",
                    GhostRecorder.VectorToString(ghostFrame.pos, "F4", 4),
                    "c",
                    ghostFrame.angle.ToString("F4", CultureInfo.InvariantCulture.NumberFormat),
                    "d",
                    ghostFrame.cameraPitch.ToString("F4", CultureInfo.InvariantCulture.NumberFormat)
                    }));
                }
                else
                {
                    GhostFrame ghostFrame2 = framesToSave[i - 1];
                    stringBuilder.Append("|");
                    int num = 2;
                    if (num == 2 || (double)(ghostFrame.time - ghostFrame2.time - 0.033333335f) > 0.002)
                    {
                        if (num == 1)
                        {
                            stringBuilder.Append("a" + GhostRecorder.GetFract(ghostFrame.time - ghostFrame2.time, 3));
                        }
                        else if (num == 2)
                        {
                            int num2 = (int)((ghostFrame.time - ghostFrame2.time) * 10000f);
                            stringBuilder.Append(string.Format("a{0}", num2));
                        }
                    }
                    string text = GhostRecorder.VectorToString(ghostFrame.pos - ghostFrame2.pos, "F3", 3);
                    if (!string.IsNullOrEmpty(text))
                    {
                        stringBuilder.Append("b" + text);
                    }
                    text = GhostRecorder.GetFract(ghostFrame.angle - ghostFrame2.angle, 4);
                    if (!string.IsNullOrEmpty(text))
                    {
                        stringBuilder.Append("c" + text);
                    }
                    text = GhostRecorder.GetFract(ghostFrame.cameraPitch - ghostFrame2.cameraPitch, 4);
                    if (!string.IsNullOrEmpty(text))
                    {
                        stringBuilder.Append("d" + text);
                    }
                    if (ghostFrame.triggerEvent != 0)
                    {
                        stringBuilder.Append(string.Format("e{0}", ghostFrame.triggerEvent));
                    }
                    if (ghostFrame.playShotAnimation)
                    {
                        stringBuilder.Append("f");
                    }
                    if (!ghostFrame.grounded)
                    {
                        stringBuilder.Append("g");
                    }
                    if (ghostFrame.zipLining)
                    {
                        stringBuilder.Append("h");
                    }
                    if (ghostFrame.stomping)
                    {
                        stringBuilder.Append("i");
                    }
                    if (ghostFrame.bulletId != 0)
                    {
                        stringBuilder.Append(string.Format("j{0}", ghostFrame.bulletId));
                    }
                    if (ghostFrame.bulletHitPos != null && ghostFrame.bulletHitPos != Vector3.zero)
                    {
                        stringBuilder.Append("k" + GhostRecorder.VectorToString(ghostFrame.bulletHitPos, "F3", 3));
                    }
                }
            }
            string contents = stringBuilder.ToString();
            File.WriteAllText(path, contents);
        }
        private static bool DeserializeLevelDataCompressed(GhostSave ghostSave, string data, Action callback)
        {
            if (!string.IsNullOrEmpty(data))
            {
                List<GhostFrame> list = new List<GhostFrame>();
                int length = data.IndexOf('$');
                string[] array = data.Substring(0, length).Split(new char[]
                {
                '/'
                });
                data = data.Substring(data.IndexOf('$') + 1);
                string[] array2 = data.Split(new char[]
                {
                '|'
                });
                ghostSave.version = int.Parse(array[0], CultureInfo.InvariantCulture.NumberFormat);
                ghostSave.saveId = ulong.Parse(array[1], CultureInfo.InvariantCulture.NumberFormat);
                ghostSave.levelName = array[2];
                ghostSave.totalTime = float.Parse(array[3], CultureInfo.InvariantCulture.NumberFormat);
                float num = float.Parse(array[4], CultureInfo.InvariantCulture.NumberFormat);
                string text = "";
                for (int i = 0; i < array2.Length; i++)
                {
                    list.Add(new GhostFrame());
                    if (GhostUtils.GetData(array2[i], 'a', ref text))
                    {
                        if (ghostSave.version == 1)
                        {
                            list[i].time = GhostUtils.UndoFracting(text);
                            if (i > 0)
                            {
                                list[i].time += list[i - 1].time;
                            }
                        }
                        else if (ghostSave.version == 2)
                        {
                            if (i > 0)
                            {
                                list[i].time = list[i - 1].time + (float)int.Parse(text, CultureInfo.InvariantCulture.NumberFormat) / 10000f;
                            }
                            else
                            {
                                list[i].time = 0f;
                            }
                        }
                    }
                    else
                    {
                        list[i].time = list[i - 1].time + num;
                    }
                    if (GhostUtils.GetData(array2[i], 'b', ref text))
                    {
                        string[] array3 = text.Split(new char[]
                        {
                        ','
                        });
                        list[i].pos = new Vector3(GhostUtils.UndoFracting(array3[0]), GhostUtils.UndoFracting(array3[1]), GhostUtils.UndoFracting(array3[2]));
                        if (i > 0)
                        {
                            list[i].pos += list[i - 1].pos;
                        }
                    }
                    else
                    {
                        list[i].pos = list[i - 1].pos;
                    }
                    if (GhostUtils.GetData(array2[i], 'c', ref text))
                    {
                        list[i].angle = GhostUtils.UndoFracting(text);
                        if (i > 0)
                        {
                            list[i].angle += list[i - 1].angle;
                        }
                    }
                    else
                    {
                        list[i].angle = list[i - 1].angle;
                    }
                    if (GhostUtils.GetData(array2[i], 'd', ref text))
                    {
                        list[i].cameraPitch = GhostUtils.UndoFracting(text);
                        if (i > 0)
                        {
                            list[i].cameraPitch += list[i - 1].cameraPitch;
                        }
                    }
                    else
                    {
                        list[i].cameraPitch = list[i - 1].cameraPitch;
                    }
                    if (GhostUtils.GetData(array2[i], 'e', ref text))
                    {
                        list[i].triggerEvent = int.Parse(text, CultureInfo.InvariantCulture.NumberFormat);
                    }
                    if (GhostUtils.GetData(array2[i], 'f', ref text))
                    {
                        list[i].playShotAnimation = true;
                    }
                    if (!GhostUtils.GetData(array2[i], 'g', ref text))
                    {
                        list[i].grounded = true;
                    }
                    if (GhostUtils.GetData(array2[i], 'h', ref text))
                    {
                        list[i].zipLining = true;
                    }
                    if (GhostUtils.GetData(array2[i], 'i', ref text))
                    {
                        list[i].stomping = true;
                    }
                    if (GhostUtils.GetData(array2[i], 'j', ref text))
                    {
                        list[i].bulletId = int.Parse(text, CultureInfo.InvariantCulture.NumberFormat);
                    }
                    if (GhostUtils.GetData(array2[i], 'k', ref text))
                    {
                        string[] array4 = text.Split(new char[]
                        {
                        ','
                        });
                        list[i].bulletHitPos = new SerializableVector3(GhostUtils.UndoFracting(array4[0]), GhostUtils.UndoFracting(array4[1]), GhostUtils.UndoFracting(array4[2]));
                    }
                }
                if (ghostSave.version == 2)
                {
                    float totalTime = ghostSave.totalTime;
                    float time = list[list.Count - 1].time;
                    for (int j = 0; j < list.Count; j++)
                    {
                        //list[j].time = GhostUtils.Remap(list[j].time, time, totalTime);
                        list[j].time = ((Func<float, float, float, float>)((value, to1, to2) =>
                            (value - 0f) / (to1 - 0f) * (to2 - 0f) + 0f))(list[j].time, time, totalTime);
                    }
                }
                ghostSave.frames = list.ToArray();
            }
            return true;
        }
        private static void LoadLevelTotalTimeCompressedCustom(string path, Action<float> callback)
        {
            float resultTime = float.MaxValue;
            if (File.Exists(path))
            {
                string fileContent = File.ReadAllText(path);
                resultTime = ParseCompressedGhostTime(fileContent);
            }
            callback?.Invoke(resultTime);
        }
        private static float ParseCompressedGhostTime(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                try
                {
                    string segment = data.Substring(0, data.IndexOf('$'));
                    string[] parts = segment.Split('/');
                    return float.Parse(parts[3], CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"Failed to parse ghost time: {ex.Message}");
                }
            }
            return float.MaxValue;
        }
    }
}
