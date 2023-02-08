using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IBIMTool.Core;
using IBIMTool.CutOpening;
using IBIMTool.RevitEventHandlers;
using IBIMTool.RevitExtensions;
using IBIMTool.RevitModels;
using IBIMTool.RevitUtils;
using IBIMTool.Services;
using IBIMTool.Views;
using Microsoft.Extensions.DependencyInjection;
using Revit.Async;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Document = Autodesk.Revit.DB.Document;


namespace IBIMTool.ViewModels
{
    public sealed class CutHoleDataViewModel : ObservableObject, IDisposable
    {
        private Document doc = null;
        public CutHoleDockPaneView DockPanelView { get; set; }
        public static ExternalEvent RevitExternalEvent { get; set; }

        private string selectedId = Properties.Settings.Default.OpeningUId;
        private static readonly string localPath = IBIMToolHelper.LocalPath;
        private static readonly IServiceProvider services = IBIMToolApp.Host.Services;
        private string activeDocumentTitle = Properties.Settings.Default.ActiveDocumentTitle;
        private readonly TaskScheduler taskContext = TaskScheduler.FromCurrentSynchronizationContext();
        private readonly CutHoleCollisionManager collisionMng = services.GetRequiredService<CutHoleCollisionManager>();


        public CutHoleDataViewModel(SettingsEventHandler eventHandler)
        {
            RevitExternalEvent = ExternalEvent.Create(eventHandler);
            RefreshDataCommand = new RelayCommand(RefreshActiveDataHandler);
            ShowModelCommand = new AsyncRelayCommand(ShowHandelCommandAsync);
        }


        #region Visibility

        private bool started = false;
        public bool IsStarted
        {
            get => started;
            set
            {
                if (SetProperty(ref started, value) && started)
                {
                    activeDocumentTitle = Properties.Settings.Default.ActiveDocumentTitle;
                    DockPanelView.ActiveTitle.Content = activeDocumentTitle;
                    DockPanelView.UpdateLayout();
                    RetrieveAllDocuments();
                }
            }
        }


        private bool enabled = false;
        public bool IsOptionEnabled
        {
            get => enabled;
            set
            {
                if (SetProperty(ref enabled, value) && enabled)
                {
                    IsOptionCompleted = true;
                    GetCoreMaterialsToData();
                    GetFamilySymbols();
                }
            }
        }


        private bool сompleted = false;
        public bool IsOptionCompleted
        {
            get => сompleted;
            set
            {
                if (started && documents != null && material != null)
                {
                    сompleted = SetProperty(ref сompleted, value);
                }
            }
        }


        private bool refresh = false;
        public bool IsDataRetrieved
        {
            get => refresh;
            set
            {
                if (SetProperty(ref refresh, value) && refresh)
                {
                    SnoopIntersectionByInputData();
                }
            }
        }

        #endregion


        #region GeneralData

        private Material material = null;
        public Material SelectedMaterial
        {
            get => material;
            set
            {
                if (SetProperty(ref material, value) && material != null)
                {
                    CommandManager.InvalidateRequerySuggested();
                    IsOptionCompleted = true;
                }
            }
        }


        private IList<DocumentModel> documents = null;
        public IList<DocumentModel> DocumentCollection
        {
            get => documents;
            set
            {
                if (SetProperty(ref documents, value) && documents != null)
                {
                    collisionMng.DocumentModelList = documents;
                }
            }
        }


        private IDictionary<string, Material> materials = null;
        public IDictionary<string, Material> StructureMaterials
        {
            get => materials;
            set => SetProperty(ref materials, value);
        }

        #endregion


        #region FamilySymbols

        private FamilySymbol symbol;
        public FamilySymbol SelectedSymbol
        {
            get => symbol;
            set
            {
                if (SetProperty(ref symbol, value) && symbol != null)
                {
                    selectedId = symbol.UniqueId;
                    GetFamilySymbolParameterData(symbol);
                    if (selectedId != Properties.Settings.Default.OpeningUId)
                    {
                        Properties.Settings.Default.OpeningUId = selectedId;
                        Properties.Settings.Default.Save();
                    }
                }
            }
        }


        private IList<FamilySymbol> symbols;
        public IList<FamilySymbol> SymbolList
        {
            get => symbols;
            set
            {
                if (SetProperty(ref symbols, value) && symbols != null)
                {
                    if (!string.IsNullOrWhiteSpace(selectedId))
                    {
                        for (int i = 0; i < symbols.Count; i++)
                        {
                            FamilySymbol symbol = symbols[i];
                            if (selectedId.Equals(symbol.UniqueId))
                            {
                                SelectedSymbol = symbol;
                            }
                        }
                    }
                }
            }
        }


        public async void LoadFamilyAsync(string familyPath)
        {
            SymbolList = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return LoadFamilySymbols(doc, familyPath);
            });
        }


        private string[] ProcessDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                IBIMLogger.Error("Not found directory: " + directory);
            }
            return Directory.GetFiles(directory, "*.rfa", SearchOption.TopDirectoryOnly);
        }


        private IList<FamilySymbol> LoadFamilySymbols(Document doc, string familyPath)
        {
            IList<FamilySymbol> result = new List<FamilySymbol>();
            Family family = RevitFamilyManager.LoadFamily(ref doc, familyPath);
            if (family != null && activeDocumentTitle.Equals(doc.Title))
            {
                Document familyDoc = doc.EditFamily(family);
                familyPath = $@"{localPath}\{family.Name}.rfa";
                if (File.Exists(familyPath)) { File.Delete(familyPath); }
                result = RevitFamilyManager.GetFamilySymbols(ref doc, family);
                familyDoc.SaveAs(familyPath, new SaveAsOptions
                {
                    OverwriteExistingFile = true,
                    MaximumBackups = 3,
                    Compact = true,
                });
                if (!familyDoc.Close(false))
                {
                    IBIMLogger.Error(family.Name);
                }
            }
            return result;
        }

        #endregion


        #region ParameterData

        private string widthParam = Properties.Settings.Default.WidthValParam;
        public string WidthParamName
        {
            get => widthParam;
            set
            {
                if (SetProperty(ref widthParam, value) && !string.IsNullOrEmpty(widthParam))
                {
                    Properties.Settings.Default.WidthValParam = widthParam;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private string hightParam = Properties.Settings.Default.HightValParam;
        public string HightParamName
        {
            get => hightParam;
            set
            {
                if (SetProperty(ref hightParam, value) && !string.IsNullOrEmpty(hightParam))
                {
                    Properties.Settings.Default.HightValParam = hightParam;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private string elevation = Properties.Settings.Default.ElevateOfBaseParam;
        public string ElevationOfLevel
        {
            get => elevation;
            set
            {
                if (SetProperty(ref elevation, value) && !string.IsNullOrEmpty(elevation))
                {
                    Properties.Settings.Default.ElevateOfBaseParam = elevation;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private string levelElevat = Properties.Settings.Default.LevelElevationParam;
        public string LevelElevation
        {
            get => levelElevat;
            set
            {
                if (SetProperty(ref levelElevat, value) && !string.IsNullOrEmpty(levelElevat))
                {
                    Properties.Settings.Default.LevelElevationParam = levelElevat;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private string section = Properties.Settings.Default.ProjectSectionParam;
        public string SectionParamName
        {
            get => section;
            set
            {
                if (SetProperty(ref section, value) && !string.IsNullOrEmpty(section))
                {
                    Properties.Settings.Default.ProjectSectionParam = section;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private ICollectionView sharedParams;
        public ICollectionView SharedParameters
        {
            get => sharedParams;
            set => SetProperty(ref sharedParams, value);
        }


        internal async void GetFamilySymbolParameterData(FamilySymbol symbol)
        {
            Document familyDoc = doc.EditFamily(symbol.Family);
            if (null != familyDoc && familyDoc.IsFamilyDocument)
            {
                SharedParameters = await RevitTask.RunAsync(app =>
                {
                    IList<string> result = new List<string>(5);
                    FamilyManager familyManager = familyDoc.FamilyManager;
                    RevitFamilyManager.ActivateFamilySimbol(ref doc, symbol);
                    foreach (FamilyParameter param in familyManager.GetParameters())
                    {
                        if (!param.IsReadOnly && !param.IsDeterminedByFormula)
                        {
                            if (param.IsInstance && param.UserModifiable)
                            {
                                StorageType storage = param.StorageType;
                                if (storage.Equals(StorageType.Double))
                                {
                                    result.Add(param.Definition.Name);
                                }
                            }
                        }
                    }
                    return CollectionViewSource.GetDefaultView(result);
                });
            }
        }

        #endregion


        #region SizeData

        private int minSize = Properties.Settings.Default.MinSideSizeInMm;
        public int MinSideSize
        {
            get => minSize;
            set
            {
                if (SetProperty(ref minSize, value))
                {
                    Properties.Settings.Default.MinSideSizeInMm = minSize;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private int minDepth = Properties.Settings.Default.MinDepthSizeInMm;
        public int MinDepthSize
        {
            get => minDepth;
            set
            {
                if (SetProperty(ref minDepth, value))
                {
                    Properties.Settings.Default.MinDepthSizeInMm = minDepth;
                    Properties.Settings.Default.Save();
                }
            }
        }


        private int cutOffset = Properties.Settings.Default.CutOffsetInMm;
        public int CutOffsetSize
        {
            get => cutOffset;
            set
            {
                if (SetProperty(ref cutOffset, value))
                {
                    Properties.Settings.Default.CutOffsetInMm = cutOffset;
                    Properties.Settings.Default.Save();
                }
            }
        }

        #endregion


        #region AsyncMethods

        private void ClearAndResetData()
        {
            if (IsStarted)
            {
                Task task = Task.WhenAll();
                task = task.ContinueWith(_ =>
                {
                    IsStarted = false;
                    IsDataRetrieved = false;
                    IsOptionEnabled = false;
                    DocumentCollection = null;
                    StructureMaterials = null;
                    ElementModelData = null;
                    SymbolTextFilter = null;
                    LevelTextFilter = null;
                }, taskContext);
            }
        }


        public async void RetrieveAllDocuments()
        {
            DocumentCollection = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                collisionMng.InitializeElementTypeIdData(doc);
                return RevitFilterManager.GetDocumentCollection(doc);
            });
        }


        private async void GetCoreMaterialsToData()
        {
            StructureMaterials ??= await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                return collisionMng.GetStructureCoreMaterialData(doc);
            });
        }


        private async void GetFamilySymbols()
        {
            SymbolList = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                List<FamilySymbol> output = new List<FamilySymbol>(15);
                foreach (string familyPath in ProcessDirectory(localPath))
                {
                    string fileName = Path.GetFileNameWithoutExtension(familyPath);
                    FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Family));
                    Element element = collector.FirstOrDefault(x => x.Name == fileName);
                    if (element != null && element is Family family)
                    {
                        IBIMLogger.Info("Founded: " + family.Name);
                        output.AddRange(RevitFamilyManager.GetFamilySymbols(ref doc, family));
                    }
                    else
                    {
                        output.AddRange(LoadFamilySymbols(doc, familyPath));
                    }
                }
                return output;
            });
        }


        private async void SnoopIntersectionByInputData()
        {
            ElementModelData = await RevitTask.RunAsync(app =>
            {
                doc = app.ActiveUIDocument.Document;
                ObservableCollection<ElementModel> result = activeDocumentTitle.Equals(doc.Title)
                ? collisionMng.GetCollisionModelData(doc, material).ToObservableCollection()
                : new ObservableCollection<ElementModel>();
                return result;
            });
        }


        internal void ShowElementModelView(ElementModel model)
        {
            if (model != null && model.IsValidModel())
            {
                Task task = RevitTask.RunAsync(app =>
                {
                    doc = app.ActiveUIDocument.Document;
                    UIDocument uidoc = app.ActiveUIDocument;
                    if (activeDocumentTitle.Equals(doc.Title))
                    {
                        System.Windows.Clipboard.SetText(model.Element.Id.ToString());
                        uidoc.Selection.SetElementIds(new List<ElementId> { model.Element.Id });
                        RevitViewManager.ShowModelInPlanView(uidoc, model, ViewDiscipline.Mechanical);
                    }
                });
            }
        }

        #endregion


        #region DataGrid

        private bool? allSelected = false;
        public bool? AllSelectChecked
        {
            get => allSelected;
            set
            {
                if (SetProperty(ref allSelected, value))
                {
                    if (viewData != null && value.HasValue)
                    {
                        bool booleanValue = allSelected.Value;
                        foreach (ElementModel model in viewData)
                        {
                            model.IsSelected = booleanValue;
                        }
                    }
                }
            }
        }


        private ObservableCollection<ElementModel> modelData = null;
        public ObservableCollection<ElementModel> ElementModelData
        {
            get => modelData;
            set
            {
                if (SetProperty(ref modelData, value) && modelData != null)
                {
                    ViewDataGridCollection = new ListCollectionView(modelData);
                    UniqueLevelNames = new SortedSet<string>(modelData.Select(m => m.LevelName).Append(string.Empty)).ToList();
                    UniqueSymbolNames = new SortedSet<string>(modelData.Select(m => m.SymbolName).Append(string.Empty)).ToList();
                }
            }
        }


        private ListCollectionView viewData = null;
        public ListCollectionView ViewDataGridCollection
        {
            get => viewData;
            set
            {
                if (SetProperty(ref viewData, value))
                {
                    AllSelectChecked = false;
                    ReviewDataViewCollection();
                    VerifySelectDataViewCollection();
                }
            }
        }


        private void ReviewDataViewCollection()
        {
            if (viewData != null && !viewData.IsEmpty)
            {
                using (viewData.DeferRefresh())
                {
                    viewData.SortDescriptions.Clear();
                    viewData.GroupDescriptions.Clear();
                    viewData.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ElementModel.HostCategory)));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.FamilyName), ListSortDirection.Ascending));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.IsSelected), ListSortDirection.Descending));
                    viewData.SortDescriptions.Add(new SortDescription(nameof(ElementModel.MidSize), ListSortDirection.Ascending));
                    viewData.Filter = FilterModelCollection;
                }
            }
        }


        internal void VerifySelectDataViewCollection()
        {
            if (viewData != null && !viewData.IsEmpty)
            {
                object currentItem = viewData.GetItemAt(0);
                if (currentItem is ElementModel model)
                {
                    IEnumerable<ElementModel> items = viewData.OfType<ElementModel>();
                    AllSelectChecked = items.All(x => x.IsSelected == model.IsSelected) ? model.IsSelected : (bool?)null;
                }
            }
        }

        #endregion


        #region DataFilter

        private string levelText;
        public string LevelTextFilter
        {
            get => levelText;
            set
            {
                if (SetProperty(ref levelText, value))
                {
                    ViewDataGridCollection.Filter = FilterModelCollection;
                    if (!string.IsNullOrEmpty(levelText))
                    {
                        ActivatePlanViewByLevel();
                        AllSelectChecked = false;
                    }
                }
            }
        }


        private string symbolText;
        public string SymbolTextFilter
        {
            get => symbolText;
            set
            {
                if (SetProperty(ref symbolText, value))
                {
                    ViewDataGridCollection.Filter = FilterModelCollection;
                    if (!string.IsNullOrEmpty(symbolText))
                    {
                        ActivatePlanViewByLevel();
                        AllSelectChecked = false;
                    }
                }
            }
        }


        private IList<string> levelNames = null;
        public IList<string> UniqueLevelNames
        {
            get => levelNames;
            set => SetProperty(ref levelNames, value);
        }


        private IList<string> symbolNames = null;
        public IList<string> UniqueSymbolNames
        {
            get => symbolNames;
            set => SetProperty(ref symbolNames, value);
        }


        private bool FilterModelCollection(object obj)
        {
            return obj is ElementModel model
            && model.SymbolName.Equals(symbolText)
            && model.LevelName.Equals(levelText);
        }


        private void ActivatePlanViewByLevel()
        {
            if (viewData != null && !viewData.IsEmpty)
            {
                object currentItem = viewData.GetItemAt(0);
                if (currentItem is ElementModel model)
                {
                    Task task = RevitTask.RunAsync(app =>
                    {
                        Level level = model.HostLevel;
                        UIDocument uidoc = app.ActiveUIDocument;
                        ViewPlan view = RevitViewManager.GetPlanByLevel(uidoc, level);
                        RevitViewManager.ActivateView(uidoc, view, ViewDiscipline.Mechanical);
                    });
                }
            }
        }

        #endregion


        #region RefreshDataCommand

        public ICommand RefreshDataCommand { get; private set; }
        private void RefreshActiveDataHandler()
        {
            IsDataRetrieved = false;
            Task task = Task.WhenAll();
            task = task.ContinueWith(_ =>
            {
                IsDataRetrieved = true;
                IsOptionEnabled = false;
                IsOptionCompleted = false;
            }, taskContext);
        }

        #endregion


        #region ShowCollisionCommand

        public ICommand ShowModelCommand { get; private set; }
        private async Task ShowHandelCommandAsync()
        {
            await RevitTask.RunAsync(app =>
            {
                if (ViewDataGridCollection.IsInUse)
                {
                    if (activeDocumentTitle.Equals(doc.Title))
                    {
                        ViewDataGridCollection.Refresh();
                        doc = app.ActiveUIDocument.Document;
                        PreviewViewModel previewVM = services.GetRequiredService<PreviewViewModel>();
                        foreach (object current in ViewDataGridCollection)
                        {
                            if (current is ElementModel model && model.IsValidModel())
                            {
                                if (model.IsSelected && ElementModelData.Remove(model))
                                {
                                    if (previewVM.ShowPreviewModel(app, model)) { break; }
                                }
                            }
                        }
                    }
                }
            });
        }

        #endregion


        // Алгоритм проверки семейств отверстия
        /*
        * Проверить семейство отверстий правильно ли они расположены
        * Если пересекается с чем либо (по краю) то удалить
        * Если по центру элемента нет то удалить
        */


        public void Dispose()
        {
            ClearAndResetData();
            collisionMng?.Dispose();
        }
    }
}