using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShellX
{
    public partial class TestForm : Form
    {
        public static void Show(string path)
        {
            new TestForm() { initialPath = path }.ShowDialog();
        }

        public TestForm()
        {
            InitializeComponent();
            this.VisibleChanged += (s, e)
                => pathTextBox.Text = initialPath ?? Environment.CurrentDirectory;
        }

        string initialPath;

        void Popup()
        {
            var selection = new List<string>();

            for (int i = 0; i < selectionCount.Value; i++)
                selection.Add(pathTextBox.Text);

            var explorerSelction = new ExplorerSelectionStub(selection.ToArray());

            if (!explorerSelction.CanShowMenu())
            {
                MessageBox.Show("No menu is configured for the specified path");
                return;
            }

            var cm = explorerSelction.CreateMenu();

            cm.Items.Add(new ToolStripSeparator());

            var screen = Screen.FromPoint(Cursor.Position);
            var left = screen.Bounds.X + screen.Bounds.Width / 2 - 200;
            var top = screen.Bounds.Y + 150;
            cm.Show(new Point(left, top));

            cm.Focus();
        }

        void button1_Click(object sender, EventArgs e) => Popup();

        void button2_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(App.ConfigDir) && Directory.GetDirectories(App.ConfigDir).Any())
                MessageBox.Show("Directory already exists and it is not empty.");
            else
                App.Main(new[] { "-init" });
        }

        void button3_Click(object sender, EventArgs e)
        {
            App.Main(new[] { "-open" });
        }

        private void pathTextBox_TextChanged(object sender, EventArgs e)
        {
        }
    }
}