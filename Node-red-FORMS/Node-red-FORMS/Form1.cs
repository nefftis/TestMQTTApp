﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Node_red_FORMS
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Add add = new Add(this);
            add.Show();        
            this.Hide();         
        }
        private void button2_Click(object sender, EventArgs e)
        {
            Update update = new Update(this);
            update.Show();        
            this.Hide();         
        }
        private void button3_Click(object sender, EventArgs e)
        {
            Browse browse = new Browse(this);
            browse.Show();
            this.Hide();

        }
        private void button4_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
