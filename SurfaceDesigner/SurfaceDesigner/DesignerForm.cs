using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisualizationDesigner;

namespace SurfaceDesigner
{
    public partial class DesignerForm : Form
    {

        Designer surface = new Designer();
        UndoEngineExt undoEngine;
        ISelectionService _selectionService = null;
        Form CurrentRoot = null;
        object currentItem = null;

        public DesignerForm()
        {
            InitializeComponent();
            CreateDesigner();
        }

        public void CreateDesigner()
        {
            Form rootComponent = null;

            rootComponent = (Form)surface.CreateRootComponent(typeof(Form), new Size(1920, 814));
            rootComponent.BackColor = Color.LightGray;
            rootComponent.FormBorderStyle = FormBorderStyle.None;
            rootComponent.Text = "Root";
            rootComponent.AllowDrop = true;
        //    rootComponent.DragDrop += RootComponent_DragDrop;

            Control view = surface.GetView();
            if (null == view) return;
            view.Text = "Form";
            view.Dock = DockStyle.Fill;
            view.Parent = panel1;
            view.Parent.AllowDrop = true;
        //   view.Parent.DragDrop += Parent_DragDrop;
            undoEngine = surface.GetUndoEngineExt();
            undoEngine.Enabled = true;

            CurrentRoot = rootComponent;
          
            _selectionService = (ISelectionService)(surface.GetIDesignerHost().GetService(typeof(ISelectionService)));
            if (null != _selectionService)
                _selectionService.SelectionChanged += new System.EventHandler(OnSelectionChanged);

            surface.CreateControl(typeof(Button), new Size(100, 50), new Point(50, 50));

          var group =  (GroupBox)surface.CreateControl(typeof(GroupBox), new Size(100, 100), new Point(100, 50));

          var control = surface.CreateControl(typeof(Button), new Size(100, 50), new Point(5, 5));

            group.Controls.Add(control);
        }

        private void OnSelectionChanged(object sender, System.EventArgs e)
        {
            if (_selectionService == null)
                return;


            ISelectionService selectionService = null;
            selectionService = surface.GetIDesignerHost().GetService(typeof(ISelectionService)) as ISelectionService;
            this.propertyGrid1.SelectedObject = currentItem = selectionService.PrimarySelection;
            var currentControl = currentItem as Control;

           

            //    propertyGridControl1.SelectedObject = currentItem;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadRuntime(1);
        }

        private void LoadRuntime(int type)
        {
            var controls = surface.ComponentContainer.Components;
            SerializationStore data = (SerializationStore)surface._designerSerializationService.Serialize(controls);


            MemoryStream ms = new MemoryStream();
            data.Save(ms);
            SaveData.Data = ms.ToArray();
            SaveData.LoadType = type;
            new RuntimeForm().Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            LoadRuntime(2);
        }
    }
}
