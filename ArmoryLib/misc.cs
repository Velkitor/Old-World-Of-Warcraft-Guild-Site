using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Web;
using System.Threading;
using System.IO;
using System.Data;
using System.Data.Sql;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Data.SqlClient;

namespace ArmoryLib
{


    /*
     * =======================================================
     * Character sheet related classes
     * =======================================================
    */
    public class TallentSpec
    {
        public byte[] trees { protected set; get; }
        public string TreeDistribution
        {
            get
            {
                if (trees == null || trees.Length < 3) return "0/0/0";
                else
                    return trees[0] + "/" + trees[1] + "/" + trees[2];
            }
        }
        public string pTreeName { protected set; get; }
        string iconName;
        public string SpecIcon
        {
            get
            {
                return "<img src=\"" + RequestXml.baseUrl + "/wow-icons/_images/43x43/" + iconName + ".png\">";
            }
        }

        public TallentSpec(string name, byte t1, byte t2, byte t3, string icon)
        {
            this.pTreeName = name;
            this.trees = new byte[] { t1, t2, t3 };
            this.iconName = icon;
        }

        // /wow-icons/_images/43x43/inv_shield_06.png
    }
    /// <summary>
    /// Mana/Rage/Energy/Runic Power etc.
    /// </summary>
    public class BaseStats
    {
        #region Stat Classes
        #region str
        public class atribStr
        {
            //Strength section
            public int      baseStr         { protected set; get; }
            public int      effectiveStr    { protected set; get; }
            public int      atkFromStr      { protected set; get; }
            public int      blockFromStr    { protected set; get; }

            public atribStr(int Base, int Effective, int Atk, int Block)
            {
                this.baseStr = Base;
                this.effectiveStr = Effective;
                this.atkFromStr = Atk;
                this.blockFromStr = Block;
            }
        }
        #endregion
        #region agi
        public class atribAgi
        {
            //Agility section
            public int      baseAgi         { protected set; get; }
            public int      effectiveAgi    { protected set; get; }
            public int      armorFromAgi    { protected set; get; }
            public int      atkFromAgi      { protected set; get; }
            public float    critFromAgi     { protected set; get; }

            public atribAgi(int Base, int Effective, int Atk, int Armor, float Crit)
            {
                this.baseAgi = Base;
                this.effectiveAgi = Effective;
                this.atkFromAgi = Atk;
                this.armorFromAgi = Armor;
                this.critFromAgi = Crit;
            }
        }
        #endregion
        #region sta
        public class atribSta
        {
            //Stamina section
            public int      baseSta         { protected set; get; }
            public int      effectiveSta    { protected set; get; }
            public int      hpFromSta       { protected set; get; }
            public int      petBonusStam    { protected set; get; }

            public atribSta(int Base, int Effective, int HP, int PetBonus)
            {
                this.baseSta        = Base;
                this.effectiveSta   = Effective;
                this.hpFromSta      = HP;
                this.petBonusStam   = PetBonus;
            }
        }
        #endregion
        #region int
        public class atribInt
        {
            //Intellect section
            public int      baseInt         { protected set; get; }
            public int      effectiveInt    { protected set; get; }
            public int      mpFromInt       { protected set; get; }
            public int      petBonusInt     { protected set; get; }
            public float    critFromInt     { protected set; get; }

            public atribInt(int Base, int Effective, int MP, int PetBonus, float Crit)
            {
                this.baseInt        = Base;
                this.effectiveInt   = Effective;
                this.mpFromInt      = MP;
                this.petBonusInt    = PetBonus;
                this.critFromInt    = Crit;
            }
        }
        #endregion
        #region spr
        public class atribSpir
        {
            //Spirit section
            public int      baseSpir        { protected set; get; }
            public int      effectiveSpir   { protected set; get; }
            public int      hpRegenFromSpir { protected set; get; }
            public int      mpRegenFromSpir { protected set; get; }

            public atribSpir(int Base, int Effective, int regenHP, int regenMP)
            {
                this.baseSpir           = Base;
                this.effectiveSpir      = Effective;
                this.hpRegenFromSpir    = regenHP;
                this.mpRegenFromSpir    = regenMP;
            }
        }
        #endregion
        #region armor
        public class atribArmor
        {
            //Armor section
            public int      baseArmor       { protected set; get; }
            public int      effectiveArmor  { protected set; get; }
            public int      petArmorBonus   { protected set; get; }
            public float    armorMitigation { protected set; get; }

            public atribArmor(int Base, int Effective, int PetBonus, float mitigation)
            {
                this.baseArmor          = Base;
                this.effectiveArmor     = Effective;
                this.petArmorBonus      = PetBonus;
                this.armorMitigation    = mitigation;
            }
        }
        #endregion 
        #endregion

        public int          maxHP   { protected set; get; }
        public atribStr     str     { protected set; get; }
        public atribAgi     agi     { protected set; get; }
        public atribSta     sta     { protected set; get; }
        public atribInt     intel   { protected set; get; }
        public atribSpir    spir    { protected set; get; }
        public atribArmor   armor   { protected set; get; }
        public SecondBar    _2ndBar { protected set; get; }
        //Raw Data
        public BaseStats(int MaxHp, atribStr Str, atribAgi Agi, atribSta Sta, atribInt Intel, atribSpir Spir, atribArmor Armor, SecondBar secondBar)
        {
            this.maxHP = MaxHp;
            this.str = Str;
            this.agi = Agi;
            this.sta = Sta;
            this.intel = Intel;
            this.spir = Spir;
            this.armor = Armor;
            this._2ndBar = secondBar;
        }
        //Xml Import
        public BaseStats(XmlNode baseStats, int maxHP, XmlNode secondBar)
        {
            this.maxHP = maxHP;
            #region BaseStat Classes
            foreach (XmlNode xNode in baseStats.ChildNodes)
            {
                switch (xNode.Name)
                {
                    case "strength":
                        FillStrengthAttributes(xNode);
                        break;
                    case "agility":
                        FillAgilityAttributes(xNode);
                        break;
                    case "stamina":
                        FillStaminaAttributes(xNode);
                        break;
                    case "intellect":
                        FillIntellectAttributes(xNode);
                        break;
                    case "spirit":
                        FillSpiritAttributes(xNode);
                        break;
                    case "armor":
                        FillArmorAttributes(xNode);
                        break;
                }
            } 
            #endregion
            FillSecondBar(secondBar);
        }
        private void FillStrengthAttributes     (XmlNode str)
        {
            int baseStr, effectiveStr, atkFromStr, blockFromStr;
            try
            {
                baseStr = Convert.ToInt32(str.Attributes["base"].Value);
            }
            catch { baseStr = 0; }
            try
            {
                effectiveStr = Convert.ToInt32(str.Attributes["effective"].Value);
            }
            catch { effectiveStr = 0; }
            try
            {
                atkFromStr = Convert.ToInt32(str.Attributes["attack"].Value);
            }
            catch { atkFromStr = 0; }
            try
            {
                blockFromStr = Convert.ToInt32(str.Attributes["block"].Value);
            }
            catch { blockFromStr = 0; }
            this.str = new atribStr(baseStr, effectiveStr, atkFromStr, blockFromStr);
        }
        private void FillAgilityAttributes      (XmlNode agi)
        {
            int baseAgi, effectiveAgi, atkFromAgi, armorFromAgi;
            float critFromAgi;
            try
            {
                baseAgi = Convert.ToInt32(agi.Attributes["base"].Value);
            }
            catch { baseAgi = 0; }
            try
            {
                effectiveAgi = Convert.ToInt32(agi.Attributes["effective"].Value);
            }
            catch { effectiveAgi = 0; }
            try
            {
                atkFromAgi = Convert.ToInt32(agi.Attributes["attack"].Value);
            }
            catch { atkFromAgi = 0; }
            try
            {
                armorFromAgi = Convert.ToInt32(agi.Attributes["armor"].Value);
            }
            catch { armorFromAgi = 0; }
            try
            {
                critFromAgi = Convert.ToSingle(agi.Attributes["critHitPercent"].Value);
            }
            catch { critFromAgi = 0; }
            this.agi = new atribAgi(baseAgi, effectiveAgi, atkFromAgi, armorFromAgi, critFromAgi);
        }
        private void FillStaminaAttributes      (XmlNode sta)
        {
            int baseSta, effectiveSta, hpFromSta, petBonusStam;
            try
            {
                baseSta = Convert.ToInt32(sta.Attributes["base"].Value);
            }
            catch { baseSta = 0; }
            try
            {
                effectiveSta = Convert.ToInt32(sta.Attributes["effective"].Value);
            }
            catch { effectiveSta = 0; }
            try
            {
                hpFromSta = Convert.ToInt32(sta.Attributes["health"].Value);
            }
            catch { hpFromSta = 0; }
            try
            {
                petBonusStam = Convert.ToInt32(sta.Attributes["petBonus"].Value);
            }
            catch { petBonusStam = 0; }
            this.sta = new atribSta(baseSta, effectiveSta, hpFromSta, petBonusStam);
        }
        private void FillIntellectAttributes    (XmlNode intel)
        {
            int baseInt, effectiveInt, mpFromInt, petBonusInt;
            float critFromInt;
            try
            {
                baseInt = Convert.ToInt32(intel.Attributes["base"].Value);
            }
            catch { baseInt = 0; }
            try
            {
                effectiveInt = Convert.ToInt32(intel.Attributes["effective"].Value);
            }
            catch { effectiveInt = 0; }
            try
            {
                mpFromInt = Convert.ToInt32(intel.Attributes["mana"].Value);
            }
            catch { mpFromInt = 0; }
            try
            {
                petBonusInt = Convert.ToInt32(intel.Attributes["petBonus"].Value);
            }
            catch { petBonusInt = 0; }
            try
            {
                critFromInt = Convert.ToSingle(intel.Attributes["critHitPercent"].Value);
            }
            catch { critFromInt = 0; }
            this.intel = new atribInt(baseInt, effectiveInt, mpFromInt, petBonusInt, critFromInt);
        }
        private void FillSpiritAttributes       (XmlNode spir)
        {
            int baseSpir, effectiveSpir, hpRegenFromSpir, mpRegenFromSpir;
            try
            {
                baseSpir = Convert.ToInt32(spir.Attributes["base"].Value);
            }
            catch { baseSpir = 0; }
            try
            {
                effectiveSpir = Convert.ToInt32(spir.Attributes["effective"].Value);
            }
            catch { effectiveSpir = 0; }
            try
            {
                hpRegenFromSpir = Convert.ToInt32(spir.Attributes["healthRegen"].Value);
            }
            catch { hpRegenFromSpir = 0; }
            try
            {
                mpRegenFromSpir = Convert.ToInt32(spir.Attributes["manaRegen"].Value);
            }
            catch { mpRegenFromSpir = 0; }
            this.spir = new atribSpir(baseSpir, effectiveSpir, hpRegenFromSpir, mpRegenFromSpir);
        }
        private void FillArmorAttributes        (XmlNode armor)
        {
            int baseArmor, effectiveArmor,petArmorBonus;
            float armorMitigation;
            try
            {
                baseArmor = Convert.ToInt32(armor.Attributes["base"].Value);
            }
            catch { baseArmor = 0; }
            try
            {
                effectiveArmor = Convert.ToInt32(armor.Attributes["effective"].Value);
            }
            catch { effectiveArmor = 0; }
            try
            {
                petArmorBonus = Convert.ToInt32(armor.Attributes["petBonus"].Value);
            }
            catch { petArmorBonus = 0; }
            try
            {
                armorMitigation = Convert.ToSingle(armor.Attributes["percent"].Value);
            }
            catch { armorMitigation = 0; }
            this.armor = new atribArmor(baseArmor,effectiveArmor,petArmorBonus,armorMitigation);
        }
        private void FillSecondBar              (XmlNode secondBar)
        {
            this._2ndBar = new SecondBar(secondBar);
        }
    }
    public class SecondBar
    {
        public int effective { protected set; get; }
        public int casting { protected set; get; }
        public int notCasting { protected set; get; }
        public int perFive { protected set; get; }
        public string Casting
        {
            get
            {
                if (casting < 1) return "N/A";
                else return casting.ToString();
            }
        }
        public string NotCasting
        {
            get
            {
                if (notCasting < 1) return "N/A";
                else return notCasting.ToString();
            }
        }
        public string PerFive
        {
            get
            {
                if (perFive < 1) return "N/A";
                else return perFive.ToString();
            }
        }
        public char type { protected set; get; }
        public string Type
        {
            get
            {
                switch (type)
                {
                    case 'e':
                        return "Energy";
                    case 'r':
                        return "Rage";
                    case 'm':
                        return "Mana";
                    case 'p':
                        return "Runic Power";
                    default:
                        return "Unknown";
                }
            }
        }

        public SecondBar(XmlNode secondBar)
        {
            try
            {
                this.type = secondBar.Attributes["type"].Value[0];
            }
            catch { type = '?'; }
            try
            {
                this.effective = Convert.ToInt32(secondBar.Attributes["effective"].Value);
            }
            catch { }
            try
            {
                this.casting = Convert.ToInt32(secondBar.Attributes["casting"].Value);
            }
            catch { }
            try
            {
                this.notCasting = Convert.ToInt32(secondBar.Attributes["notCasting"].Value);
            }
            catch { }
            try
            {
                this.perFive = Convert.ToInt32(secondBar.Attributes["perFive"].Value);
            }
            catch { }
        }
        public SecondBar(int Effective, int Casting, int NotCasting, int PerFive, char Type)
        {
            this.effective = Effective;
            this.casting = Casting;
            this.notCasting = NotCasting;
            this.perFive = PerFive;
            this.type = Type;
        }
    }
    public class Resist
    {
        public int value { protected set; get; }
        protected int petValue;
        public string PetValue
        {
            get
            {
                if (petValue < 0) return "N/A";
                else return petValue.ToString();
            }
        }
        public bool HasPetValue
        {
            get
            {
                return (petValue >= 0);
            }
        }

        public Resist(XmlNode r)
        {
            try
            {
                value = Convert.ToInt32(r.Attributes["value"].Value);
            }
            catch
            {
                value = 0;
            }
            try
            {
                petValue = Convert.ToInt32(r.Attributes["petBonus"].Value);
            }
            catch
            {
                petValue = -1;
            }
        }
    }
    public class Resistances
    {
        public Resist arcane { protected set; get; }
        public Resist fire { protected set; get; }
        public Resist frost { protected set; get; }
        public Resist holy { protected set; get; }
        public Resist nature { protected set; get; }
        public Resist shadow { protected set; get; }

        public Resistances(XmlNode resistances)
        {
            foreach (XmlNode xNode in resistances.ChildNodes)
            {
                switch (xNode.Name)
                {
                    case "arcane":
                        arcane = new Resist(xNode);
                        break;
                    case "fire":
                        fire = new Resist(xNode);
                        break;
                    case "frost":
                        frost = new Resist(xNode);
                        break;
                    case "holy":
                        holy = new Resist(xNode);
                        break;
                    case "nature":
                        nature = new Resist(xNode);
                        break;
                    case "shadow":
                        shadow = new Resist(xNode);
                        break;
                }
            }
        }
    }
    public class Melee
    {
        //TODO: Fill this class
        public Melee(XmlNode melee)
        {
        }
    }
    public class Ranged
    {
        //TODO: Fill this class
        public Ranged(XmlNode ranged)
        {
        }
    }
    public class Spell
    {
        public class SpellAttibute
        {
            public int dmg { protected set; get; }
            public float crit { protected set; get; }

            public SpellAttibute(int d, float c)
            {
                dmg = d; crit = c;
            }
        }
        public class SpellStats
        {
            public float hitPct { protected set; get; }
            public int hitRating { protected set; get; }
            public int penetration { protected set; get; }
            public int reducedResist { protected set; get; }
            public float manaCasting { protected set; get; }
            public float manaNotCasting{protected set; get;}
            public float haste { protected set; get; }
            public int hasteRating { protected set; get; }

            public SpellStats(float HitPct,int HitRating, int Penetration,int ReducedResist, float ManaCasting, float ManaNotCasting, float Haste, int HasteRating)
            {
                this.hitPct = HitPct;
                this.hitRating = HitRating;
                this.reducedResist = ReducedResist;
                this.manaCasting = ManaCasting;
                this.manaNotCasting = ManaNotCasting;
                this.haste = Haste;
                this.hasteRating = HasteRating;
            }

        }
        public SpellAttibute arcane { protected set; get; }
        public SpellAttibute fire { protected set; get; }
        public SpellAttibute frost { protected set; get; }
        public SpellAttibute holy { protected set; get; }
        public SpellAttibute nature { protected set; get; }
        public SpellAttibute shadow { protected set; get; }
        public int healing { protected set; get; }
        public SpellStats stats { protected set; get; }

        public Spell(SpellAttibute Arcane, SpellAttibute Fire, SpellAttibute Frost, SpellAttibute Holy, SpellAttibute Nature, SpellAttibute Shadow, int Healing, SpellStats Stats)
        {
            this.arcane = Arcane;
            this.fire = Fire;
            this.frost = Frost;
            this.holy = Holy;
            this.nature = Nature;
            this.shadow = Shadow;
            this.healing = Healing;
            this.stats = Stats;
        }
        public Spell(XmlNode spell)
        {
            int dArcane = 0, dFire = 0, dFrost = 0, dHoly = 0, dNature = 0, dShadow = 0;
            float cArcane = 0, cFire = 0, cFrost = 0, cHoly = 0, cNature = 0, cShadow = 0;
            float hitPct =0, manaCasting=0, manaNotCasting=0, haste =0;
            int hitRating = 0, pen = 0, reducedRes = 0, hasteRating = 0;

            #region Get the values
            foreach (XmlNode xNode in spell.ChildNodes)
            {
                switch (xNode.Name)
                {
                    #region Bonus Damage
                    case "bonusDamage":
                        foreach (XmlNode xxNode in xNode.ChildNodes)
                        {
                            switch (xxNode.Name)
                            {
                                case "arcane":
                                    dArcane = Convert.ToInt32(xxNode.Attributes["value"].Value);
                                    break;
                                case "fire":
                                    dFire = Convert.ToInt32(xxNode.Attributes["value"].Value);
                                    break;
                                case "frost":
                                    dFrost = Convert.ToInt32(xxNode.Attributes["value"].Value);
                                    break;
                                case "holy":
                                    dHoly = Convert.ToInt32(xxNode.Attributes["value"].Value);
                                    break;
                                case "nature":
                                    dNature = Convert.ToInt32(xxNode.Attributes["value"].Value);
                                    break;
                                case "shadow":
                                    dShadow = Convert.ToInt32(xxNode.Attributes["value"].Value);
                                    break;
                            }
                        }
                        break; 
                    #endregion
                    case "bonusHealing":
                        try
                        {
                            healing = Convert.ToInt32(xNode.Attributes["value"].Value);
                        }
                        catch { healing = 0; }
                        break;
                    case "hitRating":
                        hitPct = Convert.ToSingle(xNode.Attributes["increasedHitPercent"].Value);
                        pen = Convert.ToInt32(xNode.Attributes["penetration"].Value);
                        reducedRes = Convert.ToInt32(xNode.Attributes["reducedResist"].Value);
                        hitRating = Convert.ToInt32(xNode.Attributes["value"].Value);
                        break;
                    #region Crit Chance
                    case "critChance":
                        foreach (XmlNode xxNode in xNode.ChildNodes)
                        {
                            switch (xxNode.Name)
                            {
                                case "arcane":
                                    cArcane = Convert.ToSingle(xxNode.Attributes["percent"].Value);
                                    break;
                                case "fire":
                                    cFire = Convert.ToSingle(xxNode.Attributes["percent"].Value);
                                    break;
                                case "frost":
                                    cFrost = Convert.ToSingle(xxNode.Attributes["percent"].Value);
                                    break;
                                case "holy":
                                    cHoly = Convert.ToSingle(xxNode.Attributes["percent"].Value);
                                    break;
                                case "nature":
                                    cNature = Convert.ToSingle(xxNode.Attributes["percent"].Value);
                                    break;
                                case "shadow":
                                    cShadow = Convert.ToSingle(xxNode.Attributes["percent"].Value);
                                    break;
                            }
                        }
                        break; 
                    #endregion
                    case "manaRegen":
                        manaCasting = Convert.ToSingle(xNode.Attributes["casting"].Value);
                        manaNotCasting = Convert.ToSingle(xNode.Attributes["notCasting"].Value);
                        break;
                    case "hasteRating":
                        haste = Convert.ToSingle(xNode.Attributes["hastePercent"].Value);
                        hasteRating = Convert.ToInt32(xNode.Attributes["hasteRating"].Value);
                        break;
                }
            }
            #endregion
            this.arcane = new SpellAttibute(dArcane, cArcane);
            this.fire = new SpellAttibute(dFire, cFire);
            this.frost = new SpellAttibute(dFrost, cFrost);
            this.holy = new SpellAttibute(dHoly, cHoly);
            this.nature = new SpellAttibute(dNature, cNature);
            this.shadow = new SpellAttibute(dShadow, cShadow);
            this.stats = new SpellStats(hitPct, hitRating, pen, reducedRes, manaCasting, manaNotCasting, haste, hasteRating);
        }
    }
    public class Defenses
    {
        //TODO: Fill this class
        public Defenses(XmlNode defenses)
        {
        }
    }
    public class Items
    {
        //TODO: Fill this class
        public Items(XmlNode items)
        {
        }
    }
    public class Glyphs
    {
        //TODO: Fill this class
        public Glyphs(XmlNode glyphs)
        {
        }
    }
}