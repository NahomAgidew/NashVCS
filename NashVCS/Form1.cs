using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;

namespace NashVCS
{
    /// <summary>
    /// Created by Nahom on 6/26/2016
    /// </summary>
    public partial class Form1 : Form
    {
        string proj = "";
        const string DATABASE = "log.bin";
        bool opened = false;

        public Form1()
        {
            InitializeComponent();
            listBox1.SelectedIndexChanged += new EventHandler(listBox1_SelectedIndexChanged);
        }

        /// <summary>
        /// New Project
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowNewFolderButton = true;
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string project = folderBrowserDialog1.SelectedPath;
                proj = project;
                if (!File.Exists(project + "\\" + DATABASE))
                {
                    Register(project);
                }

                else if (File.Exists(project + "\\" + DATABASE))
                {
                    MessageBox.Show("Already on my watch.");
                }
            }
        }

        /// <summary>
        /// Open Project
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowNewFolderButton = false;            

            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string project = folderBrowserDialog1.SelectedPath;
                listBox1.Items.Clear();
                textBox2.Clear();
                textBox3.Clear();

                if (File.Exists(project + "\\" + DATABASE))
                {
                    proj = project;
                    opened = true;
                    foreach (var dir in Directory.GetDirectories(project))
                    {
                        foreach (var file in Directory.GetFiles(dir))
                        {
                            if (Path.GetExtension(file) == ".cs")
                            {
                                listBox1.Items.Add(file.Remove(0, file.LastIndexOf("\\") + 1));
                            }
                        }
                    }

                    Database db = new Database(proj + "\\" + DATABASE);
                    List<string> initialFiles = new List<string>();

                    SQLiteDataReader reader = db.ExecuteQuery("SELECT * FROM InitialFiles");

                    while (reader.Read())
                    {
                        initialFiles.Add(reader[0].ToString());
                    }

                    foreach (var i in ListFiles(proj))
                    {
                        if (Path.GetExtension(i) == ".cs")
                        {
                            string temp = i.Remove(0, i.LastIndexOf("\\") + 1);
                            if (!initialFiles.Contains(temp))
                            {
                                LogData("New file " + i + " added.");
                            }
                        }
                    }
                    db.CloseDatabase();
                }
                else
                {
                    MessageBox.Show("Not on my watch.", "Could not open", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        /// <summary>
        /// Commit Project
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (opened)
            {
                /******************************* Commit Files *******************************/
                Database db = new Database(proj + "\\" + DATABASE);
                List<string> initialFiles = new List<string>();

                SQLiteDataReader reader = db.ExecuteQuery("SELECT * FROM InitialFiles");

                while (reader.Read())
                {
                    initialFiles.Add(reader[0].ToString());
                }
                db.ExecuteNonQuery("DROP TABLE InitialFiles");
                db.ExecuteNonQuery("CREATE TABLE InitialFiles (file VARCHAR)");

                foreach (var i in ListFiles(proj))
                {
                    if (Path.GetExtension(i) == ".cs")
                    {
                        string temp = i.Remove(0, i.LastIndexOf("\\") + 1);
                        db.ExecuteQuery(string.Format("INSERT INTO InitialFiles (file) VALUES ('{0}')", temp));
                    }
                }

                /******************************* Commit Actual Code *******************************/
                db.ExecuteNonQuery("DROP TABLE InitialCode");
                db.ExecuteNonQuery("CREATE TABLE InitialCode (file VARCHAR, code VARCHAR)");

                foreach (var s in ListFiles(proj))
                {
                    if (Path.GetExtension(s) == ".cs")
                    {
                        using (StreamReader read = new StreamReader(s))
                        {
                            try
                            {
                                db.ExecuteNonQuery(string.Format("INSERT INTO InitialCode (file, code) VALUES ('{0}', '{1}')", s.Remove(0, s.LastIndexOf("\\") + 1), read.ReadToEnd()));
                            }
                            catch (Exception ex0)
                            {
                                MessageBox.Show(ex0.Message, "Data Not added", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }

                /******************************* *******************************/
                LogData("Committed!");
                db.CloseDatabase();
                /******************************** END *******************************/
            }

            else
            {
                MessageBox.Show("Project not opened");
            }
        }

        void Register(string path)
        {
            Database db = new Database(proj + "\\" + DATABASE);

            db.ExecuteNonQuery("CREATE TABLE InitialFiles (file VARCHAR)");
            db.ExecuteNonQuery("CREATE TABLE InitialCode (file VARCHAR, code VARCHAR)");

            foreach (var s in ListFiles(proj))
            {
                if (Path.GetExtension(s) == ".cs")
                {
                    string temp = s.Remove(0, s.LastIndexOf("\\") + 1);
                    /******************************* Add Files *******************************/
                    temp = string.Format("INSERT INTO InitialFiles (file) VALUES ('{0}')", temp);
                    db.ExecuteNonQuery(temp);
                    /******************************* END *******************************/

                    /******************************* Add Actual Code *******************************/
                    using (StreamReader reader = new StreamReader(s))
                    {
                        temp = string.Format("INSERT INTO InitialCode (file, code) VALUES ('{0}', '{1}')", s.Remove(0, s.LastIndexOf("\\") + 1), reader.ReadToEnd());
                        try
                        {
                            db.ExecuteNonQuery(temp);
                        }
                        catch(Exception ex0)
                        {
                            MessageBox.Show(ex0.Message);
                        }
                    }
                    /******************************* END *******************************/
                }
            }

            db.CloseDatabase();
            MessageBox.Show("New Project created", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        string[] ListFiles(string path)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                return Directory.GetFiles(dir);
            }
            return null;
        }

        void LogData(string logstring)
        {
            textBox1.AppendText(logstring + Environment.NewLine);
        }


        void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            using (Database db = new Database(proj + "\\" + DATABASE))
            {
                textBox2.Text = db.ExecuteQuery(string.Format("SELECT code FROM InitialCode WHERE file = '{0}'", listBox1.SelectedItem))[0].ToString();
            }
            foreach (var s in ListFiles(proj))
            {
                if (Path.GetExtension(s) == ".cs" && s.Remove(0, s.LastIndexOf("\\")+1) == listBox1.SelectedItem.ToString())
                {
                    using (StreamReader reader = new StreamReader(s))
                    {
                        textBox3.Text = reader.ReadToEnd();
                    }
                }
            }
        }
    }
}
