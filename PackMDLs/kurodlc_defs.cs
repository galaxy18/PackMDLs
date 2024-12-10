using Newtonsoft.Json;
using System.Collections.Generic;

namespace repack_YSX_MODs
{
    public class kuroitem
    {
        public DLC DLC;
        public Item Item;
        public List<Costume> CostumeTable;
        public List<CostumeAttach> CostumeAttachTable;
        public List<CostumeMaterial> CostumeMaterialTable;
        public string zipname;
        public string p3aname;
        public string preview;
    }
    public class kurodlc
    {
        public List<DLC> DLCTable;
        public List<Costume> CostumeTable;
        public List<CostumeAttach> CostumeAttachTable;
        public List<CostumeMaterial> CostumeMaterialTable;
        public List<Item> ItemTableData;
    }

    public class DLC
    {
        public int id;
        public int sort_id = 1;
        public List<int> items;
        [JsonProperty("unk0")]
        public int int2 = 0;
        public List<int> quantity;
        [JsonProperty("unk1")]
        public int int3 = 0;
        public string name;
        public string type_desc;
        [JsonProperty("desc")]
        public string description;
    }
    public class Item
    {
        public int id;
        public int icon;
        public string name;
        public string short_desc;
        [JsonProperty("desc")]
        public string long_desc;
        [JsonProperty("unk0")]
        public int int2=0;
        public int type;
        [JsonProperty("unk1")]
        public short short1=0;
        [JsonProperty("unk2")]
        public int int3=1;
        public int unused0 = 0;
        public int unused1 = 0;
        public int unused2 = 0;
        //public List<Dictionary<string, int>> maybe_unused0 = new List<Dictionary<string, int>>()
        //    {
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                },
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                },
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                }
        //    };
        [JsonProperty("unk3")]
        public byte byte0 = 4;
        [JsonProperty("unk4")]
        public byte byte1 = 43;
        [JsonProperty("unk5")]
        public byte byte2 = 0;
        [JsonProperty("unk6")]
        public byte byte3 = 0;
        [JsonProperty("unk7")]
        public int int4 = 100;
        [JsonProperty("unk8")]
        public byte byte4 = 0;
        [JsonProperty("unk9")]
        public byte byte5 = 2;
        public int unused3 = 0;
        public int unused4 = 0;
        public int unused5 = 0;
        //public List<Dictionary<string, int>> maybe_unused1 = new List<Dictionary<string, int>>()
        //    {
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                },
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                },
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                },
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                },
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                },
        //                new Dictionary<string, int>{
        //                    ["value"]=0
        //                }
        //    };
        public string obtain_type="";//"use_instant_hp"
        public double obtain_amount=0.0;//10.0
        [JsonProperty("unk10")]
        public int int5= 50395;
        public int hp=0;
        public int str = 0;
        [JsonProperty("break")]
        public int brk = 0;
        public int def = 0;
        public int vit = 0;
        public int luck = 0;
        public int crit = 0;
        public int eva = 0;
        public int dmg = 0;
        public int dmg_received = 0;
        public int eff1_id = 0;
        public double eff1_0 = 0.0;
        public int eff2_id = 0;
        public double eff2_0 = 0.0;
        public int eff3_id = 0;
        public double eff3_0 = 0.0;
        public int eff4_id = 0;
        public double eff4_0 = 0.0;
        //public List<effect> effects = new List<effect>()
        //    {
        //               new effect{
        //                   id=0,
        //                   value=0.0
        //               },
        //               new effect{
        //                   id=0,
        //                   value=0.0
        //               },
        //               new effect{
        //                   id=0,
        //                   value=0.0
        //               },
        //               new effect{
        //                   id=0,
        //                   value=0.0
        //               },
        //    };
        [JsonProperty("unk_txt")]
        public string text4="";
        [JsonProperty("unk11")]
        public int int8=0;
        [JsonProperty("unk12")]
        public int int9=0;
    }
    public class effect
    {
        public int id;
        public double value;
    }
    public class CostumeBase
    {
        public int character_id;
        public int item_id = 0;
        public string base_model;
    }
    public class Costume : CostumeBase
    {
        public string costume_model;
    }
    public class CostumeAttach : CostumeBase
    {
        public string equip_model;
        [JsonProperty("unk0")]
        public int int0 = 2;
        [JsonProperty("unk1")]
        public int int2 = 0;
        public string attach_point;
        [JsonProperty("unk_text0")]
        public string text3;
        [JsonProperty("unk_text1")]
        public string text4;
        [JsonProperty("unk_text2")]
        public string text5;
        [JsonProperty("unk_text3")]
        public string text6;
        [JsonProperty("unk_text4")]
        public string text7;
    }
    public class CostumeMaterial : CostumeBase
    {
        public string equip_model;

    }
}