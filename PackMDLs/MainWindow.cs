using System;
using System.Windows.Forms;

namespace repack_YSX_MODs
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();

            this.tabControl1.SelectedIndexChanged += new System.EventHandler(tabControl1_SelectedIndexChanged);

            this.tabPage1.Controls.Clear();
            repack_YSX_MODs main = new repack_YSX_MODs();
            main.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            main.TopLevel = false;
            main.Show();
            main.Dock = DockStyle.Fill;
            this.tabPage1.Controls.Add(main);
            main.WindowState = FormWindowState.Maximized;

            this.tabPage2.Controls.Clear();
            repack_YSX_DLCs sub = new repack_YSX_DLCs();
            sub.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            sub.TopLevel = false;
            sub.Show();
            sub.Dock = DockStyle.Fill;
            this.tabPage2.Controls.Add(sub);
            sub.WindowState = FormWindowState.Maximized;
        }
        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //switch ((sender as TabControl).SelectedIndex)
            //{
            //    case 0:
            //        {
            //            this.tabPage1.Controls.Clear();
            //            repack_YSX_MODs main = new repack_YSX_MODs();
            //            main.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            //            main.TopLevel = false;
            //            this.tabPage1.Controls.Add(main);
            //            main.WindowState = FormWindowState.Maximized;
            //            main.Show();
            //            main.Dock = DockStyle.Fill;
            //            break;
            //        }
            //    case 1:
            //        {
            //            this.tabPage2.Controls.Clear();
            //            pack_YSX_DLCs sub = new pack_YSX_DLCs();
            //            sub.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            //            sub.TopLevel = false;
            //            this.tabPage2.Controls.Add(sub);
            //            sub.WindowState = FormWindowState.Maximized;
            //            sub.Show();
            //            sub.Dock = DockStyle.Fill;
            //            break;
            //        }
            //}
        }
    }
}
