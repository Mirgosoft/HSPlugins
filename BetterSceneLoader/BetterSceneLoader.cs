using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UILib;
using IllusionPlugin;
using System.IO;
using System.Collections;
using System.Diagnostics;

// imitate windows explorer thumbnail spacing and positioning for scene loader
// reset hsstudioaddon lighting on load if no xml data
// problem adjusting thumbnail size when certain number range of scenes
// indicator if scene has mod xml attached

namespace BetterSceneLoader
{
    public class BetterSceneLoader : MonoBehaviour
    {
        static string scenePath = Environment.CurrentDirectory + "/UserData/studioneo/BetterSceneLoader/";
        static string orderPath = scenePath + "order.txt";

        float buttonSize = 10f;
        float marginSize = 5f;
        float headerSize = 20f;
        float UIScale = 1.4f;
        float scrollOffsetX = -15f;
        float windowMarginX = 45f;
        float windowMarginY = 35f;

        Color dragColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        Color backgroundColor = new Color(1f, 1f, 1f, 1f);
        Color outlineColor = new Color(0f, 0f, 0f, 1f);

        Canvas UISystem;
        Image mainPanel;
        Dropdown category;
        ScrollRect imagelist;
        Image optionspanel;
        Image lastLoadedMark_Panel;
        string lastLoadedMark_filePath = "";
        Image confirmpanel;
        Image confirmpanel2;
        Text fileNameText;
        Button yesbutton;
        Button nobutton;
        Button yesbutton2;
        Button nobutton2;
        Text nametext;

        int columnCount;
        bool useExternalSavedata;
        float scrollSensitivity;
        bool autoClose;
        bool smallWindow;

        Dictionary<string, Image> sceneCache = new Dictionary<string, Image>();
        Button currentButton;
        string currentPath = "";

        void Awake()
        {
            UIUtility.Init("BetterSceneLoader");
            MakeBetterSceneLoader();
            LoadSettings();
            StartCoroutine(StartingScene());
        }

        IEnumerator StartingScene()
        {
            for(int i = 0; i < 10; i++) yield return null;
            var files = Directory.GetFiles(scenePath, "defaultscene.png", SearchOption.TopDirectoryOnly).ToList();
            if(files.Count > 0) LoadScene(files[0]);
        }

        void OnDestroy()
        {
            DestroyImmediate(UISystem.gameObject);
        }

        bool LoadSettings()
        {
            columnCount = ModPrefs.GetInt("BetterSceneLoader", "ColumnCount", 3, true);
            useExternalSavedata = ModPrefs.GetBool("BetterSceneLoader", "UseExternalSavedata", true, true);
            scrollSensitivity = ModPrefs.GetFloat("BetterSceneLoader", "ScrollSensitivity", 3f, true);
            autoClose = ModPrefs.GetBool("BetterSceneLoader", "AutoClose", true, true);
            smallWindow = ModPrefs.GetBool("BetterSceneLoader", "SmallWindow", true, true);

            UpdateWindow();
            return true;
        }

        void UpdateWindow()
        {
            foreach(var scene in sceneCache)
            {
                var gridlayout = scene.Value.gameObject.GetComponent<AutoGridLayout>();
                if(gridlayout != null)
                {
                    gridlayout.m_Column = columnCount;
                    gridlayout.CalculateLayoutInputHorizontal();
                }
            }

            if(imagelist != null)
            {
                imagelist.scrollSensitivity = Mathf.Lerp(30f, 300f, scrollSensitivity / 10f);
            }

            if(mainPanel)
            {
                if(smallWindow)
                    mainPanel.transform.SetRect(0.5f, 0f, 1f, 1f, windowMarginX, windowMarginY, -windowMarginX, -windowMarginY);
                else
                    mainPanel.transform.SetRect(0f, 0f, 1f, 1f, windowMarginX, windowMarginY, -windowMarginX, -windowMarginY); 
            }
        }
        
        void MakeBetterSceneLoader()
        {
            UISystem = UIUtility.CreateNewUISystem("BetterSceneLoaderCanvas");
            UISystem.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f / UIScale, 1080f / UIScale);
            UISystem.gameObject.SetActive(false);
            UISystem.gameObject.transform.SetParent(transform);

            mainPanel = UIUtility.CreatePanel("Panel", UISystem.transform);
            mainPanel.color = backgroundColor;
            UIUtility.AddOutlineToObject(mainPanel.transform, outlineColor);

            var drag = UIUtility.CreatePanel("Draggable", mainPanel.transform);
            drag.transform.SetRect(0f, 1f, 1f, 1f, 0f, -headerSize);
            drag.color = dragColor;
            UIUtility.MakeObjectDraggable(drag.rectTransform, mainPanel.rectTransform);

            nametext = UIUtility.CreateText("Nametext", drag.transform, "Scenes");
            nametext.transform.SetRect(0.87f, 0f, 0.98f, 1f, 340f, 0f, -buttonSize * 2f);
            nametext.alignment = TextAnchor.MiddleCenter;

            var close = UIUtility.CreateButton("CloseButton", drag.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -buttonSize * 2f);
            close.onClick.AddListener(() => UISystem.gameObject.SetActive(false));
            Utils.AddCloseSymbol(close);
            
            category = UIUtility.CreateDropdown("Dropdown", drag.transform, "Categories");
            category.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 200f);
            category.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
            category.captionText.alignment = TextAnchor.MiddleCenter;
            category.onValueChanged.AddListener((x) =>
            {
                imagelist.content.GetComponentInChildren<Image>().gameObject.SetActive(false);
                imagelist.content.anchoredPosition = new Vector2(0f, 0f);
                PopulateGrid();
            });
            // Увеличиваем область выпадающего списка и чувствительность скролла.
            ScrollRect categoryListRect = category.transform.Find("Template").GetComponent<ScrollRect>();
            categoryListRect.scrollSensitivity = 45f;
            categoryListRect.transform.SetRect(0f,0f,1f,1f, 0f, -330f, 0f, 0f);
            GetCategories();

            var refresh = UIUtility.CreateButton("RefreshButton", drag.transform, "Refresh");
            refresh.transform.SetRect(0f, 0f, 0f, 1f, 200f, 0f, 280f);
            refresh.onClick.AddListener(() => ReloadImages());

            var save = UIUtility.CreateButton("SaveButton", drag.transform, "Save");
            save.transform.SetRect(0f, 0f, 0f, 1f, 280f, 0f, 360f, 15f);
            save.onClick.AddListener(() => SaveScene());

            var loadingPanel = UIUtility.CreatePanel("LoadingIconPanel", drag.transform);
            loadingPanel.transform.SetRect(0f, 0f, 0f, 1f, 380f, 0f, 380f + headerSize);
            loadingPanel.color = new Color(0f, 0f, 0f, 0f);
            var loadingIcon = UIUtility.CreatePanel("LoadingIcon", loadingPanel.transform);
            loadingIcon.transform.SetRect(0.1f, 0.1f, 0.9f, 0.9f);
            var texture = Utils.LoadTexture(Properties.Resources.loadicon);
            loadingIcon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            LoadingIcon.Init(loadingIcon, -5f);

            var folder = UIUtility.CreateButton("FolderButton", drag.transform, "Folder");
            folder.transform.SetRect(0f, 0f, 0f, 1f, 360f, 0f, 440f);
            folder.onClick.AddListener(() => openFolder());
            
            var oneColumn = UIUtility.CreateButton("oneColumn", drag.transform, "1");
            oneColumn.transform.SetRect(0f, 0f, 0f, 1f, 450f, 0f, 470f, 0f);
            oneColumn.onClick.AddListener(() => setColums(1));

            var twoColumn = UIUtility.CreateButton("twoColumn", drag.transform, "2");
            twoColumn.transform.SetRect(0f, 0f, 0f, 1f, 470f, 0f, 490f, 0f);
            twoColumn.onClick.AddListener(() => setColums(2));

            imagelist = UIUtility.CreateScrollView("Imagelist", mainPanel.transform);
            imagelist.transform.SetRect(0f, 0f, 1f, 1f, marginSize, marginSize, -marginSize, -headerSize - marginSize / 2f);
            imagelist.gameObject.AddComponent<Mask>();
            imagelist.content.gameObject.AddComponent<VerticalLayoutGroup>();
            imagelist.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            imagelist.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(scrollOffsetX, 0f);
            imagelist.viewport.offsetMax = new Vector2(scrollOffsetX, 0f);
            imagelist.movementType = ScrollRect.MovementType.Clamped;

            lastLoadedMark_Panel = UIUtility.CreatePanel("lastLoadedMark_Panel", imagelist.transform);
            lastLoadedMark_Panel.color = new Color(1f, .5f, 1f, 1f);
            UIUtility.AddOutlineToObject(lastLoadedMark_Panel.transform, outlineColor);
            lastLoadedMark_Panel.gameObject.SetActive(false);

            var lastLoadedMark_Text = UIUtility.CreateText("confirmPanelText", lastLoadedMark_Panel.transform, "Current");
            lastLoadedMark_Text.color = new Color(.1f, .1f, .1f, 1f);
            lastLoadedMark_Text.transform.SetRect(.05f, .02f, .95f, .98f);
            lastLoadedMark_Text.alignment = TextAnchor.MiddleCenter;

            optionspanel = UIUtility.CreatePanel("ButtonPanel", imagelist.transform);
            optionspanel.gameObject.SetActive(false);

            confirmpanel = UIUtility.CreatePanel("ConfirmPanel", imagelist.transform);
            confirmpanel.gameObject.SetActive(false);

            var confirmPanelText = UIUtility.CreateText("confirmPanelText", confirmpanel.transform, "confirmText");
            confirmPanelText.text = "Delete scene?";
            confirmPanelText.color = Color.red;
            confirmPanelText.fontStyle = FontStyle.Bold;
            confirmPanelText.transform.SetRect(-1f, 1f, 2f, 2f);
            confirmPanelText.alignment = TextAnchor.MiddleCenter;

            yesbutton = UIUtility.CreateButton("YesButton", confirmpanel.transform, "Y");
            yesbutton.transform.SetRect(0f, 0f, 0.5f, 1f);
            yesbutton.onClick.AddListener(() => DeleteScene(currentPath));

            nobutton = UIUtility.CreateButton("NoButton", confirmpanel.transform, "N");
            nobutton.transform.SetRect(0.5f, 0f, 1f, 1f);
            nobutton.onClick.AddListener(() => { confirmpanel.gameObject.SetActive(false); confirmpanel2.gameObject.SetActive(false); });

            confirmpanel2 = UIUtility.CreatePanel("ConfirmPanel2", imagelist.transform);
            confirmpanel2.gameObject.SetActive(false);

            var confirmPanelText2 = UIUtility.CreateText("confirmPanelText2", confirmpanel2.transform, "confirmText2");
            confirmPanelText2.text = "ReSave scene?";
            confirmPanelText2.color = Color.magenta;
            confirmPanelText2.fontStyle = FontStyle.Bold;
            confirmPanelText2.transform.SetRect(-1f, 1f, 2f, 2f);
            confirmPanelText2.alignment = TextAnchor.MiddleCenter;

            yesbutton2 = UIUtility.CreateButton("YesButton2", confirmpanel2.transform, "Y");
            yesbutton2.transform.SetRect(0f, 0f, 0.5f, 1f);
            yesbutton2.onClick.AddListener(() => { StartCoroutine(ResaveScene()); });

            nobutton2 = UIUtility.CreateButton("NoButton2", confirmpanel2.transform, "N");
            nobutton2.transform.SetRect(0.5f, 0f, 1f, 1f);
            nobutton2.onClick.AddListener(() => { confirmpanel.gameObject.SetActive(false); confirmpanel2.gameObject.SetActive(false); });

            var filenamePanel = UIUtility.CreatePanel("filenamePanel", optionspanel.transform);
            filenamePanel.color = new Color(0f, 0f, 0f, .8f);
            filenamePanel.transform.SetRect(0f, 5.2f, 1f, 6.6f);

            fileNameText = UIUtility.CreateText("FileNameText", filenamePanel.transform, "filename");
            fileNameText.color = new Color(.95f, .95f, .95f, 1f);
            fileNameText.transform.SetRect(0.05f, 0f, 0.95f, 1f);
            fileNameText.alignment = TextAnchor.MiddleCenter;

            var resavebutton = UIUtility.CreateButton("ReSaveButton", optionspanel.transform, "ReSave");
            resavebutton.transform.SetRect(0.68f, 4f, 0.98f, 5f);
            resavebutton.onClick.AddListener(() => { confirmpanel.gameObject.SetActive(false); confirmpanel2.gameObject.SetActive(true); });

            var loadbutton = UIUtility.CreateButton("LoadButton", optionspanel.transform, "Load");
            loadbutton.transform.SetRect(0f, 0f, 0.3f, 1f);
            loadbutton.onClick.AddListener(() => LoadScene(currentPath));

            var importbutton = UIUtility.CreateButton("ImportButton", optionspanel.transform, "Import");
            importbutton.transform.SetRect(0.35f, 0f, 0.65f, 1f);
            importbutton.onClick.AddListener(() => ImportScene(currentPath));

            var deletebutton = UIUtility.CreateButton("DeleteButton", optionspanel.transform, "Delete");
            deletebutton.transform.SetRect(0.7f, 0f, 1f, 1f);
            deletebutton.onClick.AddListener(() => { confirmpanel.gameObject.SetActive(true); confirmpanel2.gameObject.SetActive(false); });

            PopulateGrid();
        }

        private void GetCategories()
        {
            if(!File.Exists(scenePath)) Directory.CreateDirectory(scenePath);
            var folders = Directory.GetDirectories(scenePath);

            if(folders.Length == 0)
            {
                Directory.CreateDirectory(scenePath + "Category1");
                Directory.CreateDirectory(scenePath + "Category2");
                folders = Directory.GetDirectories(scenePath);
            }

            string[] order;
            if(File.Exists(orderPath))
            {
                order = File.ReadAllLines(orderPath);
            }
            else
            {
                order = new string[0];
                File.Create(orderPath);
            }
            
            var sorted = folders.Select(x => Path.GetFileName(x)).OrderBy(x => order.Contains(x) ? Array.IndexOf(order, x) : order.Length);
            category.options = sorted.Select(x => new Dropdown.OptionData(x)).ToList();
        }

        void LoadScene(string path)
        {
            confirmpanel.gameObject.SetActive(false);
            confirmpanel2.gameObject.SetActive(false);
            optionspanel.gameObject.SetActive(false);
            lastLoadedMark_filePath = path;
            SetLastLoadedMarkParentObj(optionspanel.transform.parent, true);
            Utils.InvokePluginMethod("LockOnPlugin.LockOnBase", "ResetModState");
            Studio.Studio.Instance.LoadScene(path);
            if(useExternalSavedata) StartCoroutine(StudioNEOExtendSaveMgrLoad(path));
            if(autoClose) UISystem.gameObject.SetActive(false);

        }

        IEnumerator StudioNEOExtendSaveMgrLoad(string path)
        {
            for(int i = 0; i < 3; i++) yield return null;
            Utils.InvokePluginMethod("HSStudioNEOExtSave.StudioNEOExtendSaveMgr", "LoadExtData", path);
            Utils.InvokePluginMethod("HSStudioNEOExtSave.StudioNEOExtendSaveMgr", "LoadExtDataRaw", path);
        }

        void SaveScene(string filepath = "", bool resave = false)
        {
            string category_path = GetCategoryFolder();
            if (!Directory.Exists(category_path)) {
                GetCategories();
                return;
            }

            Studio.Studio.Instance.dicObjectCtrl.Values.ToList().ForEach(x => x.OnSavePreprocessing());
            Studio.Studio.Instance.sceneInfo.cameraSaveData = Studio.Studio.Instance.cameraCtrl.Export();
            string path = "";
            if (filepath == "")
                path += category_path + DateTime.Now.ToString("yyyy_MMdd_HHmm_ss_fff") + ".png";
            else
                path = filepath;
            if (File.Exists(path))
                File.Delete(path);
            Studio.Studio.Instance.sceneInfo.Save(path);
            if(useExternalSavedata)
            {
                Utils.InvokePluginMethod("HSStudioNEOExtSave.StudioNEOExtendSaveMgr", "SaveExtData", path);
                //InvokePluginMethod("HSStudioNEOExtSave.StudioNEOExtendSaveMgr", "SaveExtDataRaw", path);
            }
            
            if (!resave) {
                var button = CreateSceneButton(imagelist.content.GetComponentInChildren<Image>().transform, PngAssist.LoadTexture(path), path);
                button.transform.SetAsFirstSibling();
                lastLoadedMark_filePath = path;
                SetLastLoadedMarkParentObj(button.transform, true);
            }
        }

        void DeleteScene(string path)
        {
            File.Delete(path);
            currentButton.gameObject.SetActive(false);
            confirmpanel.gameObject.SetActive(false);
            confirmpanel2.gameObject.SetActive(false);
            optionspanel.gameObject.SetActive(false);
            if (lastLoadedMark_Panel.transform.parent == optionspanel.transform.parent) 
                SetLastLoadedMarkParentObj(imagelist.transform, false);
        }

        void ImportScene(string path)
        {
            Studio.Studio.Instance.ImportScene(path);
            confirmpanel.gameObject.SetActive(false);
            confirmpanel2.gameObject.SetActive(false);
            optionspanel.gameObject.SetActive(false);
        }

        IEnumerator ResaveScene() {
            SaveScene(currentPath, true); 
            string file_path = currentPath; // (currentPath updated in onBtnClick event.)

            Transform curr_btn_trans = optionspanel.transform.parent;

            // Mod Orginizer fix
            if (!File.Exists(file_path))
                file_path = ModOrginizerPathFix(file_path);

            using (WWW www = new WWW("file:///" + file_path)) {
                yield return www;
                if (!string.IsNullOrEmpty(www.error)) throw new Exception(www.error);
                Texture2D texture = PngAssist.ChangeTextureFromByte(www.bytes);
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                curr_btn_trans.gameObject.GetComponent<Image>().sprite = sprite;
            }

            optionspanel.gameObject.SetActive(false);
            confirmpanel.gameObject.SetActive(false);
            confirmpanel2.gameObject.SetActive(false);
            SetLastLoadedMarkParentObj(curr_btn_trans, true);
        }

        void ReloadImages()
        {
            optionspanel.transform.SetParent(imagelist.transform);
            confirmpanel.transform.SetParent(imagelist.transform);
            confirmpanel2.transform.SetParent(imagelist.transform);
            optionspanel.gameObject.SetActive(false);
            confirmpanel.gameObject.SetActive(false);
            confirmpanel2.gameObject.SetActive(false);
            SetLastLoadedMarkParentObj(imagelist.transform, false);

            Destroy(imagelist.content.GetComponentInChildren<Image>().gameObject);
            imagelist.content.anchoredPosition = new Vector2(0f, 0f);
            PopulateGrid(true);
        }

        void PopulateGrid(bool forceUpdate = false)
        {
            if(forceUpdate) sceneCache.Remove(category.captionText.text);

            Image sceneList;
            if(sceneCache.TryGetValue(category.captionText.text, out sceneList))
            {
                sceneList.gameObject.SetActive(true);
            }
            else
            {
                List<KeyValuePair<DateTime, string>> scenefiles = new List<KeyValuePair<DateTime, string>>();

                string category_path = GetCategoryFolder();
                if (Directory.Exists(category_path))
                    scenefiles = (from s in Directory.GetFiles(category_path, "*.png") select new KeyValuePair<DateTime, string> (File.GetLastWriteTime(s), s)).ToList();
                scenefiles.Sort((KeyValuePair<DateTime, string> a, KeyValuePair<DateTime, string> b) => StrCmpLogicalW(b.Value, a.Value));
                
                var container = UIUtility.CreatePanel("GridContainer", imagelist.content.transform);
                container.transform.SetRect(0f, 0f, 1f, 1f);
                container.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var gridlayout = container.gameObject.AddComponent<AutoGridLayout>();
                gridlayout.spacing = new Vector2(marginSize, marginSize);
                gridlayout.m_IsColumn = true;
                gridlayout.m_Column = columnCount;

                StartCoroutine(LoadButtonsAsync(container.transform, scenefiles));
                sceneCache.Add(category.captionText.text, container);
            }
        }

        IEnumerator LoadButtonsAsync(Transform parent, List<KeyValuePair<DateTime, string>> scenefiles)
        {
            string categoryText = category.captionText.text;
            string sceneFilePath = "";
            foreach(var scene in scenefiles)
            {
                sceneFilePath = scene.Value;

                // Mod Orginizer fix
                if (!File.Exists(sceneFilePath))
                    sceneFilePath = ModOrginizerPathFix(sceneFilePath);

                LoadingIcon.loadingState[categoryText] = true;

                //Console.WriteLine("URL 1:" + scene.Value);
                using (WWW www = new WWW("file:///" + sceneFilePath))
                {
                    //Console.WriteLine("URL 2:"+scene.Value);
                    yield return www;
                    if(!string.IsNullOrEmpty(www.error)) throw new Exception(www.error);
                    CreateSceneButton(parent, PngAssist.ChangeTextureFromByte(www.bytes), sceneFilePath);
                }
            }

            LoadingIcon.loadingState[categoryText] = false;
        }

        Button CreateSceneButton(Transform parent, Texture2D texture, string path)
        {
            var button = UIUtility.CreateButton("ImageButton", parent, "");
            button.onClick.AddListener(() =>
            {
                currentButton = button;
                currentPath = path;

                if(optionspanel.transform.parent != button.transform)
                {
                    fileNameText.text = currentPath.Substring(currentPath.LastIndexOf("/") + 1).Replace(".png", "");
                    optionspanel.transform.SetParent(button.transform);
                    optionspanel.transform.SetRect(0f, 0f, 1f, 0.15f);
                    optionspanel.gameObject.SetActive(true);

                    confirmpanel.transform.SetParent(button.transform);
                    confirmpanel.transform.SetRect(0.4f, 0.4f, 0.6f, 0.6f);
                    confirmpanel2.transform.SetParent(button.transform);
                    confirmpanel2.transform.SetRect(0.4f, 0.4f, 0.6f, 0.6f);
                }
                else {
                    optionspanel.gameObject.SetActive(!optionspanel.gameObject.activeSelf);
                }

                confirmpanel.gameObject.SetActive(false);
                confirmpanel2.gameObject.SetActive(false);
            });
            
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            button.gameObject.GetComponent<Image>().sprite = sprite;

            if (path == lastLoadedMark_filePath)
                SetLastLoadedMarkParentObj(button.transform, true);

            return button;
        }

        string GetCategoryFolder()
        {
            if(category?.captionText?.text != null)
            {
                return ModOrginizerPathFix(scenePath + category.captionText.text + "/");
            }

            return scenePath;
        }

        void openFolder() {
            string path = GetCategoryFolder();

            path = ModOrginizerPathFix(path);
            if (!Directory.Exists(path)) {
                GetCategories();
                return;
            }
            Process.Start(path);
        }

        void setColums(int columsNum) {
            columnCount = columsNum;
            UpdateWindow();
            return;
        }

        public static int StrCmpLogicalW(string x, string y)
        {
            if (x != null && y != null)
            {
                int xIndex = 0;
                int yIndex = 0;

                while (xIndex < x.Length)
                {
                    if (yIndex >= y.Length)
                        return 1;

                    if (char.IsDigit(x[xIndex]))
                    {
                        if (!char.IsDigit(y[yIndex]))
                            return -1;

                        // Compare the numbers
                        List<char> xText = new List<char>();
                        List<char> yText = new List<char>();

                        for (int i = xIndex; i < x.Length; i++)
                        {
                            var xChar = x[i];

                            if (char.IsDigit(xChar))
                                xText.Add(xChar);
                            else
                                break;
                        }

                        for (int j = yIndex; j < y.Length; j++)
                        {
                            var yChar = y[j];

                            if (char.IsDigit(yChar))
                                yText.Add(yChar);
                            else
                                break;
                        }

                        int xValue = Convert.ToInt32(new string(xText.ToArray()));
                        int yValue = Convert.ToInt32(new string(yText.ToArray()));

                        if (xValue < yValue)
                            return -1;
                        else if (xValue > yValue)
                            return 1;

                        // Skip
                        xIndex += xText.Count;
                        yIndex += yText.Count;
                    }
                    else if (char.IsDigit(y[yIndex]))
                        return 1;
                    else
                    {
                        int difference = char.ToUpperInvariant(x[xIndex]).CompareTo(char.ToUpperInvariant(y[yIndex]));
                        if (difference > 0)
                            return 1;
                        else if (difference < 0)
                            return -1;

                        xIndex++;
                        yIndex++;
                    }
                }

                if (yIndex < y.Length)
                    return -1;
            }

            return 0;
        }

        private static string ModOrginizerPathFix(string path) {
            path = path.Replace("data\\UserData\\", "MOHS\\overwrite\\UserData\\").Replace("Data\\UserData\\", "MOHS\\overwrite\\UserData\\");
            path = path.Replace("data/UserData/", "MOHS/overwrite/UserData/").Replace("Data/UserData/", "MOHS/overwrite/UserData/");
            return path;
        }

        void SetLastLoadedMarkParentObj(Transform transform, bool visible = true) {
            lastLoadedMark_Panel.transform.SetParent(transform);
            lastLoadedMark_Panel.gameObject.SetActive(visible);
            lastLoadedMark_Panel.transform.SetRect(0f, .6f, .25f, .75f);
        }
    }
}
