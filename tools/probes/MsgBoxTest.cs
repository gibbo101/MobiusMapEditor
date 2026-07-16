using System;
using System.Drawing;
using System.Windows.Forms;

class MsgBoxTest
{
    [STAThread]
    static void Main()
    {
        var form = new Form { Text = "MsgBoxTest", Size = new Size(300, 120), StartPosition = FormStartPosition.Manual, Location = new Point(60, 60) };
        var btn = new Button { Text = "NormalButton", Location = new Point(20, 20), Size = new Size(120, 30) };
        form.Controls.Add(btn);
        form.Shown += (s, e) => MessageBox.Show(form, "Do you want to save?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        Application.Run(form);
    }
}
