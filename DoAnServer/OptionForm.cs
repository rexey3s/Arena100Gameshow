using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DoAnServer
{
    public partial class OptionForm : Form
    {
        public OptionForm()
        {
            InitializeComponent();
        }
        public OptionForm(Form form)
        {
            InitializeComponent();
            _mainForm = form as Mainform;
        }
        public Mainform _mainForm = null;
        /// <summary>
        /// OK Button 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int iPlayers = int.Parse(textBox1.Text);
                int iQuestions = int.Parse(textBox3.Text);
                int iTimeOutSeconds = int.Parse(textBox2.Text);
                _mainForm = new Mainform(iPlayers,iTimeOutSeconds,iQuestions);
                this.Hide();
                _mainForm._OptionForm = this;
                this.Owner = _mainForm;
                _mainForm.Show();

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Cancel Button 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();

            
        }
    }
}
