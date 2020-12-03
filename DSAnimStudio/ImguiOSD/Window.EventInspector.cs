﻿using DSAnimStudio.TaeEditor;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaeParamType = SoulsAssetPipeline.Animation.TAE.Template.ParamType;

namespace DSAnimStudio.ImguiOSD
{
    public abstract partial class Window
    {
        public class EventInspector : Window
        {
            public override string Title => "Event Inspector";
            public override ImGuiWindowFlags Flags => ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize;

            string CurrentUnmappedEventHex = "";

            protected override void PreUpdate()
            {
                IsOpen = true;
            }

            protected override void BuildContents()
            {
                ImGui.SetWindowPos((Tae.ImGuiEventInspectorPos + new System.Numerics.Vector2(0, 12)) * Main.DPIVectorN);
                ImGui.SetWindowSize((Tae.ImGuiEventInspectorSize - new System.Numerics.Vector2(0, 12)) * Main.DPIVectorN);

                if (Tae.SingleEventBoxSelected)
                {
                    var ev = Tae.SelectedEventBox;
                    if (MenuBar.ClickItem(ev.MyEvent.TypeName ?? $"[Event Type {ev.MyEvent.Type}]",
                        shortcut: "(Change)", shortcutColor: Color.Cyan))
                    {
                        Tae.QueuedChangeEventType = true;
                    }
                    ImGui.Separator();

                    int ClampInt(int v, int min, int max)
                    {
                        v = Math.Max(v, min);
                        v = Math.Min(v, max);
                        return v;
                    }

                    if (ev.MyEvent.TypeName != null)
                    {
                        CurrentUnmappedEventHex = "";

                        Dictionary<string, string> event0ParameterMap = null;
                        if (ev.MyEvent.Type == 0)
                            event0ParameterMap = new Dictionary<string, string>();

                        foreach (var p in ev.MyEvent.Parameters.Template)
                        {
                            string parameterName = $"{p.Value.Type} {p.Key}";

                            if (p.Value.ValueToAssert != null)
                                continue;

                            bool intSigned = p.Value.Type == TaeParamType.s8 || p.Value.Type == TaeParamType.s16 || p.Value.Type == TaeParamType.s32;
                            bool intUnsigned = p.Value.Type == TaeParamType.u8 || p.Value.Type == TaeParamType.u16 || p.Value.Type == TaeParamType.u32;
                            bool intHex = p.Value.Type == TaeParamType.x8 || p.Value.Type == TaeParamType.x16 || p.Value.Type == TaeParamType.x32;

                            if (intSigned || intUnsigned || intHex)
                            {
                                int currentVal = Convert.ToInt32(ev.MyEvent.Parameters[p.Key]);

                                int prevVal = currentVal;

                                if (p.Value.EnumEntries != null && p.Value.EnumEntries.Count > 0)
                                {
                                    string[] items = p.Value.EnumEntries.Keys.ToArray();
                                    string[] dispItems = new string[items.Length];
                                    int currentItemIndex = -1;
                                    for (int i = 0; i < items.Length; i++)
                                    {
                                        if (Convert.ToInt32(p.Value.EnumEntries[items[i]]) == currentVal)
                                            currentItemIndex = i;

                                        if (ev.MyEvent.Type == 0 && items[i].Contains("|"))
                                        {
                                            var allArgsSplit = items[i].Split('|').Select(x => x.Trim()).ToList();
                                            dispItems[i] = allArgsSplit[0];
                                            for (int j = 1; j < allArgsSplit.Count; j++)
                                            {
                                                var argSplit = allArgsSplit[j].Split(':').Select(x => x.Trim()).ToList();
                                                var argParameter = argSplit[0];
                                                var argName = argSplit[1];
                                                if (i == currentItemIndex && !event0ParameterMap.ContainsKey(argParameter))
                                                    event0ParameterMap.Add(argParameter, argName);
                                            }
                                        }
                                        else
                                        {
                                            dispItems[i] = items[i];
                                        }
                                    }
                                    ImGui.Combo(parameterName, ref currentItemIndex, dispItems, items.Length);
                                    if (currentItemIndex >= 0 && currentItemIndex < items.Length)
                                        currentVal = Convert.ToInt32(p.Value.EnumEntries[items[currentItemIndex]]);
                                    else
                                        currentVal = 0;
                                }
                                else
                                {
                                    string dispName = parameterName;
                                    bool grayedOut = false;
                                    if (ev.MyEvent.Type == 0)
                                    {
                                        if (event0ParameterMap.ContainsKey(p.Key))
                                            dispName = event0ParameterMap[p.Key];
                                        else
                                            grayedOut = true;
                                    }

                                    if (grayedOut)
                                        Tools.PushGrayedOut();


                                    ImGui.InputInt(dispName, ref currentVal, 1, 5, intHex ? (ImGuiInputTextFlags.CharsHexadecimal |
                                    ImGuiInputTextFlags.CharsUppercase) : ImGuiInputTextFlags.None);

                                    if (grayedOut)
                                        Tools.PopGrayedOut();

                                }


                                if (currentVal < 0 && intUnsigned)
                                {
                                    currentVal = 0;
                                }

                                if (p.Value.Type == TaeParamType.u8)
                                    ev.MyEvent.Parameters[p.Key] = (byte)ClampInt(currentVal, byte.MinValue, byte.MaxValue);
                                else if (p.Value.Type == TaeParamType.s8)
                                    ev.MyEvent.Parameters[p.Key] = (sbyte)ClampInt(currentVal, sbyte.MinValue, sbyte.MaxValue);
                                else if (p.Value.Type == TaeParamType.s16)
                                    ev.MyEvent.Parameters[p.Key] = (short)ClampInt(currentVal, short.MinValue, short.MaxValue);
                                else if (p.Value.Type == TaeParamType.u16)
                                    ev.MyEvent.Parameters[p.Key] = (ushort)ClampInt(currentVal, ushort.MinValue, ushort.MaxValue);
                                else if (p.Value.Type == TaeParamType.s32)
                                    ev.MyEvent.Parameters[p.Key] = (int)ClampInt(currentVal, int.MinValue, int.MaxValue);
                                else if (p.Value.Type == TaeParamType.u32)
                                    ev.MyEvent.Parameters[p.Key] = (uint)ClampInt(currentVal, (int)uint.MinValue, int.MaxValue);

                                if (currentVal != prevVal)
                                {
                                    Tae.SelectedTaeAnim?.SetIsModified(true);
                                }
                            }
                            else if (p.Value.Type == TaeParamType.f32)
                            {
                                string dispName = parameterName;
                                bool grayedOut = false;
                                if (ev.MyEvent.Type == 0)
                                {
                                    if (event0ParameterMap.ContainsKey(p.Key))
                                        dispName = event0ParameterMap[p.Key];
                                    else
                                        grayedOut = true;
                                }

                                float current = Convert.ToSingle(ev.MyEvent.Parameters[p.Key]);
                                float prevValue = current;

                                if (grayedOut)
                                    Tools.PushGrayedOut();

                                ImGui.InputFloat(dispName, ref current);

                                if (grayedOut)
                                    Tools.PopGrayedOut();

                                ev.MyEvent.Parameters[p.Key] = current;

                                if (current != prevValue)
                                {
                                    Tae.SelectedTaeAnim?.SetIsModified(true);
                                }
                            }
                            else if (p.Value.Type == TaeParamType.b)
                            {
                                bool current = Convert.ToBoolean(ev.MyEvent.Parameters[p.Key]);
                                bool prevValue = current;
                                ImGui.Checkbox(parameterName, ref current);
                                ev.MyEvent.Parameters[p.Key] = current;
                                if (current != prevValue)
                                    Tae.SelectedTaeAnim?.SetIsModified(true);
                            }
                            else if (p.Value.Type == TaeParamType.aob)
                            {
                                byte[] buf = (byte[])(ev.MyEvent.Parameters[p.Key]);
                                byte[] newBuf = new byte[buf.Length];
                                string current = string.Join("", buf.Select(bb => bb.ToString("X2")));
                                ImGui.InputText($"{parameterName}[{p.Value.AobLength}]", ref current, (uint)((p.Value.AobLength * 2) - 0), ImGuiInputTextFlags.CharsHexadecimal |
                                    ImGuiInputTextFlags.CharsUppercase);
                                //current = current.Replace(" ", "");
                                if (current.Length < p.Value.AobLength * 2)
                                    current += new string('0', (p.Value.AobLength * 2) - current.Length);
                                bool wasModified = false;
                                for (int i = 0; i < buf.Length; i++)
                                {
                                    newBuf[i] = byte.Parse(current.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                                    if (newBuf[i] != buf[i])
                                        wasModified = true;
                                }
                                ev.MyEvent.Parameters[p.Key] = newBuf;
                                if (wasModified)
                                    Tae.SelectedTaeAnim?.SetIsModified(true);
                            }
                        }
                    }
                    else
                    {
                        CurrentUnmappedEventHex = ""; // temp idk when i'm gonna bother

                        ImGui.Button("Copy to clipboard");
                        if (ImGui.IsItemClicked())
                        {
                            System.Windows.Forms.Clipboard.SetText(string.Join(" ", ev.MyEvent.GetParameterBytes(GameDataManager.IsBigEndianGame).Select(xx => xx.ToString("X2"))));
                        }
                    }

                }

            }
        }
    }
}
