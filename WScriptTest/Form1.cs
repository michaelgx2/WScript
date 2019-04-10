using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WScriptLib;

namespace WScriptTest
{
    public partial class Form1 : Form
    {
        WUScriptRunner _runner;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _runner = new WUScriptRunner(()=>0.1f);
            //Prepare script functions
            //msg("content"[, secs]);//Show a message and wait for optional several seconds.
            _runner.RegisterCmd("msg", line =>//Register a command called 'msg'
            {
                this.Invoke(new Action(() =>
                {
                    txtConsole.AppendText(_runner.ReplaceParams(line.Parameters[0].ToString() + Environment.NewLine));//Get string parameter
                }));
                float waitTime = 0.5f;//Default wait seconds
                if (line.Parameters.Count > 1)
                {
                    waitTime = Convert.ToSingle(line.Parameters[1]); //Get number parameter
                }

                return waitTime;
            });
            //clear();//Clear all texts in console
            _runner.RegisterCmd("clear", line =>
            {
                this.Invoke(new Action(() => { txtConsole.Text = ""; }));
                return 0;//without waiting
            });
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _runner.Update();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            _runner.Parameters.Clear();
            WScript ws = new WScript(txtScript.Text);
            _runner.RunScript(ws, WUScrRunType.Override);
        }
    }
}
