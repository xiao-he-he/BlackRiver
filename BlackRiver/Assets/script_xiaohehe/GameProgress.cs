using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Single-slot, scene-entry checkpoints. Puzzle positions and timers restart on continue.</summary>
public sealed class GameProgress : MonoBehaviour
{
    public const string SaveKey = "BlackRiver.Progress.v1";
    public const string MainMenuPath = "Assets/Scenes/主菜单.unity";
    public const string FirstScenePath = "Assets/Scenes/序章1.unity";

    [Serializable]
    private sealed class Checkpoint
    {
        public int version = 1;
        public string scenePath;
        public bool staffUnlocked;
    }

    private static GameProgress instance;

    public static bool CanContinue => TryRead(out _);
    public static string SavedScenePath => TryRead(out var save) ? save.scenePath : string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        var root = new GameObject("Game Progress");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<GameProgress>();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && IsGameplayScene(scene.path))
            Write(scene.path, StaffAbility.UnlockedThisSession);
    }

    public static void SaveCurrentCheckpoint()
    {
        string path = SceneManager.GetActiveScene().path;
        if (IsGameplayScene(path)) Write(path, StaffAbility.UnlockedThisSession);
    }

    public static bool StartNewGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(FirstScenePath)) return false;
        StaffAbility.RestoreSession(false);
        Write(FirstScenePath, false);
        return Load(FirstScenePath);
    }

    public static bool ContinueGame()
    {
        if (!TryRead(out var save)) return false;
        StaffAbility.RestoreSession(save.staffUnlocked);
        return Load(save.scenePath);
    }

    public static bool RestartLevel()
    {
        string path = SceneManager.GetActiveScene().path;
        if (!IsGameplayScene(path)) return false;
        SaveCurrentCheckpoint();
        return Load(path);
    }

    public static bool ReturnToMainMenu()
    {
        if (!Application.CanStreamedLevelBeLoaded(MainMenuPath)) return false;
        SaveCurrentCheckpoint();
        return Load(MainMenuPath);
    }

    private static bool Load(string path)
    {
        if (!Application.CanStreamedLevelBeLoaded(path)) return false;
        GamePauseState.ResetForSceneChange();
        PrologueSceneFader.LoadScene(path, 0.25f, 0.4f);
        return true;
    }

    private static bool IsGameplayScene(string path) =>
        !string.IsNullOrEmpty(path) && path != MainMenuPath &&
        path.StartsWith("Assets/Scenes/", StringComparison.Ordinal) &&
        SceneUtility.GetBuildIndexByScenePath(path) >= 0;

    private static bool TryRead(out Checkpoint save)
    {
        save = null;
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return false;
        try { save = JsonUtility.FromJson<Checkpoint>(json); }
        catch (ArgumentException) { return false; }
        return save != null && save.version == 1 && IsGameplayScene(save.scenePath) &&
            Application.CanStreamedLevelBeLoaded(save.scenePath);
    }

    private static void Write(string path, bool staffUnlocked)
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new Checkpoint
        {
            scenePath = path,
            staffUnlocked = staffUnlocked
        }));
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveCurrentCheckpoint();
    }

    private void OnApplicationQuit() => SaveCurrentCheckpoint();
}
