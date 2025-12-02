using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteAlways]
public class MultiSliceViewer : MonoBehaviour
{
    [System.Serializable]
    public class SliceView
    {
        public string name;
        public RawImage displayImage;
        public Slider sliceSlider;
        public Button fullscreenButton;
        public Image headerBackground;
        public FullscreenSliceController fullscreenController;

        [HideInInspector] public List<Texture2D> slices = new List<Texture2D>();
        [HideInInspector] public bool usingAlternate = false;

        public MeasureTool measureTool;
    }

    [System.Serializable]
    public class TableMapping
    {
        public string normalTable;
        public string alternateTable;
    }

    [Header("Normal View")]
    public GameObject normalFourUpGrid;
    public SliceView[] sliceViews;

    [Header("Normal ↔ Alternate Table Mapping")]
    public TableMapping[] tableMappings;

    private Dictionary<string, SliceSize> sliceSizes = new Dictionary<string, SliceSize>();

    private string currentParentFolder = "";
    private bool usingAlternate = false;

    private struct SliceSize
    {
        public float WidthMM;
        public float HeightMM;

        public SliceSize(float width, float height)
        {
            WidthMM = width;
            HeightMM = height;
        }
    }

    private void OnEnable()
    {
        if (sliceViews.Length > 0)
        {
            SetupFullscreenButtons();
        }
    }

    public void SetParentFolder(string parentFolder)
    {
        if (string.IsNullOrEmpty(parentFolder))
        {
            Debug.LogError("Parent folder path is null!");
            return;
        }

        currentParentFolder = parentFolder;

        string[] folders = parentFolder.Split('/');
        string modelName = folders[folders.Length - 1];

        LoadSliceSizes(modelName);

        foreach (var view in sliceViews)
        {
            string folderToLoad = GetCurrentFolder(view.name);
            LoadSlicesFromFolder(view, folderToLoad);
            SetupSlider(view);
            UpdateSliceDisplay(view);

            if (sliceSizes.TryGetValue(view.name, out var size))
            {
                if (view.measureTool != null)
                {
                    view.measureTool.realWidthMM = size.WidthMM;
                    view.measureTool.realHeightMM = size.HeightMM;
                }
            }
        }

        SetupFullscreenButtons();
    }

    private string GetCurrentFolder(string sliceViewName)
    {
        string baseFolder = usingAlternate ? GetAlternateFolder(currentParentFolder) : currentParentFolder;
        return $"{baseFolder}/{sliceViewName}";
    }

    private string GetAlternateFolder(string normalFolder)
    {
        string[] parts = normalFolder.Split('/');
        string normalName = parts[parts.Length - 1];


        foreach (var mapping in tableMappings)
        {
            if (mapping.normalTable == normalName)
            {

                string altFolder = $"CTSlices/{mapping.alternateTable}";
                Debug.Log($"[Toggle] Switching from '{normalFolder}' → '{altFolder}'");
                return altFolder;
            }
        }

        Debug.LogWarning($"[Toggle] No mapping found for '{normalName}', staying normal.");
        return normalFolder;
    }


    public void ToggleAlternate()
    {
        if (string.IsNullOrEmpty(currentParentFolder))
        {
            Debug.LogError("Cannot toggle — parent folder not set yet! Call SetParentFolder() first.");
            return;
        }

        usingAlternate = !usingAlternate;
        SetParentFolder(currentParentFolder);
    }


    private void LoadSliceSizes(string modelName)
    {
        sliceSizes.Clear();

        TextAsset csvFile = Resources.Load<TextAsset>("Configs/SliceSizes");
        if (csvFile == null)
        {
            Debug.LogError(" NO CVS FILE FOR SIZES FOUND!");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        bool headerSkipped = false;
        foreach (var line in lines)
        {
            if (!headerSkipped)
            {
                headerSkipped = true;
                continue;
            }

            var fields = line.Split(',');
            if (fields.Length < 4) continue;

            string csvModel = fields[0].Trim();
            string csvColor = fields[1].Trim();
            if (!float.TryParse(fields[2].Trim(), out float width)) continue;
            if (!float.TryParse(fields[3].Trim(), out float height)) continue;

            if (csvModel == modelName)
            {
                sliceSizes[csvColor] = new SliceSize(width, height);
            }
        }
    }

    private void LoadSlicesFromFolder(SliceView view, string folder)
    {
        view.slices.Clear();

        if (string.IsNullOrEmpty(folder))
        {
            Debug.LogWarning($"Slice folder for view '{view.name}' is empty.");
            return;
        }

        Texture2D[] loaded = Resources.LoadAll<Texture2D>(folder);
        if (loaded.Length == 0)
        {
            Debug.LogError($"Slices not found in Resources/{folder} for '{view.name}'.");
            return;
        }

        System.Array.Sort(loaded, (a, b) => a.name.CompareTo(b.name));
        view.slices.AddRange(loaded);
    }

    private void SetupSlider(SliceView view)
    {
        if (view.sliceSlider == null || view.slices.Count == 0) return;

        view.sliceSlider.minValue = 0;
        view.sliceSlider.maxValue = view.slices.Count - 1;
        view.sliceSlider.wholeNumbers = true;

        // view.sliceSlider.onValueChanged.RemoveAllListeners();
        view.sliceSlider.onValueChanged.AddListener((value) => UpdateSliceDisplay(view));
    }

    private void UpdateSliceDisplay(SliceView view)
    {
        if (view.slices.Count == 0 || view.displayImage == null || view.sliceSlider == null) return;

        int index = Mathf.Clamp((int)view.sliceSlider.value, 0, view.slices.Count - 1);
        view.displayImage.texture = view.slices[index];
    }

    private void SetupFullscreenButtons()
    {
        foreach (var view in sliceViews)
        {
            if (view.fullscreenButton != null)
            {
                string capturedName = view.name;
                view.fullscreenButton.onClick.RemoveAllListeners();
                view.fullscreenButton.onClick.AddListener(() => ShowFullscreenFromABN(capturedName));
            }
        }
    }

    public void ShowFullscreenFromABN(string colorName)
    {
        ShowFullscreenFor(colorName);
    }

    private void ShowFullscreenFor(string colorName)
    {
        SliceView normalView = null;

        foreach (var view in sliceViews)
        {
            if (view.name == colorName)
            {
                normalView = view;
                break;
            }
        }

        if (normalView != null)
        {
            ShowFullscreenSingleView(normalView);
            if (normalFourUpGrid != null)
                normalFourUpGrid.SetActive(false);
        }
    }

    private void ShowFullscreenSingleView(SliceView view)
    {
        if (view.slices.Count == 0 || view.fullscreenController == null)
            return;

        int index = Mathf.RoundToInt(view.sliceSlider.value);
        Sprite headerSprite = view.headerBackground != null ? view.headerBackground.sprite : null;

        if (sliceSizes.TryGetValue(view.name, out var size) && view.measureTool != null)
        {
            view.measureTool.realWidthMM = size.WidthMM;
            view.measureTool.realHeightMM = size.HeightMM;
        }

        view.fullscreenController.ShowFullscreen(headerSprite, view.slices, index);
    }

    public void CloseFullscreen()
    {
        foreach (var view in sliceViews)
        {
            if (view.fullscreenController != null)
                view.fullscreenController.CloseFullscreen();
        }

        if (normalFourUpGrid != null)
            normalFourUpGrid.SetActive(true);
    }
}
