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
        Image confirmpanel;
        Text fileNameText;
        Button yesbutton;
        Button nobutton;
        Text nametext;

        int columnCount;
        bool useExternalSavedata;
        float scrollSensitivity;
        bool autoClose;
        bool smallWindow;

        Dictionary<string, Image> sceneCache = new Dictionary<string, Image>();
        Button currentButton;
        string currentPath;

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
            nametext.transform.SetRect(0f, 0f, 1f, 1f, 340f, 0f, -buttonSize * 2f);
            nametext.alignment = TextAnchor.MiddleCenter;

            var close = UIUtility.CreateButton("CloseButton", drag.transform, "");
            close.transform.SetRect(1f, 0f, 1f, 1f, -buttonSize * 2f);
            close.onClick.AddListener(() => UISystem.gameObject.SetActive(false));
            Utils.AddCloseSymbol(close);
            
            category = UIUtility.CreateDropdown("Dropdown", drag.transform, "Categories");
            category.transform.SetRect(0f, 0f, 0f, 1f, 0f, 0f, 140f);
            category.captionText.transform.SetRect(0f, 0f, 1f, 1f, 0f, 2f, -15f, -2f);
            category.captionText.alignment = TextAnchor.MiddleCenter;
            category.options = GetCategories();
            category.onValueChanged.AddListener((x) =>
            {
                imagelist.content.GetComponentInChildren<Image>().gameObject.SetActive(false);
                imagelist.content.anchoredPosition = new Vector2(0f, 0f);
                PopulateGrid();
            });

            var refresh = UIUtility.CreateButton("RefreshButton", drag.transform, "Refresh");
            refresh.transform.SetRect(0f, 0f, 0f, 1f, 140f, 0f, 220f);
            refresh.onClick.AddListener(() => ReloadImages());

            var save = UIUtility.CreateButton("SaveButton", drag.transform, "Save");
            save.transform.SetRect(0f, 0f, 0f, 1f, 220f, 0f, 300f, 20f);
            save.onClick.AddListener(() => SaveScene());

            var folder = UIUtility.CreateButton("FolderButton", drag.transform, "Folder");
            folder.transform.SetRect(0f, 0f, 0f, 1f, 300f, 0f, 380f);
            folder.onClick.AddListener(() => openFolder());
            
            var oneColumn = UIUtility.CreateButton("oneColumn", drag.transform, "1");
            oneColumn.transform.SetRect(0f, 0f, 0f, 1f, 440f, 0f, 480f, 20f);
            oneColumn.onClick.AddListener(() => setColums(1));

            var twoColumn = UIUtility.CreateButton("twoColumn", drag.transform, "2");
            twoColumn.transform.SetRect(0f, 0f, 0f, 1f, 480f, 0f, 520f, 20f);
            twoColumn.onClick.AddListener(() => setColums(2));

            var loadingPanel = UIUtility.CreatePanel("LoadingIconPanel", drag.transform);
            loadingPanel.transform.SetRect(0f, 0f, 0f, 1f, 380f, 0f, 380f + headerSize);
            loadingPanel.color = new Color(0f, 0f, 0f, 0f);
            var loadingIcon = UIUtility.CreatePanel("LoadingIcon", loadingPanel.transform);
            loadingIcon.transform.SetRect(0.1f, 0.1f, 0.9f, 0.9f);
            var texture = Utils.LoadTexture(Properties.Resources.loadicon);
            loadingIcon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            LoadingIcon.Init(loadingIcon, -5f);

            imagelist = UIUtility.CreateScrollView("Imagelist", mainPanel.transform);
            imagelist.transform.SetRect(0f, 0f, 1f, 1f, marginSize, marginSize, -marginSize, -headerSize - marginSize / 2f);
            imagelist.gameObject.AddComponent<Mask>();
            imagelist.content.gameObject.AddComponent<VerticalLayoutGroup>();
            imagelist.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            imagelist.verticalScrollbar.GetComponent<RectTransform>().offsetMin = new Vector2(scrollOffsetX, 0f);
            imagelist.viewport.offsetMax = new Vector2(scrollOffsetX, 0f);
            imagelist.movementType = ScrollRect.MovementType.Clamped;

            optionspanel = UIUtility.CreatePanel("ButtonPanel", imagelist.transform);
            optionspanel.gameObject.SetActive(false);

            confirmpanel = UIUtility.CreatePanel("ConfirmPanel", imagelist.transform);
            confirmpanel.gameObject.SetActive(false);

            yesbutton = UIUtility.CreateButton("YesButton", confirmpanel.transform, "Y");
            yesbutton.transform.SetRect(0f, 0f, 0.5f, 1f);
            yesbutton.onClick.AddListener(() => DeleteScene(currentPath));

            nobutton = UIUtility.CreateButton("NoButton", confirmpanel.transform, "N");
            nobutton.transform.SetRect(0.5f, 0f, 1f, 1f);
            nobutton.onClick.AddListener(() => confirmpanel.gameObject.SetActive(false));
            
            fileNameText = UIUtility.CreateText("FileNameText", optionspanel.transform, "filename");
            fileNameText.color = Color.red;
            fileNameText.transform.SetRect(0.05f, 5.2f, 0.95f, 6.6f);
            fileNameText.alignment = TextAnchor.MiddleCenter;

            var loadbutton = UIUtility.CreateButton("LoadButton", optionspanel.transform, "Load");
            loadbutton.transform.SetRect(0f, 0f, 0.3f, 1f);
            loadbutton.onClick.AddListener(() => LoadScene(currentPath));

            var importbutton = UIUtility.CreateButton("ImportButton", optionspanel.transform, "Import");
            importbutton.transform.SetRect(0.35f, 0f, 0.65f, 1f);
            importbutton.onClick.AddListener(() => ImportScene(currentPath));

            var deletebutton = UIUtility.CreateButton("DeleteButton", optionspanel.transform, "Delete");
            deletebutton.transform.SetRect(0.7f, 0f, 1f, 1f);
            deletebutton.onClick.AddListener(() => confirmpanel.gameObject.SetActive(true));

            PopulateGrid();
        }

        List<Dropdown.OptionData> GetCategories()
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
            return sorted.Select(x => new Dropdown.OptionData(x)).ToList();
        }

        void LoadScene(string path)
        {
            confirmpanel.gameObject.SetActive(false);
            optionspanel.gameObject.SetActive(false);
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

        void SaveScene()
        {
            Studio.Studio.Instance.dicObjectCtrl.Values.ToList().ForEach(x => x.OnSavePreprocessing());
            Studio.Studio.Instance.sceneInfo.cameraSaveData = Studio.Studio.Instance.cameraCtrl.Export();
            string path = GetCategoryFolder() + DateTime.Now.ToString("yyyy_MMdd_HHmm_ss_fff") + ".png";
            Studio.Studio.Instance.sceneInfo.Save(path);
            if(useExternalSavedata)
            {
                Utils.InvokePluginMethod("HSStudioNEOExtSave.StudioNEOExtendSaveMgr", "SaveExtData", path);
                //InvokePluginMethod("HSStudioNEOExtSave.StudioNEOExtendSaveMgr", "SaveExtDataRaw", path);
            }

            var button = CreateSceneButton(imagelist.content.GetComponentInChildren<Image>().transform, PngAssist.LoadTexture(path), path);
            button.transform.SetAsFirstSibling();
        }

        void DeleteScene(string path)
        {
            File.Delete(path);
            currentButton.gameObject.SetActive(false);
            confirmpanel.gameObject.SetActive(false);
            optionspanel.gameObject.SetActive(false);
        }

        void ImportScene(string path)
        {
            Studio.Studio.Instance.ImportScene(path);
            confirmpanel.gameObject.SetActive(false);
            optionspanel.gameObject.SetActive(false);
        }

        void ReloadImages()
        {
            optionspanel.transform.SetParent(imagelist.transform);
            confirmpanel.transform.SetParent(imagelist.transform);
            optionspanel.gameObject.SetActive(false);
            confirmpanel.gameObject.SetActive(false);

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
                List<KeyValuePair<DateTime, string>> scenefiles = (from s in Directory.GetFiles(GetCategoryFolder(), "*.png") select new KeyValuePair<DateTime, string> (File.GetLastWriteTime(s), s)).ToList();
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
                    sceneFilePath = sceneFilePath.Replace("data/UserData/", "MOHS/overwrite/UserData/").Replace("Data/UserData/", "MOHS/overwrite/UserData/");

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
                }
                else
                {
                    optionspanel.gameObject.SetActive(!optionspanel.gameObject.activeSelf);
                }

                confirmpanel.gameObject.SetActive(false);
            });
            
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            button.gameObject.GetComponent<Image>().sprite = sprite;

            return button;
        }

        string GetCategoryFolder()
        {
            if(category?.captionText?.text != null)
            {
                return scenePath + category.captionText.text + "/";
            }

            return scenePath;
        }

        void openFolder() {
            string path = GetCategoryFolder();

            path = path.Replace("data\\UserData\\", "MOHS\\overwrite\\UserData\\").Replace("Data\\UserData\\", "MOHS\\overwrite\\UserData\\");
            path = path.Replace("data/UserData/", "MOHS/overwrite/UserData/").Replace("Data/UserData/", "MOHS/overwrite/UserData/");
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

    }
}
