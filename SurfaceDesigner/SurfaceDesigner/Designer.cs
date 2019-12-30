

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml.Serialization;

namespace VisualizationDesigner
{
    public class Designer : DesignSurface
    {
        private UndoEngineExt _undoEngine = null;
        private NameCreationServiceImp _nameCreationService = null;
        public DesignerSerializationServiceImpl _designerSerializationService = null;
        private CodeDomComponentSerializationService _codeDomComponentSerializationService = null;

        public Designer()
        {
            InitServices();
            SetOptions();

        }

        public void SetOptions()
        {
            IServiceContainer serviceProvider = this.GetService(typeof(IServiceContainer)) as IServiceContainer;
            DesignerOptionService opsService = serviceProvider.GetService(typeof(DesignerOptionService)) as DesignerOptionService;
            if (null != opsService)
            {
                serviceProvider.RemoveService(typeof(DesignerOptionService));
            }
            DesignerOptionService opsService2 = new SMDesignerOptions();
            serviceProvider.AddService(typeof(DesignerOptionService), opsService2);

        }

        public UndoEngineExt GetUndoEngineExt()
        {
            return this._undoEngine;
        }


        private void InitServices()
        {
            //- each DesignSurface has its own default services
            //- We can leave the default services in their present state,
            //- or we can remove them and replace them with our own.
            //- Now add our own services using IServiceContainer
            //-
            //-
            //- Note
            //- before loading the root control in the design surface
            //- we must add an instance of naming service to the service container.
            //- otherwise the root component did not have a name and this caused
            //- troubles when we try to use the UndoEngine
            //-
            //-
            //- 1. NameCreationService
            _nameCreationService = new NameCreationServiceImp();
            if (_nameCreationService != null)
            {
                this.ServiceContainer.RemoveService(typeof(INameCreationService), false);
                this.ServiceContainer.AddService(typeof(INameCreationService), _nameCreationService);
            }
            //-
            //-
            //- 2. CodeDomComponentSerializationService
            _codeDomComponentSerializationService = new CodeDomComponentSerializationService(this.ServiceContainer);
            if (_codeDomComponentSerializationService != null)
            {
                //- the CodeDomComponentSerializationService is ready to be replaced
                this.ServiceContainer.RemoveService(typeof(ComponentSerializationService), false);
                this.ServiceContainer.AddService(typeof(ComponentSerializationService), _codeDomComponentSerializationService);
            }
            //-
            //-
            //- 3. IDesignerSerializationService
            _designerSerializationService = new DesignerSerializationServiceImpl(this.ServiceContainer);
            if (_designerSerializationService != null)
            {
                //- the IDesignerSerializationService is ready to be replaced
                this.ServiceContainer.RemoveService(typeof(IDesignerSerializationService), false);
                this.ServiceContainer.AddService(typeof(IDesignerSerializationService), _designerSerializationService);
            }


            //-
            //-
            //- 4. UndoEngine
            _undoEngine = new UndoEngineExt(this.ServiceContainer);
            //- disable the UndoEngine
            _undoEngine.Enabled = false;
            if (_undoEngine != null)
            {
                //- the UndoEngine is ready to be replaced
                this.ServiceContainer.RemoveService(typeof(UndoEngine), false);
                this.ServiceContainer.AddService(typeof(UndoEngine), _undoEngine);
            }
            //-
            //-
            //- 5. IMenuCommandService
            this.ServiceContainer.AddService(typeof(IMenuCommandService), new MenuCommandService(this));

            _toolboxService = new ToolboxServiceImp(this.GetIDesignerHost());
            if (_toolboxService != null)
            {
                this.ServiceContainer.RemoveService(typeof(IToolboxService), false);
                this.ServiceContainer.AddService(typeof(IToolboxService), _toolboxService);
            }

        }

        private ToolboxServiceImp _toolboxService = null;

        public ToolboxServiceImp GetIToolboxService()
        {
            return (ToolboxServiceImp)this.GetService(typeof(IToolboxService));
        }




        public void EnableDragandDrop()
        {
            // For the management of the drag and drop of the toolboxItems
            Control ctrl = this.GetView();
            if (null == ctrl)
                return;
            ctrl.AllowDrop = true;
            ctrl.DragDrop += new DragEventHandler(OnDragDrop);

            //- enable the Dragitem inside the our Toolbox
            ToolboxServiceImp tbs = this.GetIToolboxService();
            if (null == tbs)
                return;
            if (null == tbs.Toolbox)
                return;
            tbs.Toolbox.MouseDown += new MouseEventHandler(OnListboxMouseDown);
        }


        //- Management of the Drag&Drop of the toolboxItems contained inside our Toolbox
        private void OnListboxMouseDown(object sender, MouseEventArgs e)
        {
            ToolboxServiceImp tbs = this.GetIToolboxService();
            if (null == tbs)
                return;
            if (null == tbs.Toolbox)
                return;
            //if (null == tbs.Toolbox())
            //    return;

            //var tool = tbs.Toolbox;

            //if (tool == null)
            //    return;

            //var hit = tool.CalcHitInfo(e.X, e.Y);
            //if (hit.RowHandle < 0)
            //    return;

            //tbs.Toolbox.DoDragDrop(tool.GetRow(hit.RowHandle), DragDropEffects.Copy | DragDropEffects.Move);
        }

        //- Management of the drag and drop of the toolboxItems
        public void OnDragDrop(object sender, DragEventArgs e)
        {
            //- if the user don't drag a ToolboxItem 
            //- then do nothing
            if (!e.Data.GetDataPresent(typeof(ToolboxItem)))
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            //- now retrieve the data node
            ToolboxItem item = e.Data.GetData(typeof(ToolboxItem)) as ToolboxItem;
            e.Effect = DragDropEffects.Copy;
            item.CreateComponents(this.GetIDesignerHost());

        }


        //- do some Edit menu command using the MenuCommandService
        public void DoAction(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            IMenuCommandService ims = this.GetService(typeof(IMenuCommandService)) as IMenuCommandService;

            try
            {
                switch (command.ToUpper())
                {
                    case "CUT":
                        ims.GlobalInvoke(StandardCommands.Cut);
                        break;
                    case "COPY":
                        ims.GlobalInvoke(StandardCommands.Copy);
                        break;
                    case "PASTE":
                        ims.GlobalInvoke(StandardCommands.Paste);
                        break;
                    case "DELETE":
                        ims.GlobalInvoke(StandardCommands.Delete);
                        break;
                    case "MOVEUP":
                        ims.GlobalInvoke(MenuCommands.KeyMoveUp);
                        break;
                    case "MOVERIGHT":
                        ims.GlobalInvoke(MenuCommands.KeyMoveRight);
                        break;
                    default:
                        // do nothing;


                        break;
                }//end_switch
            }//end_try
            catch (Exception ex)
            {
                throw new Exception("DoAction() - Exception: error in performing the action: " + command + "(see Inner Exception)", ex);
            }//end_catch
        }

        public void DoCommandID(CommandID command)
        {
            IMenuCommandService ims = this.GetService(typeof(IMenuCommandService)) as IMenuCommandService;

            try
            {
                ims.GlobalInvoke(command);
            }//end_try
            catch (Exception ex)
            {
                throw new Exception("DoAction() - Exception: error in performing the action: " + command + "(see Inner Exception)", ex);
            }
        }

        public IComponent CreateRootComponent(Type controlType, Size controlSize)
        {
            try
            {

                //- step.1
                //- get the IDesignerHost
                //- if we are not not able to get it 
                //- then rollback (return without do nothing)
                IDesignerHost host = GetIDesignerHost();
                if (null == host) return null;
                //- check if the root component has already been set
                //- if so then rollback (return without do nothing)
                if (null != host.RootComponent) return null;
                //-
                //-
                //- step.2
                //- create a new root component and initialize it via its designer
                //- if the component has not a designer
                //- then rollback (return without do nothing)
                //- else do the initialization
                this.BeginLoad(controlType);
                if (this.LoadErrors.Count > 0)
                    throw new Exception("the BeginLoad() failed! Some error during " + controlType.ToString() + " loding");
                //-
                //-
                //- step.3
                //- try to modify the Size of the object just created
                IDesignerHost ihost = GetIDesignerHost();
                //- Set the backcolor and the Size
                Control ctrl = null;
                Type hostType = host.RootComponent.GetType();
                if (hostType == typeof(Form))
                {
                    ctrl = this.View as Control;
                    ctrl.BackColor = Color.LightGray;
                    //- set the Size
                    PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(ctrl);
                    //- Sets a PropertyDescriptor to the specific property.
                    PropertyDescriptor pdS = pdc.Find("Size", false);
                    if (null != pdS)
                        pdS.SetValue(ihost.RootComponent, controlSize);
                }
                else if (hostType == typeof(UserControl))
                {
                    ctrl = this.View as Control;
                    ctrl.BackColor = Color.DarkGray;
                    //- set the Size
                    PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(ctrl);
                    //- Sets a PropertyDescriptor to the specific property.
                    PropertyDescriptor pdS = pdc.Find("Size", false);
                    if (null != pdS)
                        pdS.SetValue(ihost.RootComponent, controlSize);
                }
                else if (hostType == typeof(Component))
                {
                    ctrl = this.View as Control;
                    ctrl.BackColor = Color.White;
                    //- don't set the Size
                }
                else
                {
                    throw new Exception("Undefined Host Type: " + hostType.ToString());
                }

                return ihost.RootComponent;
            }//end_try
            catch (Exception ex)
            {
                throw new Exception("CreateRootComponent() - Exception: (see Inner Exception)", ex);
            }//end_catch
        }

        public Control CreateControl(Type controlType, Size controlSize, Point controlLocation)
        {
            try
            {
                //- step.1
                //- get the IDesignerHost
                //- if we are not able to get it 
                //- then rollback (return without do nothing)
                IDesignerHost host = GetIDesignerHost();
                if (null == host) return null;
                //- check if the root component has already been set
                //- if not so then rollback (return without do nothing)
                if (null == host.RootComponent) return null;
                //-
                //-
                //- step.2
                //- create a new component and initialize it via its designer
                //- if the component has not a designer
                //- then rollback (return without do nothing)
                //- else do the initialization
                IComponent newComp = host.CreateComponent(controlType);

                if (null == newComp) return null;
                IDesigner designer = host.GetDesigner(newComp);
                if (null == designer) return null;
                if (designer is IComponentInitializer)
                    ((IComponentInitializer)designer).InitializeNewComponent(null);
                //-
                //-
                //- step.3
                //- try to modify the Size/Location of the object just created
                PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(newComp);
                //- Sets a PropertyDescriptor to the specific property.
                PropertyDescriptor pdS = pdc.Find("Size", false);
                if (null != pdS)
                    pdS.SetValue(newComp, controlSize);
                PropertyDescriptor pdL = pdc.Find("Location", false);
                if (null != pdL)
                    pdL.SetValue(newComp, controlLocation);
                //-
                //-
                //- step.4
                //- commit the Creation Operation
                //- adding the control to the DesignSurface's root component
                //- and return the control just created to let further initializations
                ((Control)newComp).Parent = host.RootComponent as Control;
                return newComp as Control;
            }//end_try
            catch (Exception ex)
            {
                throw new Exception("CreateControl() - Exception: (see Inner Exception)", ex);
            }//end_catch
        }

        public IDesignerHost GetIDesignerHost()
        {
            return (IDesignerHost)(this.GetService(typeof(IDesignerHost)));
        }

        public Control GetView()
        {
            Control ctrl = this.View as Control;
            ctrl.Dock = DockStyle.Fill;
            return ctrl;
        }
    }

    internal class SMDesignerOptions : DesignerOptionService
    {
        public SMDesignerOptions() : base() { }

        protected override void PopulateOptionCollection(DesignerOptionCollection options)
        {
            if (null != options.Parent) return;

            DesignerOptions ops = new DesignerOptions();
            ops.GridSize = new Size(8, 8);
            ops.SnapToGrid = false;
            ops.ShowGrid = true;
            ops.UseSnapLines = false;
            ops.UseSmartTags = true;
            DesignerOptionCollection wfd = this.CreateOptionCollection(options, "WindowsFormsDesigner", null);
            this.CreateOptionCollection(wfd, "General", ops);
        }
    }//end_class

    internal class NameCreationServiceImp : INameCreationService
    {

        public NameCreationServiceImp() { }

        public string CreateName(IContainer container, Type type)
        {
            if (null == container)
                return string.Empty;


            ComponentCollection cc = container.Components;
            int min = Int32.MaxValue;
            int max = Int32.MinValue;
            int count = 0;

            int i = 0;
            while (i < cc.Count)
            {
                Component comp = cc[i] as Component;

                if (comp.GetType() == type)
                {
                    count++;

                    string name = comp.Site.Name;
                    if (name.StartsWith(type.Name))
                    {
                        try
                        {
                            int value = Int32.Parse(name.Substring(type.Name.Length));
                            if (value < min) min = value;
                            if (value > max) max = value;
                        }
                        catch (Exception) { }
                    }//end_if
                }//end_if
                i++;
            } //end_while

            if (0 == count)
            {
                return type.Name + "1";
            }
            else if (min > 1)
            {
                int j = min - 1;
                return type.Name + j.ToString();
            }
            else
            {
                int j = max + 1;
                return type.Name + j.ToString();
            }


        }

        public bool IsValidName(string name)
        {
            //- Check that name is "something" and that is a string with at least one char
            if (String.IsNullOrEmpty(name))
                return false;

            //- then the first character must be a letter
            if (!(char.IsLetter(name, 0)))
                return false;

            //- then don't allow a leading underscore
            if (name.StartsWith("_"))
                return false;

            //- ok, it's a valid name
            return true;
        }

        public void ValidateName(string name)
        {
            //-  Use our existing method to check, if it's invalid throw an exception
            if (!(IsValidName(name)))
                throw new ArgumentException("Invalid name: " + name);
        }

    }//end_class

    public class UndoEngineExt : UndoEngine
    {
        private string _Name_ = "UndoEngineExt";

        private Stack<UndoEngine.UndoUnit> undoStack = new Stack<UndoEngine.UndoUnit>();
        private Stack<UndoEngine.UndoUnit> redoStack = new Stack<UndoEngine.UndoUnit>();

        public UndoEngineExt(IServiceProvider provider) : base(provider) { }


        public bool EnableUndo
        {
            get { return undoStack.Count > 0; }
        }

        public bool EnableRedo
        {
            get { return redoStack.Count > 0; }
        }

        public void Undo()
        {
            if (undoStack.Count > 0)
            {
                try
                {
                    UndoEngine.UndoUnit unit = undoStack.Pop();
                    unit.Undo();
                    redoStack.Push(unit);
                    //Log("::Undo - undo action performed: " + unit.Name);
                }
                catch (Exception ex)
                {
                    //Log("::Undo() - Exception " + ex.Message + " (line:" + new StackFrame(true).GetFileLineNumber() + ")");
                }
            }
            else
            {
                //Log("::Undo - NO undo action to perform!");
            }
        }

        public void Redo()
        {
            if (redoStack.Count > 0)
            {
                try
                {
                    UndoEngine.UndoUnit unit = redoStack.Pop();
                    unit.Undo();
                    undoStack.Push(unit);
                    //Log("::Redo - redo action performed: " + unit.Name);
                }
                catch (Exception ex)
                {
                    //Log("::Redo() - Exception " + ex.Message + " (line:" + new StackFrame(true).GetFileLineNumber() + ")");
                }
            }
            else
            {
                //Log("::Redo - NO redo action to perform!");
            }
        }


        protected override void AddUndoUnit(UndoEngine.UndoUnit unit)
        {
            undoStack.Push(unit);
        }


    }//end_class

    public class DesignerSerializationServiceImpl : IDesignerSerializationService
    {

        private IServiceProvider _serviceProvider;

        public DesignerSerializationServiceImpl(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public System.Collections.ICollection Deserialize(object obj)
        {
            // string data = File.ReadAllText("save.txt");
            MemoryStream fs = obj as MemoryStream;
            if (fs != null)
            {
                ComponentSerializationService componentSerializationService = _serviceProvider.GetService(typeof(ComponentSerializationService)) as ComponentSerializationService;

                SerializationStore serializationStore = componentSerializationService.LoadStore(fs);
                ICollection collection = componentSerializationService.Deserialize(serializationStore);
                fs.Close();
                return collection;
            }
            else
            {
                SerializationStore serializationStore = obj as SerializationStore;

                if (serializationStore != null)
                {

                    ComponentSerializationService componentSerializationService = _serviceProvider.GetService(typeof(ComponentSerializationService)) as ComponentSerializationService;
                    ICollection collection = componentSerializationService.Deserialize(serializationStore);
                    return collection;
                }
            }


            return new object[] { };
        }

        public object Serialize(System.Collections.ICollection objects)
        {
            ComponentSerializationService componentSerializationService = _serviceProvider.GetService(typeof(ComponentSerializationService)) as ComponentSerializationService;
            SerializationStore returnObject = null;
            using (SerializationStore serializationStore = componentSerializationService.CreateStore())
            {
                foreach (object obj in objects)
                {
                    if (obj is Control control)
                    {
                        //if (control is SMGroup group)
                        //{
                        //    foreach (object obj2 in control.Controls)
                        //    {
                        //        componentSerializationService.Serialize(serializationStore, obj2);
                        //    }
                        //}
                        //if(control is SplitContainerControl split)
                        //{
                        //    componentSerializationService.SerializeAbsolute(serializationStore, split.Panel1.Controls[0]);
                        //    componentSerializationService.SerializeAbsolute(serializationStore, split.Panel2.Controls[0]);
                        //}
                        componentSerializationService.SerializeAbsolute(serializationStore, obj);
                    }
                    returnObject = serializationStore;


                }
            }
            return returnObject;
        }

    }//end_class

    //- It is the gateway between the toolbox user interface 
    //- in the development environment and the designers. 
    //- The designers constantly query the toolbox when the 
    //- cursor is  over them to get feedback about the selected control
    //-
    //- NOTE:
    //- this is a lightweight class!
    //- this class implements the interface IToolboxService 
    //- it does NOT create a ListBox, it merely links one
    //- which is created by user and then referenced by 
    //- the ToolboxServiceImp::Toolbox property
    //-
    public class ToolboxServiceImp : IToolboxService
    {


        public IDesignerHost DesignerHost { get; private set; }

        //- our real Toolbox
        public DataGrid Toolbox { get; set; }


        //- ctor
        public ToolboxServiceImp(IDesignerHost host)
        {
            this.DesignerHost = host;

            //- Our MainForm adds our ToolboxPane to the DesignerHost's services.
            Toolbox = null;
        }






        //- Add a creator that will convert non-standard tools in the specified format into ToolboxItems, to be associated with a host.
        void IToolboxService.AddCreator(ToolboxItemCreatorCallback creator, string format, IDesignerHost host)
        {
            // UNIMPLEMENTED - We aren't handling any non-standard tools here. Our toolset is constant.
            //throw new NotImplementedException();
        }

        //- Add a creator that will convert non-standard tools in the specified format into ToolboxItems.
        void IToolboxService.AddCreator(ToolboxItemCreatorCallback creator, string format)
        {
            // UNIMPLEMENTED - We aren't handling any non-standard tools here. Our toolset is constant.
            //throw new NotImplementedException();
        }

        //- Add a ToolboxItem to our toolbox, in a specific category, bound to a certain host.
        void IToolboxService.AddLinkedToolboxItem(ToolboxItem toolboxItem, string category, IDesignerHost host)
        {
            // UNIMPLEMENTED - We didn't end up doing a project system, so there's no need
            // to add custom tools (despite that we do have a tab for such tools).
            //throw new NotImplementedException();
        }

        //- Add a ToolboxItem to our toolbox, bound to a certain host.
        void IToolboxService.AddLinkedToolboxItem(ToolboxItem toolboxItem, IDesignerHost host)
        {
            // UNIMPLEMENTED - We didn't end up doing a project system, so there's no need
            // to add custom tools (despite that we do have a tab for such tools).
            //throw new NotImplementedException();
        }

        //- Add a ToolboxItem to our toolbox under the specified category.
        void IToolboxService.AddToolboxItem(ToolboxItem toolboxItem, string category)
        {
            //- we have no category 
            ((IToolboxService)this).AddToolboxItem(toolboxItem);
        }

        //- Add a ToolboxItem to our Toolbox.
        void IToolboxService.AddToolboxItem(ToolboxItem toolboxItem)
        {

            //var test = new DevExpress.XtraToolbox.ToolboxItem() { Caption = toolboxItem.DisplayName, Tag = toolboxItem };
            //test.ImageOptions.Image = toolboxItem.Bitmap;
            //   Toolbox.Items.Add(toolboxItem);
            //   Toolbox.Templates[0].Elements[1].Image = toolboxItem.Bitmap;

        }


        //- Our toolbox has categories akin to those of Visual Studio, but you
        //- could group them any which way. Just make sure your IToolboxService knows.
        CategoryNameCollection IToolboxService.CategoryNames
        {
            get
            {
                return null;
            }
        }

        //- necessary for the Drag&Drop
        //- We deserialize a ToolboxItem when we drop it onto our design surface.
        //- The ToolboxItem comes packaged in a DataObject. We're just working
        //- with standard tools and one host, so the host parameter is ignored.
        ToolboxItem IToolboxService.DeserializeToolboxItem(object serializedObject, IDesignerHost host)
        {
            return ((IToolboxService)this).DeserializeToolboxItem(serializedObject);
        }

        //- We deserialize a ToolboxItem when we drop it onto our design surface.
        //- The ToolboxItem comes packaged in a DataObject.
        ToolboxItem IToolboxService.DeserializeToolboxItem(object serializedObject)
        {
            return (ToolboxItem)((DataObject)serializedObject).GetData(typeof(ToolboxItem));
        }

        //- Return the selected ToolboxItem in our toolbox if it is associated with this host.
        //- Since all of our tools are associated with our only host, the host parameter
        //- is ignored.
        ToolboxItem IToolboxService.GetSelectedToolboxItem(IDesignerHost host)
        {
            return ((IToolboxService)this).GetSelectedToolboxItem();
        }

        //- Return the selected ToolboxItem in our Toolbox
        ToolboxItem IToolboxService.GetSelectedToolboxItem()
        {



            //var tool = Toolbox.MainView as GridView;

            //if (null == tool || tool.GetFocusedRow() == null)
            //    return null;

            //if (tool.GetSelectedRows().Count() < 1)
            //    return null;

            //ToolboxItem tbItem = (ToolboxItem)tool.GetFocusedRow();
            //if (tbItem.DisplayName.ToUpper().Contains("POINTER"))
            //    return null;


            //return tbItem;
            return null;
        }

        //-  Get all the tools in a category
        ToolboxItemCollection IToolboxService.GetToolboxItems(string category, IDesignerHost host)
        {
            //- we have no category
            return ((IToolboxService)this).GetToolboxItems();
        }


        //- Get all of the tools.
        ToolboxItemCollection IToolboxService.GetToolboxItems(string category)
        {
            //- we have no category
            return ((IToolboxService)this).GetToolboxItems();
        }


        //- Get all of the tools. We're always using our current host though.
        ToolboxItemCollection IToolboxService.GetToolboxItems(IDesignerHost host)
        {
            return ((IToolboxService)this).GetToolboxItems();
        }


        //- Get all of the tools
        ToolboxItemCollection IToolboxService.GetToolboxItems()
        {
            if (null == Toolbox) return null;

            List<ToolboxItem> temp = new List<ToolboxItem>();

            foreach (ToolboxItem item in Toolbox.DataSource as ToolboxItemCollection)
            {
                temp.Add(item);
            }

            //ToolboxItem[] arr = new ToolboxItem[Toolbox.Groups[0].Items.Count];
            //Toolbox.Groups[0].Items.CopyTo(arr, 0);

            return new ToolboxItemCollection(temp.ToArray());
        }

        //- We are always using standard ToolboxItems, so they are always supported
        bool IToolboxService.IsSupported(object serializedObject, ICollection filterAttributes)
        {
            return true;
        }

        //- We are always using standard ToolboxItems, so they are always supported
        bool IToolboxService.IsSupported(object serializedObject, IDesignerHost host)
        {
            return true;
        }

        //- Check if a serialized object is a ToolboxItem. In our case, all of our tools
        //- are standard and from a constant set, and they all extend ToolboxItem, so if
        //- we can deserialize it in our standard-way, then it is indeed a ToolboxItem
        //- The host is ignored
        bool IToolboxService.IsToolboxItem(object serializedObject, IDesignerHost host)
        {
            return ((IToolboxService)this).IsToolboxItem(serializedObject);
        }



        //- Check if a serialized object is a ToolboxItem. In our case, all of our tools
        //- are standard and from a constant set, and they all extend ToolboxItem, so if
        //- we can deserialize it in our standard-way, then it is indeed a ToolboxItem
        bool IToolboxService.IsToolboxItem(object serializedObject)
        {
            //- If we can deserialize it, it's a ToolboxItem.
            if (((IToolboxService)this).DeserializeToolboxItem(serializedObject) != null)
                return true;

            return false;
        }


        //- Refreshes the Toolbox
        void IToolboxService.Refresh()
        {
            Toolbox.Refresh();
        }

        //- Remove the creator for the specified format, associated with a particular host.
        void IToolboxService.RemoveCreator(string format, IDesignerHost host)
        {
            // UNIMPLEMENTED - We aren't handling any non-standard tools here. Our toolset is constant.
            //throw new NotImplementedException();
        }

        //- Remove the creator for the specified format.
        void IToolboxService.RemoveCreator(string format)
        {
            // UNIMPLEMENTED - We aren't handling any non-standard tools here. Our toolset is constant.
            //throw new NotImplementedException();
        }

        //- Remove a ToolboxItem from the specified category in our Toolbox.
        void IToolboxService.RemoveToolboxItem(ToolboxItem toolboxItem, string category)
        {
            ((IToolboxService)this).RemoveToolboxItem(toolboxItem);
        }

        //- Remove a ToolboxItem from our Toolbox.
        void IToolboxService.RemoveToolboxItem(ToolboxItem toolboxItem)
        {
            if (null == Toolbox) return;

            //Toolbox.SelectedIndex = -1;
            //Toolbox.Items.Remove(toolboxItem);
        }


        //- If your toolbox is categorized, then it's good for others to know
        //- which category is selected.
        string IToolboxService.SelectedCategory
        {
            get
            {
                return null;
            }
            set
            {
                // UNIMPLEMENTED 
            }
        }


        //- This gets called after our IToolboxUser (the designer) ToolPicked method is called.
        //- In our case, we select the pointer. 
        void IToolboxService.SelectedToolboxItemUsed()
        {
            if (null == Toolbox) return;

            //var tool = Toolbox.MainView as GridView;
            ////Toolbox.SelectedItem = null;
            ////  tool.FocusedRowHandle = -1;
            //tool.ClearSelection();
        }


        //- Serialize the toolboxItem necessary for the Drag&Drop
        //- We serialize a toolbox by packaging it in a DataObject
        object IToolboxService.SerializeToolboxItem(ToolboxItem toolboxItem)
        {
            //return new DataObject(typeof( ToolboxItem), toolboxItem );
            DataObject dataObject = new DataObject();
            dataObject.SetData(typeof(ToolboxItem), toolboxItem);
            return dataObject;
        }

        //- If we've got a tool selected, then perhaps we want to set our cursor to do
        //- something interesting when its over the design surface. If we do, then
        //- we do it here and return true. Otherwise we return false so the caller
        //- can set the cursor in some default manor.
        bool IToolboxService.SetCursor()
        {
            if (Toolbox == null)
                return false;

            //var tool = Toolbox.MainView as GridView;

            //if (null == tool || null == tool.GetFocusedRow())
            //    return false;


            ////- <Pointer> is not a tool
            //ToolboxItem tbItem = (ToolboxItem)tool.GetFocusedRow();
            //if (tbItem.DisplayName.ToUpper().Contains("POINTER"))
            //    return false;


            //if (tool.GetSelectedRows().Count() > 0)
            //{
            //    Cursor.Current = Cursors.Cross;
            //    return true;
            //}

            return false;
        }

        //- Set the selected ToolboxItem in our Toolbox.
        void IToolboxService.SetSelectedToolboxItem(ToolboxItem toolboxItem)
        {
            if (null == Toolbox)
                return;

            //// Toolbox.SelectedItem = toolboxItem;
            //var tool = Toolbox.MainView as GridView;
            ////Toolbox.SelectedItem = null;
            //tool.SetFocusedValue(toolboxItem);
        }




    }

}
