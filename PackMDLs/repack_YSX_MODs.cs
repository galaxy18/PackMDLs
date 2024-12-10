using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

namespace repack_YSX_MODs
{
    public partial class repack_YSX_MODs : Form
    {
        private OpenFileDialog openFileDialog1;

        public int dlc_id = int.Parse(ConfigurationManager.AppSettings["default_DLC_id"]);

        List<kuroitem> collection = new List<kuroitem>() { };
        string workingdir = "";
        public static string outputdir = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["output"] ?? "output");
        string modeldir = "";
        string modelinfodir = "";
        string imagedir = "";

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

        public repack_YSX_MODs()
        {
            workingdir = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["working"] ?? "mods");
            modeldir = Path.Combine(outputdir, "asset", "common", "model");
            modelinfodir = Path.Combine(outputdir, "asset", "common", "model_info");
            imagedir = Path.Combine(outputdir, "asset", "dx11", "image");
            workingc0000 = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["c0000"] ?? "mdl_c0000");
            workingc0010 = Path.Combine(Application.StartupPath, ConfigurationManager.AppSettings["c0010"] ?? "mdl_c0010");
            Directory.CreateDirectory(workingdir);

            InitializeComponent();
        }

        private void repack_YSX_MODs_Load(object sender, EventArgs e)
        {
            openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "ZIP Files|*.zip";

            label1.Text = "";

            while (unavaliable_DLC_ids.Contains(dlc_id))
            {
                dlc_id++;
            }
            textBox1.Text = dlc_id + "";

            listBox1.DragDrop += new DragEventHandler(this.DragDrop_ZIP);
            listBox1.DragEnter += new DragEventHandler(this.DragEnter);
            listBox1.SelectedIndexChanged += new EventHandler(this.Select_mods);
            listBox1.DoubleClick += new EventHandler(this.show_Mods);
            listBox2.DragDrop += new DragEventHandler(this.DragDrop_C0000P3A);
            listBox2.DragEnter += new DragEventHandler(this.DragEnter);
            listBox2.DoubleClick += new EventHandler(this.show_C0000);
            listBox2.SelectedIndexChanged += new EventHandler(this.Select_C0000);
            listBox3.DragDrop += new DragEventHandler(this.DragDrop_C0010P3A);
            listBox3.DragEnter += new DragEventHandler(this.DragEnter);
            listBox3.SelectedIndexChanged += new EventHandler(this.Select_C0010);
            listBox3.DoubleClick += new EventHandler(this.show_C0010);

            Console.SetOut(new TextBoxWriter(listBox4));

            collectfiles();
        }

        public static kurodlc LoadJson(string path, JsonSerializerSettings settings)
        {
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                kurodlc result = JsonConvert.DeserializeObject<kurodlc>(json, settings);
                return result;
            }
        }

        private void collectfiles()
        {
            splitContainer1.Panel2Collapsed = true;
            collection = new List<kuroitem>() { };
            Console.WriteLine("loading " + workingdir);
            var folders = Directory.GetDirectories(workingdir);
            foreach (var folder in folders)
            {
                var foldername = Path.GetFileName(folder);
                var jsonfiles = Directory.GetFiles(folder, "*.kurodlc.json");
                foreach (var json in jsonfiles)
                {
                    string jsonfilename = Path.GetFileName(json);
                    Console.WriteLine("processing " + jsonfilename);
                    var dummy = LoadJson(json, null);
                    foreach (var dlc in dummy.DLCTable)
                    {
                        var item_ids = dlc.items;
                        var items = dummy.ItemTableData.Where(x => item_ids.IndexOf(x.id) >= 0).ToList();
                        foreach (var item in items)
                        {
                            var costumes = dummy.CostumeTable != null ? dummy.CostumeTable.Where(x => x.item_id == item.id).ToList() : new List<Costume>();
                            var costume_attachs = dummy.CostumeAttachTable != null ? dummy.CostumeAttachTable.Where(x => x.item_id == item.id).ToList() : new List<CostumeAttach>();
                            var costume_materials = dummy.CostumeMaterialTable != null ? dummy.CostumeMaterialTable.Where(x => x.item_id == item.id).ToList() : new List<CostumeMaterial>();
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
                        }
                    }
                }
            }
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            listBox3.Items.Clear();
            foreach (var item in collection)
            {
                listBox1.Items.Add(string.Format("{0} / {1} - {2}", item.zipname, item.Item.id, item.Item.name));
            }
            Console.WriteLine("load " + workingdir+" done");
            Directory.CreateDirectory(workingc0000);
            Console.WriteLine("loading " + workingc0000);
            foreach (var p3a in Directory.GetFiles(workingc0000, "*.p3a"))
            {
                Console.WriteLine("processing " + p3a);
                string working = Path.Combine(workingc0000, ConfigurationManager.AppSettings["c_processp3a"] ?? "_processp3a");
                parsep3a(working, p3a, TYPE_C0000_P3A);
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
                        name = Path.GetFileNameWithoutExtension(x)+
                            (Directory.Exists(Path.Combine(workingc0000,Path.GetFileNameWithoutExtension(x)+"_images"))?"":"(no image)"),
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
                listBox2.Show();
                foreach (var c0000mdl in c0000mdls)
                {
                    listBox2.Items.Add(c0000mdl.Item.name);
                }
            }
            Console.WriteLine("load " + workingc0000 + " done");

            Directory.CreateDirectory(workingc0010);
            foreach (var p3a in Directory.GetFiles(workingc0010, "*.p3a"))
            {
                Console.WriteLine("processing " + p3a);
                string working = Path.Combine(workingc0010, ConfigurationManager.AppSettings["c_processp3a"] ?? "_processp3a");
                parsep3a(working, p3a, TYPE_C0010_P3A);
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
                listBox3.Show();
                foreach (var c0010mdl in c0010mdls)
                {
                    listBox3.Items.Add(c0010mdl.Item.name);
                }
            }
            Console.WriteLine("load " + workingc0010 + " done");
        }

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
            else if (type == TYPE_C0000_P3A && File.Exists(filePath) && Path.GetExtension(filePath).Equals(".p3a"))
            {
                string filename = Path.GetFileName(filePath);
                File.Copy(filePath, Path.Combine(workingc0000, filename), true);
            }
            else if (type == TYPE_C0010_P3A && File.Exists(filePath) && Path.GetExtension(filePath).Equals(".p3a"))
            {
                string filename = Path.GetFileName(filePath);
                File.Copy(filePath, Path.Combine(workingc0010, filename), true);
            }
            Console.WriteLine("process " + filePath + " done, reload resource list.");
            collectfiles();
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

        private void ProcessExport(object sender, EventArgs e)
        {
            List<int> _unavaliable_item_ids = new List<int>();
            _unavaliable_item_ids = unavaliable_item_ids.Select(x=>x).ToList();
            var modelid = 1;

            dlc_id = int.Parse(textBox1.Text);
            while (unavaliable_DLC_ids.Contains(dlc_id))
            {
                dlc_id++;
            }
            textBox1.Text = dlc_id + "";

            Console.WriteLine("start export to DLC ID:" + dlc_id);

            if (Directory.Exists(Path.Combine(outputdir, "asset")))
            {
                Directory.Delete(Path.Combine(outputdir, "asset"), true);
            }
            Directory.CreateDirectory(modeldir);
            Directory.CreateDirectory(modelinfodir);
            Directory.CreateDirectory(imagedir);

            var indexs = listBox1.SelectedIndices;
            List<kuroitem> items = collection.Where((x, i) => indexs.IndexOf(i) >= 0).ToList();
            List<kuroitem> newitems = new List<kuroitem>();
            foreach (var item in items)
            {
                var newitem = JsonConvert.DeserializeObject<kuroitem>(JsonConvert.SerializeObject(item));
                var itemidmapping = new Dictionary<int, int>();
                if (!itemidmapping.ContainsKey(newitem.Item.id))
                {
                    var itemid = newitem.Item.id;
                    if (itemid == 0) { itemid = int.Parse(ConfigurationManager.AppSettings["default_item_id"] ?? "15000"); };
                    while (_unavaliable_item_ids.Contains(itemid))
                    {
                        itemid++;
                    }
                    newitem.Item.id = itemid;
                    _unavaliable_item_ids.Add(itemid);
                    itemidmapping.Add(newitem.Item.id, itemid);
                }
                else
                {
                    newitem.Item.id = itemidmapping[newitem.Item.id];
                }
                foreach (var elem in newitem.CostumeAttachTable)
                {
                    var modelname = string.Format("[{0}]_{1}", dlc_id, modelid);
                    var mdlname = elem.equip_model.ToLower();
                    var zipdir = item.zipname;
                    var contentfolder = Path.Combine(workingdir, zipdir);
                    foreach (var p3afile in Directory.GetFiles(contentfolder, "*.p3a"))
                    {
                        string p3aname = Path.GetFileNameWithoutExtension(p3afile);
                        if (!Directory.Exists(Path.Combine(contentfolder, p3aname)))
                        {
                            parsep3a(contentfolder, p3afile);
                        }
                    }
                    var mdlpath = getFile(contentfolder, mdlname + ".mdl");
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
                    var modelname = string.Format("[{0}]_{1}", dlc_id, modelid);
                    var mdlname = elem.equip_model.ToLower();
                    var zipdir = item.zipname;
                    var contentfolder = Path.Combine(workingdir, zipdir);
                    foreach (var p3afile in Directory.GetFiles(contentfolder, "*.p3a"))
                    {
                        string p3aname = Path.GetFileNameWithoutExtension(p3afile);
                        if (!Directory.Exists(Path.Combine(contentfolder, p3aname)))
                        {
                            parsep3a(contentfolder, p3afile);
                        }
                    }
                    var mdlpath = getFile(contentfolder, mdlname + ".mdl");
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
                    var modelname = string.Format("[{0}]_{1}", dlc_id, modelid);
                    var mdlname = elem.costume_model.ToLower();
                    var zipdir = item.zipname;
                    var contentfolder = Path.Combine(workingdir, zipdir);
                    foreach (var p3afile in Directory.GetFiles(contentfolder, "*.p3a"))
                    {
                        string p3aname = Path.GetFileNameWithoutExtension(p3afile);
                        if (!Directory.Exists(Path.Combine(contentfolder, p3aname)))
                        {
                            parsep3a(contentfolder, p3afile);
                        }
                    }
                    var mdlpath = getFile(contentfolder, mdlname + ".mdl");
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
            indexs = listBox2.SelectedIndices;
            var c0000items = c0000mdls.Where((x, i) => indexs.IndexOf(i) >= 0).ToList();
            foreach (var c0000item in c0000items)
            {
                var itemid = int.Parse(ConfigurationManager.AppSettings["default_item_id"] ?? "15000");
                while (_unavaliable_item_ids.Contains(itemid))
                {
                    itemid++;
                }
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
                        id = itemid,
                        icon = 370,
                        name = c0000item.Item.name,
                        short_desc = c0000item.Item.short_desc,
                        long_desc = c0000item.Item.long_desc,
                        type = 12
                    },
                    CostumeTable = new List<Costume>() { new Costume()
                        {
                            character_id = 1,
                            item_id = itemid,
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
                var itemid = int.Parse(ConfigurationManager.AppSettings["default_item_id"] ?? "15000");
                while (_unavaliable_item_ids.Contains(itemid))
                {
                    itemid++;
                }
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
                        id = itemid,
                        icon = 370,
                        name = c0010item.Item.name,
                        short_desc = c0010item.Item.short_desc,
                        long_desc = c0010item.Item.long_desc,
                        type = 13
                    },
                    CostumeTable = new List<Costume>() { new Costume()
                        {
                            character_id = 2,
                            item_id = itemid,
                            base_model = "c0010",
                            costume_model = modelname
                        }
                    }
                });
            }

            var dlc_items = new List<int>();
            var dlc_quantity = new List<int>();

            var ItemTableData = new List<Item>();
            var CostumeTable = new List<Costume>();
            var CostumeAttachTable = new List<CostumeAttach>();
            var CostumeMaterialTable = new List<CostumeMaterial>();

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

            var DLCTable = new List<DLC>(){
                new DLC() {
                    id = dlc_id,
                    name = textBox2.Text,
                    type_desc = textBox3.Text,
                    description = textBox4.Text,
                    quantity = dlc_quantity,
                    items = dlc_items,
                }
            };
            var kurodlcjson = new kurodlc()
            {
                DLCTable = DLCTable,
                CostumeTable = CostumeTable,
                CostumeAttachTable = CostumeAttachTable,
                CostumeMaterialTable = CostumeMaterialTable,
                ItemTableData = ItemTableData,
            };

            string json = JsonConvert.SerializeObject(kurodlcjson, Formatting.Indented);
            File.WriteAllText(Path.Combine(outputdir, dlc_id + ".kurodlc.json"), json);
            Console.WriteLine("end process json");
            Console.WriteLine("pack tables");
            packtbl();
            extracttbl();
            Console.WriteLine("pack p3a");
            packp3a(dlc_id+"");
            Console.WriteLine("end job");

            Process.Start(outputdir);
        }

        public static void packtbl()
        {
            if (!File.Exists(Path.Combine(outputdir, "kurodlc_make_zzz_tables.exe")))
            {
                File.Copy(Path.Combine(Application.StartupPath, "kurodlc_make_zzz_tables.exe"), Path.Combine(outputdir, "kurodlc_make_zzz_tables.exe"));
            }
            if (!File.Exists(Path.Combine(outputdir, "script.p3a")))
            {
                File.Copy(Path.Combine(Application.StartupPath, "script.p3a"), Path.Combine(outputdir, "script.p3a"));
            }
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Path.Combine(outputdir, "kurodlc_make_zzz_tables.exe");
            //startInfo.WorkingDirectory = Path.Combine(outputdir);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            Process p = Process.Start(startInfo);
            p.WaitForExit();
            File.Delete(Path.Combine(outputdir, "kurodlc_make_zzz_tables.exe"));
            File.Delete(Path.Combine(outputdir, "script.p3a"));
        }
        public static void extracttbl()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"p3a_tool";
            startInfo.Arguments = " extract zzz_combined_tables.p3a";
            startInfo.WorkingDirectory = Path.Combine(outputdir);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            Process p = Process.Start(startInfo);
            p.WaitForExit();
            File.Delete(Path.Combine(outputdir, "zzz_combined_tables.p3a"));
        }
        public static void packp3a(string p3aname)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"p3a_tool";
            startInfo.Arguments = " archive " + p3aname + ".p3a asset table table_eng table_fre";
            startInfo.WorkingDirectory = Path.Combine(outputdir);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            Process p = Process.Start(startInfo);
            p.WaitForExit();
            Directory.Delete(Path.Combine(outputdir, "table"), true);
            Directory.Delete(Path.Combine(outputdir, "table_eng"), true);
            Directory.Delete(Path.Combine(outputdir, "table_fre"), true);
        }
        public static void parsep3a(string folder, string p3afile,int type=TYPE_P3A_JSON_ARCHIVE)
        {
            if (type == TYPE_P3A_JSON_ARCHIVE)
            {
                string p3afilename = Path.GetFileName(p3afile);
                string p3adir = Path.GetFileNameWithoutExtension(p3afile);
                Directory.CreateDirectory(Path.Combine(folder, p3adir));
                File.Copy(Path.Combine(Application.StartupPath, "p3a_tool.exe"), Path.Combine(folder, p3adir,"p3a_tool.exe"), true);
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"p3a_tool";
                startInfo.Arguments = " extract \"" + Path.Combine(folder, p3afilename) + "\"";
                startInfo.WorkingDirectory = Path.Combine(folder, p3adir);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                Directory.CreateDirectory(Path.Combine(folder, p3adir));
                Process p = Process.Start(startInfo);
                p.WaitForExit();
                File.Delete(Path.Combine(folder, p3adir, "p3a_tool.exe"));
            }
            else
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
                Directory.CreateDirectory(folder);
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"p3a_tool";
                startInfo.Arguments = " extract \"" + p3afile + "\" -o";
                startInfo.WorkingDirectory = Path.Combine(folder);
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                Process p = Process.Start(startInfo);
                p.WaitForExit();
            }
        }
        #region searchfile
        public static string getFile(string contentfolder, string searchkey)
        {
            var result = getFile_casesensitive(contentfolder, searchkey, true);
            if (string.IsNullOrEmpty(result))
            {
                result = getFile_casesensitive(contentfolder, searchkey, false);
            }
            return result;
        }
        public static string getFile_casesensitive(string contentfolder, string searchkey, bool casesensitive)
        {
            var result = "";
            var folders = Directory.GetDirectories(contentfolder);
            foreach (var folder in folders)
            {
                result = getFile_casesensitive(folder, searchkey, casesensitive);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            var files = Directory.GetFiles(contentfolder);
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename == searchkey)
                {
                    return file;
                }
            }
            return result;
        }
        #endregion
        #region show and set info
        private void Select_mods(object sender, EventArgs e)
        {
            selected_index = listBox1.SelectedIndex;
            if (selected_index == -1) { return; }
            showinfo(collection.ElementAt(listBox1.SelectedIndex), TYPE_P3A_JSON_ARCHIVE);
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
            splitContainer1.Panel2Collapsed = false;
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
                    "DLC Desc:" + selecteditem.DLC.description+"\n"+
                    "assgin item name, short_desc, long_desc:";
            }
            else
            {
                label1.Text = "Model Name:" + Path.GetFileNameWithoutExtension(selecteditem.CostumeTable[0].costume_model);
            }
            textBox5.Text = selecteditem.Item.name;
            textBox6.Text = selecteditem.Item.short_desc;
            textBox7.Text = selecteditem.Item.long_desc;
        }
        private void apply_Modify_iteminfo(object sender, EventArgs e)
        {
            if (selected_index == -1)
            {
                return;
            }
            switch (selected_type)
            {
                case -1: { break; }
                case TYPE_P3A_JSON_ARCHIVE:
                    {
                        collection.ElementAt(selected_index).Item.name = textBox5.Text;
                        collection.ElementAt(selected_index).Item.short_desc = textBox6.Text;
                        collection.ElementAt(selected_index).Item.long_desc = textBox7.Text;
                        break;
                    }
                case TYPE_C0000_P3A:
                    {
                        c0000mdls.ElementAt(selected_index).Item.name = textBox5.Text;
                        c0000mdls.ElementAt(selected_index).Item.short_desc = textBox6.Text;
                        c0000mdls.ElementAt(selected_index).Item.long_desc = textBox7.Text;
                        break;
                    }
                case TYPE_C0010_P3A:
                    {
                        c0010mdls.ElementAt(selected_index).Item.name = textBox5.Text;
                        c0010mdls.ElementAt(selected_index).Item.short_desc = textBox6.Text;
                        c0010mdls.ElementAt(selected_index).Item.long_desc = textBox7.Text;
                        break;
                    }
            }
        }
        #endregion
        #region otherevents
        public class TextBoxWriter : TextWriter
        {
            ListBox lstBox;
            delegate void VoidAction();

            public TextBoxWriter(ListBox box)
            {
                lstBox = box;
                lstBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
                lstBox.MeasureItem += lst_MeasureItem;
                lstBox.DrawItem += lst_DrawItem;
            }

            public override void Write(string value)
            {
                VoidAction action = delegate
                {
                    lstBox.Items.Insert(0, string.Format(" [{0:HH:mm:ss}]{1} ", DateTime.Now, value));
                };
                lstBox.TopIndex = lstBox.Items.Count - 1;
                lstBox.BeginInvoke(action);
            }

            public override void WriteLine(string value)
            {
                VoidAction action = delegate
                {
                    string str = string.Format(" [{0:HH:mm:ss}]{1} ", DateTime.Now, value);
                    lstBox.Items.Add(str);
                    lstBox.TopIndex = lstBox.Items.Count - 1;
                };
                try
                {
                    lstBox.BeginInvoke(action);
                }
                catch (Exception e) { }
            }

            private void lst_MeasureItem(object sender, MeasureItemEventArgs e)
            {
                e.ItemHeight = (int)e.Graphics.MeasureString(lstBox.Items[e.Index].ToString(), lstBox.Font, lstBox.Width).Height;
            }

            private void lst_DrawItem(object sender, DrawItemEventArgs e)
            {
                e.DrawBackground();
                e.DrawFocusRectangle();
                e.Graphics.DrawString(lstBox.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);
            }

            public override System.Text.Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }
        }
        private void show_Mods(object sender, EventArgs e)
        {
            selected_index = listBox1.SelectedIndex;
            if (selected_index == -1)
            {
                Process.Start(workingdir);
            }
            else
            {
                Process.Start(Path.Combine(workingdir, collection.ElementAt(listBox1.SelectedIndex).zipname));
            }
        }
        private void show_C0000(object sender, EventArgs e)
        {
            Process.Start(workingc0000);
        }
        private void show_C0010(object sender, EventArgs e)
        {
            Process.Start(workingc0010);
        }
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
        private void reloadCollections(object sender, EventArgs e)
        {
            collectfiles();
        }
        private void label4_Click(object sender, EventArgs e)
        {
            label4.Hide();
        }
        #endregion
    }
}