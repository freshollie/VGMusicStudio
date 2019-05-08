﻿using Kermalis.VGMusicStudio.Util;
using System.Collections.Generic;

namespace Kermalis.VGMusicStudio.Core.GBA.M4A
{
    internal class Track
    {
        public readonly byte Index;

        public byte Voice, PitchBendRange, Priority, Volume, Rest, PrevKey,
            LFOPhase, LFODelayCount, LFOSpeed, LFODelay, LFODepth;
        public LFOType LFOType;
        public sbyte PitchBend, Tune, Panpot, Transpose;
        public bool Ready, Stopped;
        public int CurEvent;
        public int[] CallStack = new int[3];
        public byte CallStackDepth;

        public readonly List<Channel> Channels = new List<Channel>();

        public int GetPitch()
        {
            int mod = LFOType == LFOType.Pitch ? (Utils.Tri(LFOPhase) * LFODepth) >> 8 : 0;
            return (PitchBend * PitchBendRange) + Tune + mod;
        }
        public byte GetVolume()
        {
            int mod = LFOType == LFOType.Volume ? (Utils.Tri(LFOPhase) * LFODepth * 3 * Volume) >> 19 : 0;
            return (byte)(Volume + mod).Clamp(0, 0x7F);
        }
        public sbyte GetPanpot()
        {
            int mod = LFOType == LFOType.Panpot ? (Utils.Tri(LFOPhase) * LFODepth * 3) >> 12 : 0;
            return (sbyte)(Panpot + mod).Clamp(-0x40, 0x3F);
        }

        public Track(byte i)
        {
            Index = i;
        }
        public void Init()
        {
            Voice = Priority = PrevKey = Rest = LFODelay = LFODelayCount = LFOPhase = LFODepth = CallStackDepth = 0;
            PitchBend = Tune = Panpot = Transpose = 0;
            CurEvent = 0;
            PitchBendRange = 2;
            LFOType = LFOType.Pitch;
            Ready = Stopped = false;
            LFOSpeed = 22;
            Volume = 100;
            StopAllChannels();
        }
        public void Tick()
        {
            if (Rest != 0)
            {
                Rest--;
            }
            int active = 0;
            Channel[] chans = Channels.ToArray();
            for (int i = 0; i < chans.Length; i++)
            {
                if (chans[i].TickNote())
                {
                    active++;
                }
            }
            if (active != 0)
            {
                if (LFODelayCount > 0)
                {
                    LFODelayCount--;
                    LFOPhase = 0;
                }
                else
                {
                    LFOPhase += LFOSpeed;
                }
            }
            else
            {
                LFOPhase = 0;
                LFODelayCount = LFODelay;
            }
        }

        public void ReleaseChannels(byte key)
        {
            Channel[] chans = Channels.ToArray();
            for (int i = 0; i < chans.Length; i++)
            {
                Channel c = chans[i];
                if (c.Note.OriginalKey == key && c.Note.Duration == -1)
                {
                    c.Release();
                }
            }
        }
        public void StopAllChannels()
        {
            Channel[] chans = Channels.ToArray();
            for (int i = 0; i < chans.Length; i++)
            {
                chans[i].Stop();
            }
        }
        public void UpdateChannels()
        {
            byte vol = GetVolume();
            sbyte pan = GetPanpot();
            int pitch = GetPitch();
            for (int i = 0; i < Channels.Count; i++)
            {
                Channel c = Channels[i];
                c.SetVolume(vol, pan);
                c.SetPitch(pitch);
            }
        }
    }
}
