﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace SAwareness
{
    internal class ImmuneTimer //TODO: Maybe add Packetcheck
    {
        private static Dictionary<Ability, Render.Text> Abilities = new Dictionary<Ability, Render.Text>();
        //private readonly Render.Text _textF = new Render.Text("", 0, 0, 24, SharpDX.Color.Goldenrod);
        //private bool _drawActive = true;

        public ImmuneTimer()
        {
            Ability zhonyaA = new Ability("zhonyas_ring_activate", 2.5f);

            Abilities.Add(new Ability("zhonyas_ring_activate", 2.5f), null); //Zhonya
            Abilities.Add(new Ability("Aatrox_Passive_Death_Activate", 3f), null); //Aatrox Passive
            Abilities.Add(new Ability("LifeAura", 4f), null); //Zil und GA
            Abilities.Add(new Ability("nickoftime_tar", 7f), null); //Zil before death
            Abilities.Add(new Ability("eyeforaneye", 2f), null); // Kayle
            Abilities.Add(new Ability("UndyingRage_buf", 5f), null); //Tryn
            Abilities.Add(new Ability("EggTimer", 6f), null); //Anivia

            foreach(var ability in Abilities)
            {
                Render.Text text = new Render.Text(new Vector2(0,0), "", 28, SharpDX.Color.Goldenrod);
                text.OutLined = true;
                text.Centered = true;
                text.TextUpdate = delegate
                {
                    float endTime = ability.Key.TimeCasted - (int)Game.ClockTime + ability.Key.Delay;
                    var m = (float)Math.Floor(endTime / 60);
                    var s = (float)Math.Ceiling(endTime % 60);
                    return (s < 10 ? m + ":0" + s : m + ":" + s);
                };
                text.PositionUpdate = delegate
                {
                    Vector2 hpPos = new Vector2();
                    if (ability.Key.Target != null)
                    {
                        hpPos = ability.Key.Target.HPBarPosition;
                    }
                    if (ability.Key.Owner != null)
                    {
                        hpPos = ability.Key.Owner.HPBarPosition;
                    }
                    hpPos.X = hpPos.X + 80;
                    return hpPos;
                };
                text.VisibleCondition = delegate
                {
                    return Menu.Timers.GetActive() && Menu.ImmuneTimer.GetActive() && 
                            ability.Key.Casted && ability.Key.TimeCasted > 0;
                };
                text.Add();
                Abilities[ability.Key] = text;
            }

            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Game.OnGameUpdate += Game_OnGameUpdate;
            //Drawing.OnDraw += Drawing_OnDraw;
            //Drawing.OnEndScene += Drawing_OnEndScene;
            //Drawing.OnPreReset += Drawing_OnPreReset;
            //Drawing.OnPostReset += Drawing_OnPostReset;
        }

        ~ImmuneTimer()
        {
            GameObject.OnCreate -= Obj_AI_Base_OnCreate;
            Game.OnGameUpdate -= Game_OnGameUpdate;
            //Drawing.OnDraw -= Drawing_OnDraw;
            //Drawing.OnEndScene -= Drawing_OnEndScene;
            //Drawing.OnPreReset -= Drawing_OnPreReset;
            //Drawing.OnPostReset -= Drawing_OnPostReset;
            Abilities = null;
        }

        public bool IsActive()
        {
            return Menu.Timers.GetActive() && Menu.ImmuneTimer.GetActive();
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;
            foreach (var ability in Abilities)
            {
                if ((ability.Key.TimeCasted + ability.Key.Delay) < Game.ClockTime)
                {
                    ability.Key.Casted = false;
                    ability.Key.TimeCasted = 0;
                }
            }
        }

        /*private void Drawing_OnEndScene(EventArgs args)
        {
            if (!IsActive() || !_drawActive)
                return;

            foreach (Ability ability in Abilities)
            {
                if (ability.Casted && ability.TimeCasted > 0)
                {
                    Vector2 hpPos = new Vector2();
                    if(ability.Target != null && ability.Target.IsValid)
                        hpPos = ability.Target.HPBarPosition;
                    else if(ability.Owner != null && ability.Owner.IsValid)
                        hpPos = ability.Owner.HPBarPosition;
                    float endTime = ability.TimeCasted - (int)Game.ClockTime + ability.Delay;
                    var m = (float)Math.Floor(endTime / 60);
                    var s = (float)Math.Ceiling(endTime % 60);
                    String ms = (s < 10 ? m + ":0" + s : m + ":" + s);

                    _textF.Centered = true;
                    _textF.text = ms;
                    _textF.X = (int)hpPos.X + 80;
                    _textF.Y = (int)hpPos.Y;
                    _textF.OutLined = true;
                    _textF.OnEndScene();
                }
            }
        }*/

        private void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (!hero.IsEnemy)
                {
                    foreach (var ability in Abilities)
                    {
                        if (sender.Name.Contains(ability.Key.SpellName) &&                            
                            /*variable*/ Vector3.Distance(sender.Position, ObjectManager.Player.ServerPosition) <= 4000)
                        {
                            ability.Key.Owner = hero;
                            ability.Key.Casted = true;
                            ability.Key.TimeCasted = (int)Game.ClockTime;
                            if (Vector3.Distance(sender.Position, hero.ServerPosition) <= 100)
                                ability.Key.Target = hero;
                        }
                    }
                }
            }
        }

        /*private void Drawing_OnPostReset(EventArgs args)
        {
            _textF.OnPostReset();
            _drawActive = true;
        }

        private void Drawing_OnPreReset(EventArgs args)
        {
            _textF.OnPreReset();
            _drawActive = false;
        }*/

        public class Ability
        {
            public bool Casted;
            public float Delay;
            public Obj_AI_Hero Owner;
            public int Range;
            public String SpellName;
            public Obj_AI_Hero Target;
            public int TimeCasted;

            public Ability(string spellName, float delay)
            {
                SpellName = spellName;
                Delay = delay;
            }
        }
    }

    public class Timers
    {
        private static readonly Utility.Map GMap = Utility.Map.GetMap();
        private static Inhibitor _inhibitors;
        private static List<Relic> Relics = new List<Relic>();
        private static List<Altar> Altars = new List<Altar>();
        private static List<Health> Healths = new List<Health>();
        private static List<JungleMob> JungleMobs = new List<JungleMob>();
        private static List<JungleCamp> JungleCamps = new List<JungleCamp>();
        private static List<Obj_AI_Minion> JungleMobList = new List<Obj_AI_Minion>();
        private static Dictionary<Obj_AI_Hero, Summoner> Summoners = new Dictionary<Obj_AI_Hero, Summoner>();

        public Timers()
        {
            GameObject.OnCreate += Obj_AI_Base_OnCreate;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            InitJungleMobs();
        }

        ~Timers()
        {
            GameObject.OnCreate -= Obj_AI_Base_OnCreate;
            Game.OnGameUpdate -= Game_OnGameUpdate;
            Game.OnGameProcessPacket -= Game_OnGameProcessPacket;
            
            _inhibitors = null;
            Relics = null;
            Altars = null;
            Healths = null;
            JungleMobs = null;
            JungleCamps = null;
            JungleMobList = null;
            Summoners = null;
        }

        public bool IsActive()
        {
            return Menu.Timers.GetActive();
        }

        private String AlignTime(float endTime)
        {
            if (!float.IsInfinity(endTime) && !float.IsNaN(endTime))
            {
                var m = (float) Math.Floor(endTime/60);
                var s = (float) Math.Ceiling(endTime%60);
                String ms = (s < 10 ? m + ":0" + s : m + ":" + s);
                return ms;
            }
            return "";
        }

        private bool PingAndCall(String text, Vector3 pos, bool call = true, bool ping = true)
        {
            if(ping)
            {
                for (int i = 0; i < Menu.Timers.GetMenuItem("SAwarenessTimersPingTimes").GetValue<Slider>().Value; i++)
                {
                    GamePacket gPacketT;
                    if (Menu.Timers.GetMenuItem("SAwarenessTimersLocalPing").GetValue<bool>())
                    {
                        gPacketT =
                            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(pos[0], pos[1], 0, 0,
                                Packet.PingType.Normal));
                        gPacketT.Process();
                    }
                    else if (!Menu.Timers.GetMenuItem("SAwarenessTimersLocalPing").GetValue<bool>() &&
                             Menu.GlobalSettings.GetMenuItem("SAwarenessGlobalSettingsServerChatPingActive")
                                 .GetValue<bool>())
                    {
                        gPacketT = Packet.C2S.Ping.Encoded(new Packet.C2S.Ping.Struct(pos.X, pos.Y));
                        gPacketT.Send();
                    }
                }
            }
            if(call)
            {
                if (Menu.Timers.GetMenuItem("SAwarenessTimersChatChoice").GetValue<StringList>().SelectedIndex == 1)
                {
                    Game.PrintChat(text);
                }
                else if (Menu.Timers.GetMenuItem("SAwarenessTimersChatChoice").GetValue<StringList>().SelectedIndex == 2 &&
                         Menu.GlobalSettings.GetMenuItem("SAwarenessGlobalSettingsServerChatPingActive").GetValue<bool>())
                {
                    Game.Say(text);
                }
            }
            return true;
        }

        private void Obj_AI_Base_OnCreate(GameObject sender, EventArgs args)
        {
            if (!IsActive())
                return;
            if (sender.IsValid)
            {
                if (Menu.JungleTimer.GetActive())
                {
                    if (sender.Type == GameObjectType.obj_AI_Minion
                        && sender.Team == GameObjectTeam.Neutral)
                    {
                        if (JungleMobs.Any(mob => sender.Name.Contains(mob.Name)))
                        {
                            JungleMobList.Add((Obj_AI_Minion) sender);
                        }
                    }
                }

                if (Menu.RelictTimer.GetActive())
                {
                    foreach (Relic relic in Relics)
                    {
                        if (sender.Name.Contains(relic.ObjectName))
                        {
                            relic.Obj = sender;
                            relic.Locked = false;
                        }
                    }
                }
            }
        }

        public bool IsBigMob(Obj_AI_Minion jungleBigMob)
        {
            foreach (JungleMob jungleMob in JungleMobs)
            {
                if (jungleBigMob.Name.Contains(jungleMob.Name))
                {
                    return jungleMob.Smite;
                }
            }
            return false;
        }

        public bool IsBossMob(Obj_AI_Minion jungleBossMob)
        {
            foreach (JungleMob jungleMob in JungleMobs)
            {
                if (jungleBossMob.SkinName.Contains(jungleMob.Name))
                {
                    return jungleMob.Boss;
                }
            }
            return false;
        }

        public bool HasBuff(Obj_AI_Minion jungleBigMob)
        {
            foreach (JungleMob jungleMob in JungleMobs)
            {
                if (jungleBigMob.SkinName.Contains(jungleMob.Name))
                {
                    return jungleMob.Buff;
                }
            }
            return false;
        }

        private JungleMob GetJungleMobByName(string name, Utility.Map.MapType mapType)
        {
            return JungleMobs.Find(jm => jm.Name == name && jm.MapType == mapType);
        }

        private JungleCamp GetJungleCampByID(int id, Utility.Map.MapType mapType)
        {
            return JungleCamps.Find(jm => jm.CampId == id && jm.MapType == mapType);
        }

        public void InitJungleMobs()
        {
            //All
            //_inhibitors = new Inhibitor("Inhibitor", new[] { "Order_Inhibit_Gem.troy", "Chaos_Inhibit_Gem.troy" }, new[] { "Order_Inhibit_Crystal_Shatter.troy", "Chaos_Inhibit_Crystal_Shatter.troy" });

            //Summoner's Rift
            //JungleMobs.Add(new JungleMob("GreatWraith", null, true, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("AncientGolem", null, true, true, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("GiantWolf", null, true, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("Wraith", null, true, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("LizardElder", null, true, true, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("Golem", null, true, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("Worm", null, true, true, true, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("Dragon", null, true, false, true, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("Wight", null, true, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("YoungLizard", null, false, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("Wolf", null, false, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("LesserWraith", null, false, false, false, Utility.Map.MapType.SummonersRift));
            //JungleMobs.Add(new JungleMob("SmallGolem", null, false, false, false, Utility.Map.MapType.SummonersRift));

            JungleMobs.Add(new JungleMob("SRU_Blue", null, true, true, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_Murkwolf", null, true, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_Razorbeak", null, true, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_Red", null, true, true, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_Krug", null, true, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_BaronSpawn", null, true, true, true, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_Dragon", null, true, false, true, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_Gromp", null, true, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_Crab", null, true, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_RedMini", null, false, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_MurkwolfMini", null, false, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_RazorbeakMini", null, false, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_KrugMini", null, false, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_BlueMini", null, false, false, false, Utility.Map.MapType.SummonersRift));
            JungleMobs.Add(new JungleMob("SRU_BlueMini2", null, false, false, false, Utility.Map.MapType.SummonersRift));

            //Twisted Treeline
            JungleMobs.Add(new JungleMob("TT_NWraith", null, false, false, false, Utility.Map.MapType.TwistedTreeline));
            JungleMobs.Add(new JungleMob("TT_NGolem", null, false, false, false, Utility.Map.MapType.TwistedTreeline));
            JungleMobs.Add(new JungleMob("TT_NWolf", null, false, false, false, Utility.Map.MapType.TwistedTreeline));
            JungleMobs.Add(new JungleMob("TT_Spiderboss", null, true, true, true, Utility.Map.MapType.TwistedTreeline));
            JungleMobs.Add(new JungleMob("TT_Relic", null, false, false, false, Utility.Map.MapType.TwistedTreeline));

            //Altars.Add(new Altar("Left Altar", "TT_Buffplat_L", null, 180, 85, new[] { "TT_Lock_Blue_L.troy", "TT_Lock_Purple_L.troy", "TT_Lock_Neutral_L.troy" }, new[] { "TT_Unlock_Blue_L.troy", "TT_Unlock_purple_L.troy", "TT_Unlock_Neutral_L.troy" }, 1));
            //Altars.Add(new Altar("Right Altar", "TT_Buffplat_R", null, 180, 85, new[] { "TT_Lock_Blue_R.troy", "TT_Lock_Purple_R.troy", "TT_Lock_Neutral_R.troy" }, new[] { "TT_Unlock_Blue_R.troy", "TT_Unlock_purple_R.troy", "TT_Unlock_Neutral_R.troy" }, 1));

            //Crystal Scar
            Relics.Add(new Relic("Relic",
                ObjectManager.Player.Team == GameObjectTeam.Order ? "Odin_Prism_Green.troy" : "Odin_Prism_Red.troy",
                GameObjectTeam.Order, null, 180, 180, new Vector3(5500, 6500, 60), new Vector3(5500, 6500, 60)));
            Relics.Add(new Relic("Relic",
                ObjectManager.Player.Team == GameObjectTeam.Chaos ? "Odin_Prism_Green.troy" : "Odin_Prism_Red.troy",
                GameObjectTeam.Chaos, null, 180, 180, new Vector3(7550, 6500, 60), new Vector3(7550, 6500, 60)));

            //Howling Abyss
            //JungleMobs.Add(new JungleMob("HA_AP_HealthRelic", null, false, false, false, 1));

            JungleCamps.Add(new JungleCamp("blue", GameObjectTeam.Order, 1, 115, 300, Utility.Map.MapType.SummonersRift,
                new Vector3(3570, 7670, 54), new Vector3(3641.058f, 8144.426f, 1105.46f),
                new[]
                {
                    GetJungleMobByName("SRU_Blue", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_BlueMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_BlueMini2", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("wolves", GameObjectTeam.Order, 2, 115, 100, Utility.Map.MapType.SummonersRift,
                new Vector3(3430, 6300, 56), new Vector3(3730.419f, 6744.748f, 1100.24f),
                new[]
                {
                    GetJungleMobByName("SRU_Murkwolf", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_MurkwolfMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_MurkwolfMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("wraiths", GameObjectTeam.Order, 3, 115, 100, Utility.Map.MapType.SummonersRift, 
                new Vector3(6540, 7230, 56), new Vector3(7069.483f, 5800.1f, 1064.815f),
                new[]
                {
                    GetJungleMobByName("SRU_Razorbeak", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RazorbeakMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RazorbeakMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RazorbeakMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("red", GameObjectTeam.Order, 4, 115, 300, Utility.Map.MapType.SummonersRift,
                new Vector3(7370, 3830, 58), new Vector3(7710.639f, 3963.267f, 1200.182f),
                new[]
                {
                    GetJungleMobByName("SRU_Red", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RedMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RedMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("golems", GameObjectTeam.Order, 5, 115, 100, Utility.Map.MapType.SummonersRift,
                new Vector3(7990, 2550, 54), new Vector3(8419.813f, 3239.516f, 1280.222f),
                new[]
                {
                    GetJungleMobByName("SRU_Krug", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_KrugMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("wight", GameObjectTeam.Order, 13, 115, 100, Utility.Map.MapType.SummonersRift,
                new Vector3(1688, 8248, 54), new Vector3(2263.463f, 8571.541f, 1136.772f),
                new[] { GetJungleMobByName("SRU_Gromp", Utility.Map.MapType.SummonersRift) }));
            JungleCamps.Add(new JungleCamp("blue", GameObjectTeam.Chaos, 7, 115, 300, Utility.Map.MapType.SummonersRift,
                new Vector3(10455, 6800, 55), new Vector3(11014.81f, 7251.099f, 1073.918f),
                new[]
                {
                    GetJungleMobByName("SRU_Blue", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_BlueMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_BlueMini2", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("wolves", GameObjectTeam.Chaos, 8, 115, 100, Utility.Map.MapType.SummonersRift,
                new Vector3(10570, 8150, 63), new Vector3(11233.96f, 8789.653f, 1051.235f),
                new[]
                {
                    GetJungleMobByName("SRU_Murkwolf", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_MurkwolfMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_MurkwolfMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("wraiths", GameObjectTeam.Chaos, 9, 115, 100,
                Utility.Map.MapType.SummonersRift, new Vector3(7465, 9220, 56), new Vector3(7962.764f, 10028.573f, 1023.06f),
                new[]
                {
                    GetJungleMobByName("SRU_Razorbeak", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RazorbeakMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RazorbeakMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RazorbeakMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("red", GameObjectTeam.Chaos, 10, 115, 300, Utility.Map.MapType.SummonersRift,
                new Vector3(6620, 10637, 55), new Vector3(7164.198f, 11113.5f, 1093.54f),
                new[]
                {
                    GetJungleMobByName("SRU_Red", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RedMini", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_RedMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("golems", GameObjectTeam.Chaos, 11, 115, 100,
                Utility.Map.MapType.SummonersRift, new Vector3(6010, 11920, 40), new Vector3(6508.562f, 12127.83f, 1185.667f),
                new[]
                {
                    GetJungleMobByName("SRU_Krug", Utility.Map.MapType.SummonersRift),
                    GetJungleMobByName("SRU_KrugMini", Utility.Map.MapType.SummonersRift)
                }));
            JungleCamps.Add(new JungleCamp("wight", GameObjectTeam.Chaos, 14, 115, 100, Utility.Map.MapType.SummonersRift,
                new Vector3(12266, 6215, 54), new Vector3(12671.58f, 6617.756f, 1118.074f),
                new[] { GetJungleMobByName("SRU_Gromp", Utility.Map.MapType.SummonersRift) }));
            JungleCamps.Add(new JungleCamp("crab", GameObjectTeam.Neutral, 15, 2 * 60 + 30, 180, Utility.Map.MapType.SummonersRift,
                new Vector3(12266, 6215, 54), new Vector3(10557.22f, 5481.414f, 1068.042f),
                new[] { GetJungleMobByName("SRU_Crab", Utility.Map.MapType.SummonersRift) }));
            JungleCamps.Add(new JungleCamp("crab", GameObjectTeam.Neutral, 16, 2 * 60 + 30, 180, Utility.Map.MapType.SummonersRift,
                new Vector3(12266, 6215, 54), new Vector3(4535.956f, 10104.067f, 1029.071f),
                new[] { GetJungleMobByName("SRU_Crab", Utility.Map.MapType.SummonersRift) }));
            JungleCamps.Add(new JungleCamp("dragon", GameObjectTeam.Neutral, 6, 2*60 + 30, 360,
                Utility.Map.MapType.SummonersRift, new Vector3(9400, 4130, -61), new Vector3(10109.18f, 4850.93f, 1032.274f),
                new[] {GetJungleMobByName("Dragon", Utility.Map.MapType.SummonersRift)}));
            JungleCamps.Add(new JungleCamp("nashor", GameObjectTeam.Neutral, 12, 20*60, 420,
                Utility.Map.MapType.SummonersRift, new Vector3(4620, 10265, -63), new Vector3(4951.034f, 10831.035f, 1027.482f),
                new[] {GetJungleMobByName("Worm", Utility.Map.MapType.SummonersRift)}));

            JungleCamps.Add(new JungleCamp("wraiths", GameObjectTeam.Order, 1, 100, 50,
                Utility.Map.MapType.TwistedTreeline, new Vector3(4414, 5774, 60), new Vector3(4414, 5774, 60),
                new[]
                {
                    GetJungleMobByName("TT_NWraith", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWraith", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWraith", Utility.Map.MapType.TwistedTreeline)
                }));
            JungleCamps.Add(new JungleCamp("golems", GameObjectTeam.Order, 2, 100, 50,
                Utility.Map.MapType.TwistedTreeline, new Vector3(5088, 8065, 60), new Vector3(5088, 8065, 60),
                new[]
                {
                    GetJungleMobByName("TT_NGolem", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NGolem", Utility.Map.MapType.TwistedTreeline)
                }));
            JungleCamps.Add(new JungleCamp("wolves", GameObjectTeam.Order, 3, 100, 50,
                Utility.Map.MapType.TwistedTreeline, new Vector3(6148, 5993, 60), new Vector3(6148, 5993, 60),
                new[]
                {
                    GetJungleMobByName("TT_NWolf", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWolf", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWolf", Utility.Map.MapType.TwistedTreeline)
                }));
            JungleCamps.Add(new JungleCamp("wraiths", GameObjectTeam.Chaos, 4, 100, 50,
                Utility.Map.MapType.TwistedTreeline, new Vector3(11008, 5775, 60), new Vector3(11008, 5775, 60),
                new[]
                {
                    GetJungleMobByName("TT_NWraith", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWraith", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWraith", Utility.Map.MapType.TwistedTreeline)
                }));
            JungleCamps.Add(new JungleCamp("golems", GameObjectTeam.Chaos, 5, 100, 50,
                Utility.Map.MapType.TwistedTreeline, new Vector3(10341, 8084, 60), new Vector3(10341, 8084, 60),
                new[]
                {
                    GetJungleMobByName("TT_NGolem", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NGolem", Utility.Map.MapType.TwistedTreeline)
                }));
            JungleCamps.Add(new JungleCamp("wolves", GameObjectTeam.Chaos, 6, 100, 50,
                Utility.Map.MapType.TwistedTreeline, new Vector3(9239, 6022, 60), new Vector3(9239, 6022, 60),
                new[]
                {
                    GetJungleMobByName("TT_NWolf", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWolf", Utility.Map.MapType.TwistedTreeline),
                    GetJungleMobByName("TT_NWolf", Utility.Map.MapType.TwistedTreeline)
                }));
            JungleCamps.Add(new JungleCamp("heal", GameObjectTeam.Neutral, 7, 115, 90,
                Utility.Map.MapType.TwistedTreeline, new Vector3(7711, 6722, 60), new Vector3(7711, 6722, 60),
                new[] {GetJungleMobByName("TT_Relic", Utility.Map.MapType.TwistedTreeline)}));
            JungleCamps.Add(new JungleCamp("vilemaw", GameObjectTeam.Neutral, 8, 10*60, 300,
                Utility.Map.MapType.TwistedTreeline, new Vector3(7711, 10080, 60), new Vector3(7711, 10080, 60),
                new[] {GetJungleMobByName("TT_Spiderboss", Utility.Map.MapType.TwistedTreeline)}));

            //JungleCamps.Add(new JungleCamp("heal", GameObjectTeam.Neutral, 1, 190, 40, 3, new Vector3(8922, 7868, 60), new Vector3(8922, 7868, 60), new[] { GetJungleMobByName("HA_AP_HealthRelic", 3) }));
            //JungleCamps.Add(new JungleCamp("heal", GameObjectTeam.Neutral, 2, 190, 40, 3, new Vector3(7473, 6617, 60), new Vector3(7473, 6617, 60), new[] { GetJungleMobByName("HA_AP_HealthRelic", 3) }));
            //JungleCamps.Add(new JungleCamp("heal", GameObjectTeam.Neutral, 3, 190, 40, 3, new Vector3(5929, 5190, 60), new Vector3(5929, 5190, 60), new[] { GetJungleMobByName("HA_AP_HealthRelic", 3) }));
            //JungleCamps.Add(new JungleCamp("heal", GameObjectTeam.Neutral, 4, 190, 40, 3, new Vector3(4751, 3901, 60), new Vector3(4751, 3901, 60), new[] { GetJungleMobByName("HA_AP_HealthRelic", 3) }));

            foreach (GameObject objAiBase in ObjectManager.Get<GameObject>())
            {
                Obj_AI_Base_OnCreate(objAiBase, new EventArgs());
            }

            _inhibitors = new Inhibitor();
            foreach (Obj_BarracksDampener inhib in ObjectManager.Get<Obj_BarracksDampener>())
            {
                _inhibitors.Inhibitors.Add(new Inhibitor(inhib));
            }

            foreach (Obj_AI_Minion objectType in ObjectManager.Get<Obj_AI_Minion>())
            {
                if (objectType.Name.Contains("Health"))
                    Healths.Add(new Health(objectType));
                if (objectType.Name.Contains("Buffplat"))
                {
                    if (objectType.Name.Contains("_L"))
                        Altars.Add(new Altar("Left Altar", objectType));
                    else
                        Altars.Add(new Altar("Right Altar", objectType));
                }
            }

            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.IsEnemy)
                {
                    Summoners.Add(hero, new Summoner());
                }
            }

            //foreach (JungleCamp jungleCamp in JungleCamps) //Game.ClockTime BUGGED
            //{
            //    if (Game.ClockTime > 30) //TODO: Reduce when Game.ClockTime got fixed
            //    {
            //        jungleCamp.NextRespawnTime = 0;
            //    }
            //    int nextRespawnTime = jungleCamp.SpawnTime - (int)Game.ClockTime;
            //    if (nextRespawnTime > 0)
            //    {
            //        jungleCamp.NextRespawnTime = nextRespawnTime;
            //    }
            //}
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
                return;
            if (Menu.JungleTimer.GetActive())
            {
                foreach (JungleCamp jungleCamp in JungleCamps)
                {
                    if ((jungleCamp.NextRespawnTime - (int) Game.ClockTime) < 0)
                    {
                        jungleCamp.NextRespawnTime = 0;
                        jungleCamp.Called = false;
                    }
                }
            }

            if (Menu.AltarTimer.GetActive())
            {
                Altar altarDestroyed = null;
                foreach (Altar altar in Altars)
                {
                    if (altar.Obj.IsValid)
                    {
                        bool hasBuff = false;
                        foreach (BuffInstance buff in altar.Obj.Buffs)
                        {
                            if (buff.Name == "treelinelanternlock")
                            {
                                hasBuff = true;
                                break;
                            }
                        }
                        if (!hasBuff)
                        {
                            altar.Locked = false;
                            altar.NextRespawnTime = 0;
                            altar.Called = false;
                        }
                        else if (hasBuff && altar.Locked == false)
                        {
                            altar.Locked = true;
                            altar.NextRespawnTime = altar.RespawnTime + (int) Game.ClockTime;
                        }
                    }
                    else
                    {
                        if (altar.NextRespawnTime < (int) Game.ClockTime)
                        {
                            altarDestroyed = altar;
                        }
                    }
                }
                if (altarDestroyed != null && Altars.Remove(altarDestroyed))
                {
                }
                foreach (Obj_AI_Minion altar in ObjectManager.Get<Obj_AI_Minion>())
                {
                    Altar nAltar = null;
                    if (altar.Name.Contains("Buffplat"))
                    {
                        Altar health1 = Altars.Find(jm => jm.Obj.NetworkId == altar.NetworkId);
                        if (health1 == null)
                            if (altar.Name.Contains("_L"))
                                nAltar = new Altar("Left Altar", altar);
                            else
                                nAltar = new Altar("Right Altar", altar);
                    }

                    if (nAltar != null)
                        Altars.Add(nAltar);
                }
            }

            if (Menu.RelictTimer.GetActive())
            {
                foreach (Relic relic in Relics)
                {
                    if (!relic.Locked && (relic.Obj != null && (!relic.Obj.IsValid || relic.Obj.IsDead)))
                    {
                        if (Game.ClockTime < relic.SpawnTime)
                        {
                            relic.NextRespawnTime = relic.SpawnTime - (int) Game.ClockTime;
                        }
                        else
                        {
                            relic.NextRespawnTime = relic.RespawnTime + (int) Game.ClockTime;
                        }
                        relic.Locked = true;
                    }
                    if ((relic.NextRespawnTime - (int) Game.ClockTime) < 0)
                    {
                        relic.NextRespawnTime = 0;
                        relic.Called = false;
                    }
                }
            }

            //if (Menu.InhibitorTimer.GetActive())
            //{
            //    if (_inhibitors.Inhibitors == null)
            //        return;
            //    foreach (var inhibitor in _inhibitors.Inhibitors)
            //    {
            //        if (inhibitor.Locked)
            //        {
            //            if (inhibitor.NextRespawnTime < Game.ClockTime)
            //            {
            //                inhibitor.Locked = false;
            //            }
            //        }
            //    }
            //}

            if (Menu.HealthTimer.GetActive())
            {
                Health healthDestroyed = null;
                foreach (Health health in Healths)
                {
                    if (health.Obj.IsValid)
                        if (health.Obj.Health > 0)
                        {
                            health.Locked = false;
                            health.NextRespawnTime = 0;
                            health.Called = false;
                        }
                        else if (health.Obj.Health < 1 && health.Locked == false)
                        {
                            health.Locked = true;
                            health.NextRespawnTime = health.RespawnTime + (int) Game.ClockTime;
                        }
                        else
                        {
                            if (health.NextRespawnTime < (int) Game.ClockTime)
                            {
                                healthDestroyed = health;
                            }
                        }
                }
                if (healthDestroyed != null && Healths.Remove(healthDestroyed))
                {
                }
                foreach (Obj_AI_Minion health in ObjectManager.Get<Obj_AI_Minion>())
                {
                    Health nHealth = null;
                    if (health.Name.Contains("Health"))
                    {
                        Health health1 = Healths.Find(jm => jm.Obj.NetworkId == health.NetworkId);
                        if (health1 == null)
                            nHealth = new Health(health);
                    }

                    if (nHealth != null)
                        Healths.Add(nHealth);
                }
            }

            if (Menu.InhibitorTimer.GetActive())
            {
                if (_inhibitors == null || _inhibitors.Inhibitors == null)
                    return;
                foreach (Inhibitor inhibitor in _inhibitors.Inhibitors)
                {
                    if (inhibitor.Obj.Health > 0)
                    {
                        inhibitor.Locked = false;
                        inhibitor.NextRespawnTime = 0;
                        inhibitor.Called = false;
                    }
                    else if (inhibitor.Obj.Health < 1 && inhibitor.Locked == false)
                    {
                        inhibitor.Locked = true;
                        inhibitor.NextRespawnTime = inhibitor.RespawnTime + (int) Game.ClockTime;
                    }
                }
            }

            /////
             
            if (Menu.JungleTimer.GetActive())
            {
                foreach (JungleCamp jungleCamp in JungleCamps)
                {
                    if (jungleCamp.NextRespawnTime <= 0 || jungleCamp.MapType != GMap._MapType)
                        continue;
                    int time = Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value;
                    if (!jungleCamp.Called && jungleCamp.NextRespawnTime - (int)Game.ClockTime <= time &&
                        jungleCamp.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                    {
                        jungleCamp.Called = true;
                        PingAndCall(jungleCamp.Name + " respawns in " + time + " seconds!", jungleCamp.MinimapPosition);
                    }
                }
            }

            if (Menu.AltarTimer.GetActive())
            {
                foreach (Altar altar in Altars)
                {
                    if (altar.Locked)
                    {
                        if (altar.NextRespawnTime <= 0 || altar.MapType != GMap._MapType)
                            continue;
                        int time = Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value;
                        if (!altar.Called && altar.NextRespawnTime - (int)Game.ClockTime <= time &&
                            altar.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                        {
                            altar.Called = true;
                            PingAndCall(altar.Name + " unlocks in " + time + " seconds!", altar.Obj.ServerPosition);
                        }
                    }
                }
            }

            if (Menu.RelictTimer.GetActive())
            {
                foreach (Relic relic in Relics)
                {
                    if (relic.Locked)
                    {
                        if (relic.NextRespawnTime <= 0 || relic.MapType != GMap._MapType)
                            continue;
                        int time = Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value;
                        if (!relic.Called && relic.NextRespawnTime - (int)Game.ClockTime <= time &&
                            relic.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                        {
                            relic.Called = true;
                            PingAndCall(relic.Name + " respawns in " + time + " seconds!", relic.MinimapPosition);
                        }
                    }
                }
            }

            if (Menu.InhibitorTimer.GetActive())
            {
                if (_inhibitors.Inhibitors == null)
                    return;
                foreach (Inhibitor inhibitor in _inhibitors.Inhibitors)
                {
                    if (inhibitor.Locked)
                    {
                        if (inhibitor.NextRespawnTime <= 0)
                            continue;
                        int time = Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value;
                        if (!inhibitor.Called && inhibitor.NextRespawnTime - (int)Game.ClockTime <= time &&
                            inhibitor.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                        {
                            inhibitor.Called = true;
                            PingAndCall("Inhibitor respawns in " + time + " seconds!", inhibitor.Obj.Position);
                        }
                    }
                }
            }

            if (Menu.HealthTimer.GetActive())
            {
                foreach (Health health in Healths)
                {
                    if (health.Locked)
                    {
                        if (health.NextRespawnTime - (int)Game.ClockTime <= 0 || health.MapType != GMap._MapType)
                            continue;
                        int time = Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value;
                        if (!health.Called && health.NextRespawnTime - (int)Game.ClockTime <= time &&
                            health.NextRespawnTime - (int)Game.ClockTime >= time - 1)
                        {
                            health.Called = true;
                            PingAndCall("Heal respawns in " + time + " seconds!", health.Position);
                        }
                    }
                }
            }

            if (Menu.SummonerTimer.GetActive())
            {
                foreach (var hero in Summoners)
                {
                    Obj_AI_Hero enemy = hero.Key;
                    for (int i = 0; i < enemy.SummonerSpellbook.Spells.Count(); i++)
                    {
                        SpellDataInst spellData = enemy.SummonerSpellbook.Spells[i];
                        if (hero.Value.Called[i])
                        {
                            if (Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value < spellData.CooldownExpires - Game.ClockTime)
                            {
                                hero.Value.Called[i] = false;
                            }
                        }
                        if (!hero.Value.Called[i] && Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value > spellData.CooldownExpires - Game.ClockTime)
                        {
                            hero.Value.Called[i] = true;
                            String text = enemy.ChampionName + " ";
                            switch (spellData.Name.ToLower())
                            {
                                case "summonerbarrier":
                                    text = text + "Barrier";
                                    break;

                                case "summonerboost":
                                    text = text + "Cleanse";
                                    break;

                                case "summonerclairvoyance":
                                    text = text + "Clairvoyance";
                                    break;

                                case "summonerdot":
                                    text = text + "Ignite";
                                    break;

                                case "summonerexhaust":
                                    text = text + "Exhaust";
                                    break;

                                case "summonerflash":
                                    text = text + "Flash";
                                    break;

                                case "summonerhaste":
                                    text = text + "Ghost";
                                    break;

                                case "summonerheal":
                                    text = text + "Heal";
                                    break;

                                case "summonermana":
                                    text = text + "Clarity";
                                    break;

                                case "summonerodingarrison":
                                    text = text + "Garrison";
                                    break;

                                case "summonerrevive":
                                    text = text + "Revive";
                                    break;

                                case "smite":
                                    text = text + "Smite";
                                    break;

                                case "summonerteleport":
                                    text = text + "Teleport";
                                    break;
                            }
                            text = text + " " + Menu.Timers.GetMenuItem("SAwarenessTimersRemindTime").GetValue<Slider>().Value + " sec";
                            PingAndCall(text, new Vector3(), true, false);
                        }
                    }
                }
            }
        }

        private void UpdateCamps(int networkId, int campId, byte emptyType)
        {
            if (emptyType != 3)
            {
                JungleCamp jungleCamp = GetJungleCampByID(campId, GMap._MapType);
                if (jungleCamp != null)
                {
                    jungleCamp.NextRespawnTime = (int) Game.ClockTime + jungleCamp.RespawnTime;
                }
            }
        }

        private void EmptyCamp(BinaryReader b)
        {
            byte[] h = b.ReadBytes(4);
            int nwId = BitConverter.ToInt32(h, 0);

            h = b.ReadBytes(4);
            int cId = BitConverter.ToInt32(h, 0);

            byte emptyType = b.ReadByte();
            UpdateCamps(nwId, cId, emptyType);
        }

        private void Game_OnGameProcessPacket(GamePacketEventArgs args) //TODO: Check if Packet is right
        {
            if (!IsActive())
                return;
            if (!Menu.JungleTimer.GetActive())
                return;
            try
            {
                var stream = new MemoryStream(args.PacketData);
                using (var b = new BinaryReader(stream))
                {
                    int pos = 0;
                    var length = (int) b.BaseStream.Length;
                    while (pos < length)
                    {
                        int v = b.ReadInt32();
                        if (v == 195) //OLD 194
                        {
                            byte[] h = b.ReadBytes(1);
                            EmptyCamp(b);
                        }
                        pos += sizeof (int);
                    }
                }
            }
            catch (EndOfStreamException)
            {
            }
        }

        public class Summoner
        {
            public bool[] Called = new bool[] {true,true};
            public int LastTimeCalled;
        }

        public class Altar
        {
            public bool Called;
            public String[] LockNames;
            public bool Locked;
            public Vector3 MapPosition;
            public Utility.Map.MapType MapType;
            public Vector3 MinimapPosition;
            public String Name;
            public int NextRespawnTime;
            public Obj_AI_Minion Obj;
            public GameObject ObjOld;
            public String ObjectName;
            public int RespawnTime;
            public int SpawnTime;
            public String[] UnlockNames;
            public Render.Text Text;

            public Altar()
            {
                
            }

            public Altar(String name, Obj_AI_Minion obj)
            {
                Name = name;
                Obj = obj;
                SpawnTime = 185;
                RespawnTime = 90;
                Locked = false;
                NextRespawnTime = 0;
                MapType = Utility.Map.MapType.TwistedTreeline;
                Called = false;
                Text = new Render.Text(0, 0, "", 12, new ColorBGRA(Color4.White));
                Text.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                Text.PositionUpdate = delegate
                {
                    if (Obj.ServerPosition.Length().Equals(0.0f))
                        return new Vector2(0, 0);
                    Vector2 sPos = Drawing.WorldToMinimap(Obj.ServerPosition);
                    return new Vector2(sPos.X, sPos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    return Menu.Timers.GetActive() && Menu.JungleTimer.GetActive() && NextRespawnTime > 0 && MapType == GMap._MapType && Text.X != 0;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add(); 
            }
        }

        public class Health
        {
            public bool Called;
            public bool Locked;
            public Utility.Map.MapType MapType;
            public int NextRespawnTime;
            public Obj_AI_Minion Obj;
            public Vector3 Position;
            public int RespawnTime;
            public int SpawnTime;
            public Render.Text Text;

            public Health()
            {
                
            }

            public Health(Obj_AI_Minion obj)
            {
                Obj = obj;
                if (obj != null && obj.IsValid)
                    Position = obj.Position;
                else
                    Position = new Vector3();
                SpawnTime = (int) Game.ClockTime;
                RespawnTime = 40;
                NextRespawnTime = 0;
                Locked = false;
                MapType = Utility.Map.MapType.HowlingAbyss;
                Called = false;
                Text = new Render.Text(0, 0, "", 12, new ColorBGRA(Color4.White));
                Text.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                Text.PositionUpdate = delegate
                {
                    Vector2 sPos = Drawing.WorldToMinimap(Position);
                    return new Vector2(sPos.X, sPos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    return Menu.Timers.GetActive() && Menu.JungleTimer.GetActive() && NextRespawnTime > 0 && MapType == GMap._MapType;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add(); 
            }
        }

        public class Inhibitor
        {
            public bool Called;
            public List<Inhibitor> Inhibitors;
            public bool Locked;
            public int NextRespawnTime;
            public Obj_BarracksDampener Obj;
            public int RespawnTime;
            public int SpawnTime;
            public Render.Text Text;

            public Inhibitor()
            {
                Inhibitors = new List<Inhibitor>();
            }

            public Inhibitor(Obj_BarracksDampener obj)
            {
                Obj = obj;
                SpawnTime = (int) Game.ClockTime;
                RespawnTime = 300;
                NextRespawnTime = 0;
                Locked = false;
                Called = false;
                Text = new Render.Text(0, 0, "", 12, new ColorBGRA(Color4.White));
                Text.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                Text.PositionUpdate = delegate
                {
                    if (Obj.Position.Length().Equals(0.0f))
                        return new Vector2(0, 0);
                    Vector2 sPos = Drawing.WorldToMinimap(Obj.Position);
                    return new Vector2(sPos.X, sPos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    return Menu.Timers.GetActive() && Menu.JungleTimer.GetActive() && NextRespawnTime > 0;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add(); 
            }
        }

        public class JungleCamp
        {
            public bool Called;
            public int CampId;
            public JungleMob[] Creeps;
            public Vector3 MapPosition;
            public Utility.Map.MapType MapType;
            public Vector3 MinimapPosition;
            public String Name;
            public int NextRespawnTime;
            public int RespawnTime;
            public int SpawnTime;
            public GameObjectTeam Team;
            public Render.Text Text;

            public JungleCamp(String name, GameObjectTeam team, int campId, int spawnTime, int respawnTime,
                Utility.Map.MapType mapType, Vector3 mapPosition, Vector3 minimapPosition, JungleMob[] creeps)
            {
                Name = name;
                Team = team;
                CampId = campId;
                SpawnTime = spawnTime;
                RespawnTime = respawnTime;
                MapType = mapType;
                MapPosition = mapPosition;
                MinimapPosition = minimapPosition;
                Creeps = creeps;
                NextRespawnTime = 0;
                Called = false;
                Text = new Render.Text(0, 0, "", 12, new ColorBGRA(Color4.White));
                Text.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                Text.PositionUpdate = delegate
                {
                    Vector2 sPos = Drawing.WorldToMinimap(MinimapPosition);
                    return new Vector2(sPos.X, sPos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    return Menu.Timers.GetActive() && Menu.JungleTimer.GetActive() && NextRespawnTime > 0 && MapType == GMap._MapType;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add();             
            }
        }

        public class JungleMob
        {
            public bool Boss;
            public bool Buff;
            public Utility.Map.MapType MapType;
            public String Name;
            public Obj_AI_Minion Obj;
            public bool Smite;

            public JungleMob(string name, Obj_AI_Minion obj, bool smite, bool buff, bool boss,
                Utility.Map.MapType mapType)
            {
                Name = name;
                Obj = obj;
                Smite = smite;
                Buff = buff;
                Boss = boss;
                MapType = mapType;
            }
        }

        public class Relic
        {
            public bool Called;
            public bool Locked;
            public Vector3 MapPosition;
            public Utility.Map.MapType MapType;
            public Vector3 MinimapPosition;
            public String Name;
            public int NextRespawnTime;
            public GameObject Obj;
            public String ObjectName;
            public int RespawnTime;
            public int SpawnTime;
            public GameObjectTeam Team;
            public Render.Text Text;

            public Relic(string name, String objectName, GameObjectTeam team, Obj_AI_Minion obj, int spawnTime,
                int respawnTime, Vector3 mapPosition, Vector3 minimapPosition)
            {
                Name = name;
                ObjectName = objectName;
                Team = team;
                Obj = obj;
                SpawnTime = spawnTime;
                RespawnTime = respawnTime;
                Locked = false;
                MapPosition = mapPosition;
                MinimapPosition = minimapPosition;
                MapType = Utility.Map.MapType.CrystalScar;
                NextRespawnTime = 0;
                Called = false;
                Text = new Render.Text(0, 0, "", 12, new ColorBGRA(Color4.White));
                Text.TextUpdate = delegate
                {
                    return (NextRespawnTime - (int)Game.ClockTime).ToString();
                };
                Text.PositionUpdate = delegate
                {
                    Vector2 sPos = Drawing.WorldToMinimap(MinimapPosition);
                    return new Vector2(sPos.X, sPos.Y);
                };
                Text.VisibleCondition = sender =>
                {
                    return Menu.Timers.GetActive() && Menu.JungleTimer.GetActive() && NextRespawnTime > 0 && MapType == GMap._MapType;
                };
                Text.OutLined = true;
                Text.Centered = true;
                Text.Add(); 
            }
        }
    }
}
