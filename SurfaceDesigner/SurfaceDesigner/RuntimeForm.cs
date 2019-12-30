using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisualizationDesigner;

namespace SurfaceDesigner
{
    public partial class RuntimeForm : Form
    {
        public RuntimeForm()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
           
                MemoryStream ms = new MemoryStream(SaveData.Data);
                Designer d = new Designer();
                var controls = d._designerSerializationService.Deserialize(ms);

                ms.Close();
            if (SaveData.LoadType == 1)
            {
                foreach (Control cont in controls)
                {
                    var ts = Assembly.Load(cont.GetType().Assembly.FullName);
                    var o = ts.GetType(cont.GetType().FullName);
                    Control controlform = (Control)Activator.CreateInstance(o);


                    PropertyInfo[] controlProperties = cont.GetType().GetProperties();

                    foreach (PropertyInfo propInfo in controlProperties)
                    {
                        if (propInfo.CanWrite)
                        {

                            if (propInfo.Name != "Site" && propInfo.Name != "WindowTarget")
                            {
                                try
                                {
                                    var obj = propInfo.GetValue(cont, null);
                                    propInfo.SetValue(controlform, obj, null);

                                }
                                catch { }
                            }

                            else
                            {

                            }
                        }
                    }

                    Controls.Add(controlform);
                }
            }
            else
            {
                foreach (Control cont in controls)
                    Controls.Add(cont);
            }
            
        }
    }
}
