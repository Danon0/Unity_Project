using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class GameOfLife : MonoBehaviour
{
    public int width = 60;
    public int height = 40;
    public float cellSize = 0.18f;
    public GameObject cellPrefab; 
    public Transform gridParent;

    public Button playPauseButton;
    public Button stepButton;
    public Button randomizeButton;
    public Button clearButton;
    public Toggle pvpToggle;
    public Slider speedSlider; 
    public Slider randomDensitySlider;
    public Button gliderButton, blockButton, gunButton;
    public TMP_Text statusText;
    public TMP_Text whiteScoreText, blackScoreText;
    public GameObject resultsPanel;
    public TMP_Text resultsText;

    private CellData[,] grid;
    private CellView[,] views;
    private bool playing = false;
    private float timer = 0f;
    private float stepDelay = 0.2f;
    private int generation = 0;

    public enum Owner { None = 0, White = 1, Black = 2 }
    private int whiteScore = 0, blackScore = 0;

    void Start()
    {
        InitGrid();
        HookupUI();
        UpdateScoreUI();
        SetStatus("Paused");
    }

    void Update()
    {
        HandleMouseInput();

        if (playing)
        {
            timer += Time.deltaTime;
            stepDelay = 1 / Mathf.Clamp(speedSlider.value, 0.01f, 2f);
            if (timer >= stepDelay)
            {
                timer = 0f;
                StepSimulation();
            }
        }
    }

    void InitGrid()
    {
        grid = new CellData[width, height];
        views = new CellView[width, height];

        if (gridParent == null)
        {
            GameObject gp = new GameObject("GridParent");
            gridParent = gp.transform;
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new CellData();
                Vector3 pos = new Vector3((x - width / 2f) * cellSize, (y - height / 2f) * cellSize, 0);
                GameObject go = Instantiate(cellPrefab, pos, Quaternion.identity, gridParent);
                go.transform.localScale = Vector3.one * cellSize * 0.98f;
                CellView cv = go.GetComponent<CellView>();
                cv.Init(x, y, this);
                views[x, y] = cv;
                UpdateView(x, y);
            }
    }

    void HookupUI()
    {
        playPauseButton.onClick.AddListener(TogglePlayPause);
        stepButton.onClick.AddListener(() => { if (!playing) StepSimulation(); });
        randomizeButton.onClick.AddListener(Randomize);
        clearButton.onClick.AddListener(ClearGrid);

        gliderButton.onClick.AddListener(() => PlacePatternAtMouse(Patterns.Glider));
        blockButton.onClick.AddListener(() => PlacePatternAtMouse(Patterns.Block));
        gunButton.onClick.AddListener(() => PlacePatternAtMouse(Patterns.GosperGliderGun));
    }

    void SetStatus(string s) { if (statusText) statusText.text = s; }

    public void TogglePlayPause()
    {
        playing = !playing;
        SetStatus(playing ? "Playing" : "Paused");
    }

    public void StepSimulation()
    {
        generation++;
        CellData[,] next = new CellData[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                next[x, y] = grid[x, y].Copy();

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int liveCount = 0;
                int whiteNeighbors = 0, blackNeighbors = 0;
                foreach (var n in GetNeighbors(x, y))
                {
                    if (n.IsAlive)
                    {
                        liveCount++;
                        if (n.owner == Owner.White) whiteNeighbors++;
                        if (n.owner == Owner.Black) blackNeighbors++;
                    }
                }

                var cell = grid[x, y];
                if (cell.IsAlive)
                {
                    if (liveCount == 2 || liveCount == 3)
                    {
                        next[x, y].IsAlive = true;
                        next[x, y].owner = cell.owner; 
                    }
                    else
                    {
                        next[x, y].IsAlive = false;
                        next[x, y].owner = Owner.None;
                        views[x, y].AnimateDeath();
                    }
                }
                else
                {
                    if (liveCount == 3)
                    {
                        next[x, y].IsAlive = true;
                        if (pvpToggle != null && pvpToggle.isOn)
                        {
                            next[x, y].owner = (whiteNeighbors >= blackNeighbors) ? Owner.White : Owner.Black;
                            if (next[x, y].owner == Owner.White) whiteScore++;
                            else blackScore++;
                        }
                        else
                        {
                            next[x, y].owner = Owner.White; 
                        }
                        views[x, y].AnimateBirth();
                    }
                }
            }

        grid = next;
        RefreshAllViews();
        UpdateScoreUI();
        CheckEndCondition();
    }

    void RefreshAllViews()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                UpdateView(x, y);
    }

    void UpdateView(int x, int y)
    {
        views[x, y].SetState(grid[x, y].IsAlive, grid[x, y].owner);
    }

    IEnumerable<CellData> GetNeighborsRaw(int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    yield return grid[nx, ny];
            }
    }

    List<CellData> GetNeighbors(int x, int y)
    {
        List<CellData> list = new List<CellData>();
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    list.Add(grid[nx, ny]);
            }
        return list;
    }

    public void ToggleCellAt(int x, int y, Owner owner = Owner.White)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (playing) return;
        if (grid[x, y].IsAlive)
        {
            grid[x, y].IsAlive = false;
            grid[x, y].owner = Owner.None;
            views[x, y].AnimateDeath();
        }
        else
        {
            grid[x, y].IsAlive = true;
            grid[x, y].owner = owner;
            views[x, y].AnimateBirth();
        }
        UpdateView(x, y);
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int gx = Mathf.RoundToInt(world.x / cellSize + width / 2f);
            int gy = Mathf.RoundToInt(world.y / cellSize + height / 2f);
            Owner ownerToPlace = Owner.White;

            ToggleCellAt(gx, gy, ownerToPlace);
        }
        else if (pvpToggle != null && pvpToggle.isOn && Input.GetMouseButton(1))
        {
            Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int gx = Mathf.RoundToInt(world.x / cellSize + width / 2f);
            int gy = Mathf.RoundToInt(world.y / cellSize + height / 2f);
            Owner ownerToPlace = Owner.Black;
            ToggleCellAt(gx, gy, ownerToPlace);
        }
                
    }

    public void Randomize()
    {
        if (playing) return;
        float density = randomDensitySlider != null ? randomDensitySlider.value : 0.2f;
        whiteScore = blackScore = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                bool alive = Random.value < density;
                grid[x, y].IsAlive = alive;
                if (alive && pvpToggle != null && pvpToggle.isOn)
                {
                    grid[x, y].owner = (Random.value < 0.5f) ? Owner.White : Owner.Black;
                    if (grid[x, y].owner == Owner.White) whiteScore++; else blackScore++;
                }
                else if (alive)
                {
                    grid[x, y].owner = Owner.White;
                }
                else grid[x, y].owner = Owner.None;
                UpdateView(x, y);
            }
        UpdateScoreUI();
    }

    public void ClearGrid()
    {
        if (playing) return;
        whiteScore = blackScore = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                grid[x, y].IsAlive = false;
                grid[x, y].owner = Owner.None;
                UpdateView(x, y);
            }
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (whiteScoreText) whiteScoreText.text = $"White: {whiteScore}";
        if (blackScoreText) blackScoreText.text = $"Black: {blackScore}";
    }

    void CheckEndCondition()
    {
        bool anyAlive = false;
        for (int x = 0; x < width && !anyAlive; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y].IsAlive) { anyAlive = true; break; }
        if (!anyAlive)
        {
            playing = false;
            SetStatus("Ended - no live cells");
            ShowResults();
        }
    }

    void ShowResults()
    {
        if (resultsPanel) resultsPanel.SetActive(true);
        string res;
        if (whiteScore > blackScore) res = $"White wins {whiteScore} : {blackScore}";
        else if (blackScore > whiteScore) res = $"Black wins {blackScore} : {whiteScore}";
        else res = $"Draw {whiteScore} : {blackScore}";
        if (resultsText) resultsText.text = res;
    }

    void PlacePatternAtMouse(int[,] pattern)
    {
        if (playing) return;
        Vector2 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int gx = Mathf.RoundToInt(world.x / cellSize + width / 2f);
        int gy = Mathf.RoundToInt(world.y / cellSize + height / 2f);

        int pw = pattern.GetLength(0);
        int ph = pattern.GetLength(1);
        for (int px = 0; px < pw; px++)
            for (int py = 0; py < ph; py++)
            {
                int val = pattern[px, py];
                int tx = gx + px - pw / 2;
                int ty = gy + py - ph / 2;
                if (tx >= 0 && tx < width && ty >= 0 && ty < height)
                {
                    grid[tx, ty].IsAlive = val == 1;
                    if (grid[tx, ty].IsAlive)
                        grid[tx, ty].owner = pvpToggle != null && pvpToggle.isOn ? (Random.value < 0.5f ? Owner.White : Owner.Black) : Owner.White;
                    else grid[tx, ty].owner = Owner.None;
                    UpdateView(tx, ty);
                }
            }
    }

    public class CellData
    {
        public bool IsAlive = false;
        public Owner owner = Owner.None;
        public CellData Copy() { return new CellData() { IsAlive = this.IsAlive, owner = this.owner }; }
    }
}
