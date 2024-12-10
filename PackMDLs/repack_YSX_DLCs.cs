using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace repack_YSX_MODs
{
    public partial class repack_YSX_DLCs : Form
    {
        private OpenFileDialog openFileDialog1;
        public int dlc_id = int.Parse(ConfigurationManager.AppSettings["default_DLC_id"]);
        string YSXPREFIX = "YS10ADDCONT";

        List<kuroitem> collection = new List<kuroitem>() { };
        string workingdir = "";
        string outputdir = "";
        string modeldir = "";
        string modelinfodir = "";
        string imagedir = "";
        string configfile = "";
        IDmapping idmapping = new IDmapping();

        string workingc0000 = "";
        string workingc0010 = "";
        List<kuroitem> c0000mdls = new List<kuroitem>() { };
        List<kuroitem> c0010mdls = new List<kuroitem>() { };

        private const int TYPE_P3A_JSON_ARCHIVE = 0;
        private const int TYPE_C0000_P3A = 1;
        private const int TYPE_C0010_P3A = 2;

        private int selected_type = -1;
        private int selected_index = -1;

        int[] unavaliable_DLC_ids = (ConfigurationManager.AppSettings["unavaliable_DLC_ids"] ?? "-1").Split(',').Select(x => int.Parse(x)).ToArray();
        int[] unavaliable_item_ids = (ConfigurationManager.AppSettings["unavaliable_item_ids"] ?? "-1").Split(',').Select(x => int.Parse(x)).ToArray();

        string p3aname = ConfigurationManager.AppSettings["p3aname"] ?? "mods";

        public repack_YSX_DLCs()
        {
            workingdir = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["working"] ?? "mods");
            outputdir = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["output"] ?? "output");
            modeldir = Path.Combine(outputdir, "asset", "common", "model");
            modelinfodir = Path.Combine(outputdir, "asset", "common", "model_info");
            imagedir = Path.Combine(outputdir, "asset", "dx11", "image");
            workingc0000 = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["c0000"] ?? "mdl_c0000");
            workingc0010 = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["c0010"] ?? "mdl_c0010");
            configfile= Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["mdlmapping"] ?? "mdlmapping.json");
            Directory.CreateDirectory(workingdir);

            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            label1.Text = "";
            openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "ZIP Files|*.zip";
            Console.SetOut(new repack_YSX_MODs.TextBoxWriter(listBox4));
            /*while (unavaliable_DLC_ids.Contains(dlc_id))
            {
                dlc_id++;
            }*/
            collectfiles();

            treeView1.DragDrop += new DragEventHandler(this.DragDrop_ZIP);
            treeView1.DragEnter += new DragEventHandler(this.DragEnter);
            treeView1.NodeMouseClick += convTreeView_NodeMouseClick;
            treeView1.NodeMouseDoubleClick += convTreeView_NodeMouseDoubleClick;
            treeView1.KeyUp += convTreeView_KeyUp;
            listBox2.DragDrop += new DragEventHandler(this.DragDrop_C0000P3A);
            listBox2.DragEnter += new DragEventHandler(this.DragEnter);
            listBox2.DoubleClick += new EventHandler(this.show_C0000);
            listBox2.SelectedIndexChanged += new EventHandler(this.Select_C0000);
            listBox3.DragDrop += new DragEventHandler(this.DragDrop_C0010P3A);
            listBox3.DragEnter += new DragEventHandler(this.DragEnter);
            listBox3.SelectedIndexChanged += new EventHandler(this.Select_C0010);
            listBox3.DoubleClick += new EventHandler(this.show_C0010);
        }

        public class IDmapping
        {
            public Dictionary<string, Dictionary<string, int>> dlc=new Dictionary<string, Dictionary<string, int>>();
            public Dictionary<string, Dictionary<string, int>> item=new Dictionary<string, Dictionary<string, int>>();
        }

        private void collectfiles()
        {
            idmapping = new IDmapping();
            if (File.Exists(configfile))
            {
                using (StreamReader r = new StreamReader(configfile))
                {
                    string config = r.ReadToEnd();
                    idmapping = JsonConvert.DeserializeObject<IDmapping>(config);
                }
            }
            List<int> dlcids = new List<int>();
            List<int> itemids = new List<int>();
            List<int> _unavaliable_item_ids = new List<int>();
            List<int> _unavaliable_DLC_ids = new List<int>();
            _unavaliable_item_ids = unavaliable_item_ids.Select(x => x).ToList();
            _unavaliable_DLC_ids = unavaliable_DLC_ids.Select(x => x).ToList();
            int min_new_dlcid = 1;
            int min_new_item_id = 1;

            splitContainer5.Panel2Collapsed = true;
            treeView1.Nodes.Clear();
            collection = new List<kuroitem>() { };
            Console.WriteLine("loading " + workingdir);
            foreach(var zipfile in Directory.GetFiles(workingdir, "*.zip"))
            {
                var zipname = Path.GetFileNameWithoutExtension(zipfile);
                if (!Directory.Exists(Path.Combine(workingdir, zipname))){
                    ZipFile.ExtractToDirectory(zipfile, Path.Combine(workingdir, zipname));
                }
            }
            List<string> folders = Directory.GetDirectories(workingdir).ToList();
            folders.Sort((a, b) => {
                a = Path.GetFileName(a);
                b = Path.GetFileName(b);
                if (idmapping.dlc.ContainsKey(a) && idmapping.dlc.ContainsKey(b))
                {
                    return a.CompareTo(b);
                }
                else if (idmapping.dlc.ContainsKey(a))
                {
                    return -1;
                }
                else if (idmapping.dlc.ContainsKey(b))
                {
                    return 1;
                }
                else
                {
                    return a.CompareTo(b);
                }
            });
            List<string> zipnames = new List<string>();
            foreach (var folder in folders)
            {
                var foldername = Path.GetFileName(folder);
                zipnames.Add(foldername);
                var jsonfiles = Directory.GetFiles(folder, "*.kurodlc.json");
                #region getmapping
                var dlcmapping = new Dictionary<string, int>();
                if (idmapping.dlc.ContainsKey(foldername) && idmapping.dlc[foldername] != null)
                {
                    dlcmapping = idmapping.dlc[foldername];
                }
                else
                {
                    idmapping.dlc[foldername] = dlcmapping;
                }
                var itemmapping = new Dictionary<string, int>();
                if (idmapping.item.ContainsKey(foldername) && idmapping.item[foldername] != null)
                {
                    itemmapping = idmapping.item[foldername];
                }
                else
                {
                    idmapping.item[foldername] = itemmapping;
                }
                #endregion
                foreach (var p3afile in Directory.GetFiles(folder, "*.p3a"))
                {
                    string p3aname = Path.GetFileNameWithoutExtension(p3afile);
                    if (!Directory.Exists(Path.Combine(folder, p3aname,"asset")))
                    {
                        repack_YSX_MODs.parsep3a(folder, p3afile);
                        if (!Directory.Exists(Path.Combine(folder, p3aname, "asset")))
                        {
                            Console.WriteLine("unpack " + p3aname + " failed");
                        }
                    }
                }
                foreach (var json in jsonfiles)
                {
                    string jsonfilename = Path.GetFileName(json);
                    Console.WriteLine("processing " + jsonfilename);
                    var dummy = repack_YSX_MODs.LoadJson(json, null);
                    foreach (var dlc in dummy.DLCTable)
                    {
                        #region dlc
                        while (dlcids.IndexOf(min_new_dlcid) >= 0 || _unavaliable_DLC_ids.IndexOf(min_new_dlcid) >= 0)
                        {
                            min_new_dlcid++;
                        }
                        int temp_dlc_id = dlc.id;
                        if (dlcmapping.ContainsKey(dlc.id + ""))
                        {
                            temp_dlc_id = dlcmapping[dlc.id + ""];
                        }
                        if (dlcids.IndexOf(temp_dlc_id) >= 0)
                        {
                            temp_dlc_id = min_new_dlcid;
                            idmapping.dlc[foldername][dlc.id + ""] = temp_dlc_id;
                            //dlc.name += "(id duplicated)";
                        }
                        dlc.id = temp_dlc_id;
                        dlcids.Add(dlc.id);
                        var item_ids = dlc.items;
                        var items = dummy.ItemTableData.Where(x => item_ids.IndexOf(x.id) >= 0).ToList();
                        var dlcnode = new TreeNode(String.Format("{2} / [{0}]{1}", dlc.id, dlc.name, foldername));
                        dlcnode.Tag = new kuroitem()
                        {
                            DLC = dlc,
                            zipname = foldername,
                        };
                        #endregion
                        foreach (var item in items)
                        {
                            var costumes = dummy.CostumeTable != null ? dummy.CostumeTable.Where(x => x.item_id == item.id).ToList() : new List<Costume>();
                            var costume_attachs = dummy.CostumeAttachTable != null ? dummy.CostumeAttachTable.Where(x => x.item_id == item.id).ToList() : new List<CostumeAttach>();
                            var costume_materials = dummy.CostumeMaterialTable != null ? dummy.CostumeMaterialTable.Where(x => x.item_id == item.id).ToList() : new List<CostumeMaterial>();
                            #region itemid
                            while (itemids.IndexOf(min_new_item_id) >= 0 || _unavaliable_item_ids.IndexOf(min_new_item_id) >= 0)
                            {
                                min_new_item_id++;
                            }
                            int temp_item_id = item.id;
                            if (itemmapping.ContainsKey(item.id + ""))
                            {
                                temp_item_id = itemmapping[item.id + ""];
                            }
                            if (itemids.IndexOf(temp_item_id) >= 0)
                            {
                                temp_item_id = min_new_item_id;
                                idmapping.item[foldername][item.id + ""] = temp_item_id;
                                //item.name += "(id duplicated)";
                                foreach (var costume in costumes)
                                {
                                    costume.item_id = temp_item_id;
                                }
                                foreach (var costume_attach in costume_attachs)
                                {
                                    costume_attach.item_id = temp_item_id;
                                }
                                foreach (var costume_material in costume_materials)
                                {
                                    costume_material.item_id = temp_item_id;
                                }
                            }
                            #endregion
                            item.id = temp_item_id;
                            itemids.Add(item.id);
                            List<string> modelinvalid = new List<string>();
                            foreach(var costume in costumes)
                            {
                                string mdl = costume.costume_model;
                                string path = repack_YSX_MODs.getFile(folder, mdl+".mdl");
                                if (string.IsNullOrEmpty(path))
                                {
                                    modelinvalid.Add(mdl + ".mdl");
                                }
                            }
                            foreach (var costume in costume_attachs)
                            {
                                string mdl = costume.equip_model;
                                string path = repack_YSX_MODs.getFile(folder, mdl + ".mdl");
                                if (string.IsNullOrEmpty(path))
                                {
                                    modelinvalid.Add(mdl + ".mdl");
                                }
                            }
                            foreach (var costume in costume_materials)
                            {
                                string mdl = costume.equip_model;
                                string path = repack_YSX_MODs.getFile(folder, mdl + ".mdl");
                                if (string.IsNullOrEmpty(path))
                                {
                                    modelinvalid.Add(mdl + ".mdl");
                                }
                            }
                            /*foreach (var unzipped in Directory.GetDirectories(folder))
                            {
                                if (!Directory.Exists(Path.Combine(unzipped, "asset")))
                                {
                                    modelinvalid = true;
                                }
                            }*/
                            var newitem = new kuroitem()
                            {
                                DLC = dlc,
                                Item = item,
                                CostumeTable = costumes,
                                CostumeAttachTable = costume_attachs,
                                CostumeMaterialTable = costume_materials,
                                zipname = foldername,
                                preview = Path.Combine(folder, item.id + ".jpg")
                            };
                            collection.Add(newitem);
                            var itemnode = new TreeNode(string.Format("{0} - {1}", item.id, item.name));
                            itemnode.Tag = newitem;
                            if (modelinvalid.Count>0)
                            {
                                itemnode.Text += " ("+String.Join(", ", modelinvalid.ToArray()) + " not found)";
                                itemnode.BackColor = Color.LightGray;
                                dlcnode.BackColor = Color.LightGray;
                            }
                            dlcnode.Nodes.Add(itemnode);
                        }
                        treeView1.Nodes.Add(dlcnode);
                    }
                }
            }
            Console.WriteLine("load " + workingdir + " done");
            #region C0000
            listBox2.Items.Clear();
            Directory.CreateDirectory(workingc0000);
            Console.WriteLine("loading " + workingc0000);
            foreach (var p3a in Directory.GetFiles(workingc0000, "*.p3a"))
            {
                Console.WriteLine("processing " + p3a);
                string working = Path.Combine(workingc0000, ConfigurationManager.AppSettings["c_processp3a"] ?? "_processp3a");
                repack_YSX_MODs.parsep3a(working, p3a, TYPE_C0000_P3A);
                if (Directory.Exists(Path.Combine(working, "asset", "common", "model")))
                {
                    foreach (var mdl in Directory.GetFiles(Path.Combine(working, "asset", "common", "model")))
                    {
                        File.Copy(mdl, Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + ".mdl"), true);
                        if (Directory.Exists(Path.Combine(working, "asset", "dx11", "image")))
                        {
                            if (Directory.Exists(Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images")))
                            {
                                Directory.Delete(Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images"), true);
                            }
                            Directory.CreateDirectory(Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images"));
                            foreach (var image in Directory.GetFiles(Path.Combine(working, "asset", "dx11", "image")))
                            {
                                File.Copy(image, Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images", Path.GetFileName(image)));
                            }
                        }
                        else
                        {
                            Console.WriteLine("no image exists in " + p3a);
                        }
                        if (File.Exists(Path.Combine(working, "asset", "common", "model_info", Path.GetFileNameWithoutExtension(mdl) + ".mi")))
                        {
                            File.Copy(Path.Combine(working, "asset", "common", "model_info", Path.GetFileNameWithoutExtension(mdl) + ".mi"),
                               Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + ".mi"), true);
                        }
                        else
                        {
                            Console.WriteLine("no mi file exists in " + p3a + ";use default file");
                            File.Copy(Path.Combine(Application.StartupPath, "c0000.mi"),
                               Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + ".mi"), true);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("no model exists in " + p3a);
                }
                Console.WriteLine("remove " + p3a);
                File.Delete(p3a);
                Directory.Delete(working, true);
            }
            var _c0000mdls = Directory.GetFiles(workingc0000, "*.mdl");
            Console.WriteLine("processing " + workingc0000);
            if (_c0000mdls.Length > 0)
            {
                c0000mdls = _c0000mdls.Select((x, i) => new kuroitem()
                {
                    Item = new Item()
                    {
                        id = 0,
                        icon = 370,
                        name = Path.GetFileNameWithoutExtension(x) +
                            (Directory.Exists(Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(x) + "_images")) ? "" : "(no image)"),
                        short_desc = Path.GetFileNameWithoutExtension(x),
                        long_desc = Path.GetFileNameWithoutExtension(x),
                        type = 12
                    },
                    CostumeTable = new List<Costume>(){ new Costume()
                        {
                            character_id = 1,
                            item_id = 0,
                            base_model = "c0000",
                            costume_model = x
                        }
                    },
                    preview = Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(x) + ".jpg")
                }).ToList();
                //listBox2.Show();
                foreach (var c0000mdl in c0000mdls)
                {
                    listBox2.Items.Add(c0000mdl.Item.name);
                }
            }
            Console.WriteLine("load " + workingc0000 + " done");
            #endregion
            #region C0010
            listBox3.Items.Clear();
            Directory.CreateDirectory(workingc0010);
            foreach (var p3a in Directory.GetFiles(workingc0010, "*.p3a"))
            {
                Console.WriteLine("processing " + p3a);
                string working = Path.Combine(workingc0010, ConfigurationManager.AppSettings["c_processp3a"] ?? "_processp3a");
                repack_YSX_MODs.parsep3a(working, p3a, TYPE_C0010_P3A);
                if (Directory.Exists(Path.Combine(working, "asset", "common", "model")))
                {
                    foreach (var mdl in Directory.GetFiles(Path.Combine(working, "asset", "common", "model")))
                    {
                        File.Copy(mdl, Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + ".mdl"), true);
                        if (Directory.Exists(Path.Combine(working, "asset", "dx11", "image")))
                        {
                            if (Directory.Exists(Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images")))
                            {
                                Directory.Delete(Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images"), true);
                            }
                            Directory.CreateDirectory(Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images"));
                            foreach (var image in Directory.GetFiles(Path.Combine(working, "asset", "dx11", "image")))
                            {
                                File.Copy(image, Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + "_images", Path.GetFileName(image)));
                            }
                        }
                        else
                        {
                            Console.WriteLine("no image exists in " + p3a);
                        }
                        if (File.Exists(Path.Combine(working, "asset", "common", "model_info", Path.GetFileNameWithoutExtension(mdl) + ".mi")))
                        {
                            File.Copy(Path.Combine(working, "asset", "common", "model_info", Path.GetFileNameWithoutExtension(mdl) + ".mi"),
                               Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + ".mi"), true);
                        }
                        else
                        {
                            Console.WriteLine("no mi file exists in " + p3a + ";use default file");
                            File.Copy(Path.Combine(Application.StartupPath, "c0010.mi"),
                               Path.Combine(workingc0000, Path.GetFileNameWithoutExtension(p3a) + "_" + Path.GetFileNameWithoutExtension(mdl) + ".mi"), true);
                        }
                    }
                }
                Console.WriteLine("remove " + p3a);
                File.Delete(p3a);
                Directory.Delete(working, true);
            }
            var _c0010mdls = Directory.GetFiles(workingc0010, "*.mdl");
            Console.WriteLine("processing " + workingc0010);
            if (_c0010mdls.Length > 0)
            {
                c0010mdls = _c0010mdls.Select((x, i) => new kuroitem()
                {
                    Item = new Item()
                    {
                        id = 0,
                        icon = 370,
                        name = Path.GetFileNameWithoutExtension(x) +
                            (Directory.Exists(Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(x) + "_images")) ? "" : "(no image)"),
                        short_desc = Path.GetFileNameWithoutExtension(x),
                        long_desc = Path.GetFileNameWithoutExtension(x),
                        type = 13
                    },
                    CostumeTable = new List<Costume>(){ new Costume()
                        {
                            character_id = 2,
                            item_id = 0,
                            base_model = "c0010",
                            costume_model = x
                        }
                    },
                    preview = Path.Combine(workingc0010, Path.GetFileNameWithoutExtension(x) + ".jpg")
                }).ToList();
                //listBox3.Show();
                foreach (var c0010mdl in c0010mdls)
                {
                    listBox3.Items.Add(c0010mdl.Item.name);
                }
            }
            Console.WriteLine("load " + workingc0010 + " done");
            #endregion
            #region hide checkbox for unused nodes
            treeView1.ExpandAll();
            //treeView1.CheckBoxes=true;
            /*foreach(TreeNode n in treeView1.Nodes)
            {
                foreach(TreeNode n1 in n.Nodes)
                {
                    foreach (TreeNode n2 in n1.Nodes)
                    {
                        HideCheckBox(treeView1, n2);
                        foreach (TreeNode n3 in n2.Nodes)
                        {
                            HideCheckBox(treeView1, n3);
                        }
                    }
                }
            }*/
            #endregion
            idmapping = new IDmapping()
            {
                dlc=idmapping.dlc.Where(x=>zipnames.IndexOf( x.Key)>=0).ToDictionary(x=>x.Key,x=>x.Value),
                item = idmapping.item.Where(x => zipnames.IndexOf(x.Key) >= 0).ToDictionary(x=>x.Key,x=>x.Value),
            };
            string newconfig = JsonConvert.SerializeObject(idmapping, Formatting.Indented);
            if (File.Exists(configfile)) { File.Delete(configfile); }
            File.WriteAllText(configfile, newconfig);
        }
        private void repack(bool all=false)
        {
            //List<int> dlcids = new List<int>();
            //List<int> itemids = new List<int>();
            var modelid = 1;
            var ItemTableData = new List<Item>();
            var CostumeTable = new List<Costume>();
            var CostumeAttachTable = new List<CostumeAttach>();
            var CostumeMaterialTable = new List<CostumeMaterial>();
            var DLCTable = new List<DLC>();
            #region delete and create Directory
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.StartupPath).FullName, "dlc"));
            /*foreach (var dlckey in Directory.GetFiles(Path.Combine(Directory.GetParent(Application.StartupPath).FullName, "dlc")))
            {
                var id = Int32.Parse(Path.GetFileNameWithoutExtension(dlckey).Replace(YSXPREFIX, ""));
                if (_unavaliable_DLC_ids.IndexOf(id) < 0)
                {
                    _unavaliable_DLC_ids.Add(id);
                }
            }*/
            if (Directory.Exists(Path.Combine(outputdir, "asset")))
            {
                Directory.Delete(Path.Combine(outputdir, "asset"), true);
            }
            Directory.CreateDirectory(modeldir);
            Directory.CreateDirectory(modelinfodir);
            Directory.CreateDirectory(imagedir);
            #endregion

            foreach (TreeNode n in treeView1.Nodes)
            {
                kuroitem dlc = (kuroitem)n.Tag;
                List<kuroitem> items = new List<kuroitem>();
                List<kuroitem> newitems = new List<kuroitem>();
                foreach (TreeNode n1 in n.Nodes)
                {
                    if (all || n.Checked || n1.Checked)
                    {
                        kuroitem item = (kuroitem)n1.Tag;
                        items.Add(item);
                    }
                }
                if (items.Count > 0)
                {
                    Console.WriteLine("processing" + dlc.zipname);
                    var new_dlc_id =dlc.DLC.id;
                    Console.WriteLine("processing DLC " + dlc.DLC.id);
                    var itemidmapping = new Dictionary<int, int>();
                    foreach (var item in items)
                    {
                        var newitem = JsonConvert.DeserializeObject<kuroitem>(JsonConvert.SerializeObject(item));
                        Console.WriteLine("processing item " + newitem.Item.id);
                        foreach (var elem in newitem.CostumeAttachTable)
                        {
                            var modelname = string.Format("[{0}]_{1}", new_dlc_id, modelid);
                            var mdlname = elem.equip_model.ToLower();
                            var zipdir = item.zipname;
                            var contentfolder = Path.Combine(workingdir, zipdir);
                            foreach (var p3afile in Directory.GetFiles(contentfolder, "*.p3a"))
                            {
                                string p3aname = Path.GetFileNameWithoutExtension(p3afile);
                                if (!Directory.Exists(Path.Combine(contentfolder, p3aname)))
                                {
                                    repack_YSX_MODs.parsep3a(contentfolder, p3afile);
                                    if (!Directory.Exists(Path.Combine(contentfolder, p3aname, "asset")))
                                    {
                                        Console.WriteLine("unpack " + p3aname + " failed");
                                    }
                                }
                            }
                            var mdlpath = repack_YSX_MODs.getFile(contentfolder, mdlname + ".mdl");
                            if (!string.IsNullOrEmpty(mdlpath))
                            {
                                File.Copy(mdlpath, Path.Combine(modeldir, modelname + ".mdl"), true);
                                var assetfolder = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(mdlpath).FullName).FullName).FullName, "dx11", "image");
                                if (Directory.Exists(assetfolder))
                                {
                                    foreach (var image in Directory.GetFiles(assetfolder, "*.dds"))
                                    {
                                        File.Copy(image, Path.Combine(imagedir, Path.GetFileName(image)), true);
                                    }
                                }
                                var mifolder = Directory.GetParent(mdlpath).FullName + "_info";
                                var mifile = Path.Combine(mifolder, mdlname + ".mi");
                                if (Directory.Exists(mifolder) && File.Exists(mifile))
                                {
                                    File.Copy(mifile, Path.Combine(modelinfodir, modelname + ".mi"), true);
                                }
                                elem.equip_model = modelname;
                                elem.item_id = newitem.Item.id;
                            }
                            else
                            {
                                Console.WriteLine(mdlname + ".mdl not found");
                            }
                            modelid++;
                        }
                        foreach (var elem in newitem.CostumeMaterialTable)
                        {
                            var modelname = string.Format("[{0}]_{1}", new_dlc_id, modelid);
                            var mdlname = elem.equip_model.ToLower();
                            var zipdir = item.zipname;
                            var contentfolder = Path.Combine(workingdir, zipdir);
                            foreach (var p3afile in Directory.GetFiles(contentfolder, "*.p3a"))
                            {
                                string p3aname = Path.GetFileNameWithoutExtension(p3afile);
                                if (!Directory.Exists(Path.Combine(contentfolder, p3aname)))
                                {
                                    repack_YSX_MODs.parsep3a(contentfolder, p3afile);
                                }
                            }
                            var mdlpath = repack_YSX_MODs.getFile(contentfolder, mdlname + ".mdl");
                            if (!string.IsNullOrEmpty(mdlpath))
                            {
                                File.Copy(mdlpath, Path.Combine(modeldir, modelname + ".mdl"), true);
                                var assetfolder = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(mdlpath).FullName).FullName).FullName, "dx11", "image");
                                if (Directory.Exists(assetfolder))
                                {
                                    foreach (var image in Directory.GetFiles(assetfolder, "*.dds"))
                                    {
                                        File.Copy(image, Path.Combine(imagedir, Path.GetFileName(image)), true);
                                    }
                                }
                                var mifolder = Directory.GetParent(mdlpath).FullName + "_info";
                                var mifile = Path.Combine(mifolder, mdlname + ".mi");
                                if (Directory.Exists(mifolder) && File.Exists(mifile))
                                {
                                    File.Copy(mifile, Path.Combine(modelinfodir, modelname + ".mi"), true);
                                }
                                elem.equip_model = modelname;
                                elem.item_id = newitem.Item.id;
                            }
                            else
                            {
                                Console.WriteLine(mdlname + ".mdl not found");
                            }
                            modelid++;
                        }
                        foreach (var elem in newitem.CostumeTable)
                        {
                            var modelname = string.Format("[{0}]_{1}", new_dlc_id, modelid);
                            var mdlname = elem.costume_model.ToLower();
                            var zipdir = item.zipname;
                            var contentfolder = Path.Combine(workingdir, zipdir);
                            foreach (var p3afile in Directory.GetFiles(contentfolder, "*.p3a"))
                            {
                                string p3aname = Path.GetFileNameWithoutExtension(p3afile);
                                if (!Directory.Exists(Path.Combine(contentfolder, p3aname)))
                                {
                                    repack_YSX_MODs.parsep3a(contentfolder, p3afile);
                                }
                            }
                            var mdlpath = repack_YSX_MODs.getFile(contentfolder, mdlname + ".mdl");
                            if (!string.IsNullOrEmpty(mdlpath))
                            {
                                File.Copy(mdlpath, Path.Combine(modeldir, modelname + ".mdl"), true);
                                var assetfolder = Path.Combine(Directory.GetParent(Directory.GetParent(Directory.GetParent(mdlpath).FullName).FullName).FullName, "dx11", "image");
                                if (Directory.Exists(assetfolder))
                                {
                                    foreach (var image in Directory.GetFiles(assetfolder, "*.dds"))
                                    {
                                        File.Copy(image, Path.Combine(imagedir, Path.GetFileName(image)), true);
                                    }
                                }
                                var mifolder = Directory.GetParent(mdlpath).FullName + "_info";
                                var mifile = Path.Combine(mifolder, mdlname + ".mi");
                                if (Directory.Exists(mifolder) && File.Exists(mifile))
                                {
                                    File.Copy(mifile, Path.Combine(modelinfodir, modelname + ".mi"), true);
                                }
                                elem.costume_model = modelname;
                                elem.item_id = newitem.Item.id;
                            }
                            else
                            {
                                Console.WriteLine(mdlname + ".mdl not found");
                            }
                            modelid++;
                        }
                        newitems.Add(newitem);
                    }
                    var dlc_items = new List<int>();
                    var dlc_quantity = new List<int>();

                    foreach (var newitem in newitems)
                    {
                        dlc_items.Add(newitem.Item.id);
                        dlc_quantity.Add(1);
                        ItemTableData.Add(newitem.Item);
                        if (newitem.CostumeTable != null)
                        {
                            CostumeTable.AddRange(newitem.CostumeTable);
                        }
                        if (newitem.CostumeAttachTable != null)
                        {
                            CostumeAttachTable.AddRange(newitem.CostumeAttachTable);
                        }
                        if (newitem.CostumeMaterialTable != null)
                        {
                            CostumeMaterialTable.AddRange(newitem.CostumeMaterialTable);
                        }
                    }
                    DLCTable.Add(new DLC()
                    {
                        id = new_dlc_id,
                        name = dlc.DLC.name,
                        type_desc = dlc.DLC.type_desc,
                        description = dlc.DLC.description,
                        quantity = dlc_quantity,
                        items = dlc_items,
                    });
                }
            }
            if (listBox2.Items.Count+listBox3.Items.Count>0)
            {
                List<kuroitem> newitems = new List<kuroitem>();
                var dlc_items = new List<int>();
                var dlc_quantity = new List<int>();

                while (unavaliable_DLC_ids.Contains(dlc_id)|| DLCTable.Select(x=>x.id).Contains(dlc_id))
                {
                    dlc_id++;
                }
                var indexs = listBox2.SelectedIndices;
                var c0000items = c0000mdls.Where((x, i) => indexs.IndexOf(i) >= 0).ToList();
                foreach (var c0000item in c0000items)
                {
                    var modelname = string.Format("[{0}]_{1}", dlc_id, modelid);
                    modelid++;
                    var mdlpath = c0000item.CostumeTable[0].costume_model;
                    File.Copy(mdlpath, Path.Combine(modeldir, modelname + ".mdl"), true);
                    var assetfolder = Path.Combine(Directory.GetParent(mdlpath).FullName, Path.GetFileNameWithoutExtension(mdlpath) + "_images");
                    if (Directory.Exists(assetfolder))
                    {
                        foreach (var image in Directory.GetFiles(assetfolder, "*.dds"))
                        {
                            File.Copy(image, Path.Combine(imagedir, Path.GetFileName(image)), true);
                        }
                    }
                    else
                    {
                        Console.WriteLine("skip copy image files " + assetfolder + " :not exists");
                    }
                    var mifile = Path.Combine(Directory.GetParent(mdlpath).FullName, Path.GetFileNameWithoutExtension(mdlpath) + ".mi");
                    if (File.Exists(mifile))
                    {
                        File.Copy(mifile, Path.Combine(modelinfodir, modelname + ".mi"), true);
                    }
                    else
                    {
                        Console.WriteLine("mi file " + mifile + " not exists;use default");
                        File.Copy(Path.Combine(Application.StartupPath, "c0000.mi"), Path.Combine(modelinfodir, modelname + ".mi"), true);
                    }
                    newitems.Add(new kuroitem()
                    {
                        Item = new Item()
                        {
                            id = 0,
                            icon = 370,
                            name = c0000item.Item.name,
                            short_desc = c0000item.Item.short_desc,
                            long_desc = c0000item.Item.long_desc,
                            type = 12
                        },
                        CostumeTable = new List<Costume>() { new Costume()
                        {
                            character_id = 1,
                            item_id = 0,
                            base_model = "c0000",
                            costume_model = modelname
                        }
                    }
                    });
                }
                indexs = listBox3.SelectedIndices;
                var c0010items = c0010mdls.Where((x, i) => indexs.IndexOf(i) >= 0).ToList();
                foreach (var c0010item in c0010items)
                {
                    var modelname = string.Format("[{0}]_{1}", dlc_id, modelid);
                    modelid++;
                    var mdlpath = c0010item.CostumeTable[0].costume_model;
                    File.Copy(mdlpath, Path.Combine(modeldir, modelname + ".mdl"), true);
                    var assetfolder = Path.Combine(Directory.GetParent(mdlpath).FullName, Path.GetFileNameWithoutExtension(mdlpath) + "_images");
                    if (Directory.Exists(assetfolder))
                    {
                        foreach (var image in Directory.GetFiles(assetfolder, "*.dds"))
                        {
                            File.Copy(image, Path.Combine(imagedir, Path.GetFileName(image)), true);
                        }
                    }
                    else
                    {
                        Console.WriteLine("skip copy image files " + assetfolder + " :not exists");
                    }
                    var mifile = Path.Combine(Directory.GetParent(mdlpath).FullName, Path.GetFileNameWithoutExtension(mdlpath) + ".mi");
                    if (File.Exists(mifile))
                    {
                        File.Copy(mifile, Path.Combine(modelinfodir, modelname + ".mi"), true);
                    }
                    else
                    {
                        Console.WriteLine("mi file " + mifile + " not exists;use default");
                        File.Copy(Path.Combine(Application.StartupPath, "c0010.mi"), Path.Combine(modelinfodir, modelname + ".mi"), true);
                    }
                    newitems.Add(new kuroitem()
                    {
                        Item = new Item()
                        {
                            id = 0,
                            icon = 370,
                            name = c0010item.Item.name,
                            short_desc = c0010item.Item.short_desc,
                            long_desc = c0010item.Item.long_desc,
                            type = 13
                        },
                        CostumeTable = new List<Costume>() { new Costume()
                        {
                            character_id = 2,
                            item_id = 0,
                            base_model = "c0010",
                            costume_model = modelname
                        }
                    }
                    });
                }

                var itemid = int.Parse(ConfigurationManager.AppSettings["default_item_id"] ?? "15000");
                foreach (var newitem in newitems)
                {
                    while (unavaliable_item_ids.Contains(itemid) || ItemTableData.Select(x => x.id).Contains(itemid))
                    {
                        itemid++;
                    }
                    newitem.Item.id = itemid;
                    dlc_items.Add(newitem.Item.id);
                    dlc_quantity.Add(1);
                    ItemTableData.Add(newitem.Item);
                    if (newitem.CostumeTable != null)
                    {
                        foreach(var costume in newitem.CostumeTable)
                        {
                            costume.item_id = itemid;
                            CostumeTable.Add(costume);
                        }
                    }
                    if (newitem.CostumeAttachTable != null)
                    {
                        foreach (var costume in newitem.CostumeAttachTable)
                        {
                            costume.item_id = itemid;
                            CostumeAttachTable.Add(costume);
                        }
                    }
                    if (newitem.CostumeMaterialTable != null)
                    {
                        foreach (var costume in newitem.CostumeMaterialTable)
                        {
                            costume.item_id = itemid;
                            CostumeMaterialTable.Add(costume);
                        }
                    }
                }
                DLCTable.Add(new DLC() { 
                    id = dlc_id,
                    name = "mdls",
                    type_desc = "mdls",
                    description = "mdls",
                    quantity = dlc_quantity,
                    items = dlc_items,
                });
            }
            var kurodlcjson = new kurodlc()
            {
                DLCTable = DLCTable,
                CostumeTable = CostumeTable,
                CostumeAttachTable = CostumeAttachTable,
                CostumeMaterialTable = CostumeMaterialTable,
                ItemTableData = ItemTableData,
            };

            string json = JsonConvert.SerializeObject(kurodlcjson, Formatting.Indented);
            File.WriteAllText(Path.Combine(outputdir, p3aname+".kurodlc.json"), json);
            Console.WriteLine("end process json");
            Console.WriteLine("pack tables");
            repack_YSX_MODs.packtbl();
            repack_YSX_MODs.extracttbl();
            Console.WriteLine("pack "+ p3aname+".p3a");
            repack_YSX_MODs.packp3a(p3aname);

            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.StartupPath).FullName, "mods"));
            File.Copy(Path.Combine(outputdir, p3aname + ".p3a"), Path.Combine(Directory.GetParent(Application.StartupPath).FullName, "mods", p3aname + ".p3a"), true);
            foreach (var id in kurodlcjson.DLCTable.Select(x => x.id))
            {
                string filename = string.Format(YSXPREFIX+"{0:D5}", id);
                if (!File.Exists(Path.Combine(Directory.GetParent(Application.StartupPath).FullName, "dlc", filename)))
                {
                    FileStream fs; //声明FileStream对象
                    try
                    {
                        fs = new FileStream(Path.Combine(Directory.GetParent(Application.StartupPath).FullName, "dlc", filename), FileMode.Create); //初始化FileStream对象
                        byte[] byteArray = new byte[] { 240, 159, 165, 154 };
                        fs.Write(byteArray, 0, byteArray.Length);
                        fs.Close(); //关闭文件流
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            Console.WriteLine("end job");
        }

        #region HideCheckBox
        private const int TVIF_STATE = 0x8;
        private const int TVIS_STATEIMAGEMASK = 0xF000;
        private const int TV_FIRST = 0x1100;
        private const int TVM_SETITEM = TV_FIRST + 63;

        [StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Auto)]
        private struct TVITEM
        {
            public int mask;
            public IntPtr hItem;
            public int state;
            public int stateMask;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpszText;
            public int cchTextMax;
            public int iImage;
            public int iSelectedImage;
            public int cChildren;
            public IntPtr lParam;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam,
                                                 ref TVITEM lParam);

        /// <summary>
        /// Hides the checkbox for the specified node on a TreeView control.
        /// </summary>
        private void HideCheckBox(TreeView tvw, TreeNode node)
        {
            TVITEM tvi = new TVITEM();
            tvi.hItem = node.Handle;
            tvi.mask = TVIF_STATE;
            tvi.stateMask = TVIS_STATEIMAGEMASK;
            tvi.state = 0;
            SendMessage(tvw.Handle, TVM_SETITEM, IntPtr.Zero, ref tvi);
        }
        #endregion
        #region clickevents
        private void selectButton_Click(object sender, EventArgs e)
        {
            processZIP(sender, e);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            collectfiles();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            repack(true);
        }
        private void show_C0000(object sender, EventArgs e)
        {
            Process.Start(workingc0000);
        }
        private void show_C0010(object sender, EventArgs e)
        {
            Process.Start(workingc0010);
        }
        private void Select_C0000(object sender, EventArgs e)
        {
            selected_index = listBox2.SelectedIndex;
            if (selected_index == -1) { return; }
            showinfo(c0000mdls.ElementAt(listBox2.SelectedIndex), TYPE_C0000_P3A);
        }
        private void Select_C0010(object sender, EventArgs e)
        {
            selected_index = listBox3.SelectedIndex;
            if (selected_index == -1) { return; }
            showinfo(c0010mdls.ElementAt(listBox3.SelectedIndex), TYPE_C0010_P3A);
        }
        private void showinfo(kuroitem selecteditem, int type)
        {
            selected_type = type;
            splitContainer5.Panel2Collapsed = false;
            if (File.Exists(selecteditem.preview))
            {
                pictureBox1.Image = Image.FromFile(selecteditem.preview);
                label4.Text = "";
                label4.Hide();
            }
            else
            {
                pictureBox1.Image = null;
                label4.Text = selecteditem.preview + "not found.";
                label4.Show();
            }
            if (type == TYPE_P3A_JSON_ARCHIVE)
            {
                label1.Text = "DLC Name:" + selecteditem.DLC.name + "\n" +
                    "DLC Type Desc:" + selecteditem.DLC.type_desc + "\n" +
                    "DLC Desc:" + selecteditem.DLC.description + "\n";
            }
            else
            {
                label1.Text = "Model Name:" + Path.GetFileNameWithoutExtension(selecteditem.CostumeTable[0].costume_model);
            }
        }
        void ManageTreeChecked(TreeNode node)
        {
            kuroitem item = (kuroitem)node.Tag;
            showinfo(item, TYPE_P3A_JSON_ARCHIVE);
            if (string.IsNullOrEmpty(item.preview))
            {
                label4.Hide();
            }
            /*
                foreach (TreeNode n in node.Nodes)
                {
                    n.Checked = node.Checked;
                }
            */
        }
        private void convTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            ManageTreeChecked(e.Node);
        }
        private void convTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            kuroitem item = (kuroitem)e.Node.Tag;
            string zipname = item.zipname;
            Process.Start(Path.Combine(workingdir, zipname));
        }
        private void convTreeView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                ManageTreeChecked(treeView1.SelectedNode);
            }
        }
        #endregion
        private void processZIP(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var filePath = openFileDialog1.FileName;
                processimport(filePath, TYPE_P3A_JSON_ARCHIVE);
            }
        }
        private void processimport(string filePath, int type)
        {
            Console.WriteLine("processing " + filePath);
            if (type == TYPE_P3A_JSON_ARCHIVE && File.Exists(filePath) && Path.GetExtension(filePath).Equals(".zip"))
            {
                string filename = Path.GetFileName(filePath);
                string newdir = Path.GetFileNameWithoutExtension(filename);
                if (Directory.Exists(Path.Combine(workingdir, newdir)))
                {
                    Directory.Delete(Path.Combine(workingdir, newdir), true);
                }
                Directory.CreateDirectory(Path.Combine(workingdir, newdir));
                File.Copy(filePath, Path.Combine(workingdir, filename), true);
                //ZipFile.CreateFromDirectory(startPath, zipPath);
                ZipFile.ExtractToDirectory(Path.Combine(workingdir, filename), Path.Combine(workingdir, newdir));
            }
            else if (type == TYPE_C0000_P3A && File.Exists(filePath))
            {
                if (Path.GetExtension(filePath).Equals(".p3a"))
                {
                    string filename = Path.GetFileName(filePath);
                    File.Copy(filePath, Path.Combine(workingc0000, filename), true);
                }
                else if (Path.GetExtension(filePath).Equals(".zip"))
                {
                    string filename = Path.GetFileName(filePath);
                    File.Copy(filePath, Path.Combine(workingc0000, filename), true);
                    ZipFile.ExtractToDirectory(workingc0000, Path.Combine(workingc0000, filename));
                }
            }
            else if (type == TYPE_C0010_P3A && File.Exists(filePath))
            {
                if (Path.GetExtension(filePath).Equals(".p3a"))
                {
                    string filename = Path.GetFileName(filePath);
                    File.Copy(filePath, Path.Combine(workingc0010, filename), true);
                }
                else if (Path.GetExtension(filePath).Equals(".zip"))
                {
                    string filename = Path.GetFileName(filePath);
                    File.Copy(filePath, Path.Combine(workingc0010, filename), true);
                    ZipFile.ExtractToDirectory(workingc0010, Path.Combine(workingc0010, filename));
                }
            }
            Console.WriteLine("process " + filePath + " done, reload resource list.");
            collectfiles();
            //repack(true);
        }

        #region dragdrop
        private new void DragEnter(object sender, DragEventArgs e)
        {
            // If the data is a file or a bitmap, display the copy cursor.
            if (e.Data.GetDataPresent(DataFormats.Bitmap) ||
               e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void DragDrop_ZIP(object sender, DragEventArgs e)
        {
            // Handle FileDrop data.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    foreach (var file in files)
                    {
                        processimport(file, TYPE_P3A_JSON_ARCHIVE);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
        }
        private void DragDrop_C0000P3A(object sender, DragEventArgs e)
        {
            // Handle FileDrop data.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    foreach (var file in files)
                    {
                        processimport(file, TYPE_C0000_P3A);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
        }
        private void DragDrop_C0010P3A(object sender, DragEventArgs e)
        {
            // Handle FileDrop data.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Assign the file names to a string array, in 
                // case the user has selected multiple files.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                try
                {
                    foreach (var file in files)
                    {
                        processimport(file, TYPE_C0010_P3A);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            listBox4.Items.Clear();
        }
    }
}
